using OpenAI;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Samples.Whisper
{
    public class Whisper : MonoBehaviour
    {
        [SerializeField] private Button recordButton;
        [SerializeField] private Image recordButtonImage;
        [SerializeField] private Sprite defaultSprite;
        [SerializeField] private Sprite recordingSprite;
        [SerializeField] private Image progressBar;
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private TMP_Dropdown dropdown;
        [SerializeField] private GameObject whisperUI;

        private readonly string fileName = "output.wav";
        private readonly int duration = 5;

        private AudioClip clip;
        private bool isRecording;
        private bool canRecord = true; // Control recording availability
        private float time;
        private OpenAIApi openai;

        [Header("OpenAI Settings")]
        [SerializeField] private string apiKey = "sk-your-openai-api-key-here";

        private void Start()
        {
            // Initialize OpenAI API with API key
            if (!string.IsNullOrEmpty(apiKey) && apiKey != "sk-your-openai-api-key-here")
            {
                openai = new OpenAIApi(apiKey);
            }
            else
            {
                Debug.LogError("Whisper: Please set your OpenAI API key in the inspector!");
                return;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            dropdown.options.Add(new TMP_Dropdown.OptionData("Microphone not supported on WebGL"));
#else
            foreach (var device in Microphone.devices)
            {
                dropdown.options.Add(new TMP_Dropdown.OptionData(device)); // Changed to TMP_Dropdown.OptionData
            }
            recordButton.onClick.AddListener(StartRecording);
            dropdown.onValueChanged.AddListener(ChangeMicrophone);

            var index = PlayerPrefs.GetInt("user-mic-device-index");
            dropdown.SetValueWithoutNotify(index);
#endif
        }

        private void ChangeMicrophone(int index)
        {
            PlayerPrefs.SetInt("user-mic-device-index", index);
        }

        private void StartRecording()
        {
            // Check if recording is allowed
            if (!canRecord)
            {
                Debug.Log("Recording is disabled while NPC is thinking/responding");
                return;
            }

            isRecording = true;
            recordButton.enabled = false;

            // Change button image to recording state
            if (recordButtonImage != null && recordingSprite != null)
            {
                recordButtonImage.sprite = recordingSprite;
            }

            var index = PlayerPrefs.GetInt("user-mic-device-index");

#if !UNITY_WEBGL
            clip = Microphone.Start(dropdown.options[index].text, false, duration, 44100);
#endif
        }

        private async void EndRecording()
        {
            if (inputField != null)
                inputField.text = "Transcripting...";

#if !UNITY_WEBGL
            Microphone.End(null);
#endif

            byte[] data = SaveWav.Save(fileName, clip);

            var req = new CreateAudioTranscriptionsRequest
            {
                FileData = new FileData() { Data = data, Name = "audio.wav" },
                // File = Application.persistentDataPath + "/" + fileName,
                Model = "whisper-1",
                Language = "en"
            };
            var res = await openai.CreateAudioTranscription(req);

            progressBar.fillAmount = 0;
            if (inputField != null)
                inputField.text = res.Text;
            recordButton.enabled = true;

            // Change button image back to default state
            if (recordButtonImage != null && defaultSprite != null)
            {
                recordButtonImage.sprite = defaultSprite;
            }
        }

        private void Update()
        {
            if (isRecording)
            {
                time += Time.deltaTime;
                progressBar.fillAmount = time / duration;

                if (time >= duration)
                {
                    time = 0;
                    isRecording = false;
                    EndRecording();
                }
            }
        }

        // Public methods to control Whisper UI and recording
        public void ShowWhisperUI()
        {
            if (whisperUI != null)
                whisperUI.SetActive(true);
        }

        public void HideWhisperUI()
        {
            if (whisperUI != null)
                whisperUI.SetActive(false);
        }

        public void EnableRecording()
        {
            canRecord = true;
            if (recordButton != null)
                recordButton.interactable = true;
        }

        public void DisableRecording()
        {
            canRecord = false;
            if (recordButton != null)
                recordButton.interactable = false;

            // If currently recording, stop it
            if (isRecording)
            {
                time = 0;
                isRecording = false;
                EndRecording();
            }
        }
    }
}
