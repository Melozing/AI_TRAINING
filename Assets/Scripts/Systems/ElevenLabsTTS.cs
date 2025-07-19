using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using System.Text.RegularExpressions;
using System;



/// <summary>
/// Manages ElevenLabs Text-to-Speech functionality
/// Handles TTS requests and audio playback using UnityWebRequest
/// </summary>
public class ElevenLabsTTS : MonoBehaviour
{
    [Header("ElevenLabs Configuration")]
    [SerializeField] private string strApiKey = "sk_dd857dd87621d238aff57cfd9f13c577da8dc41f223f79fb";
    [SerializeField] private string strVoiceId;
    [SerializeField] private string strModelId = "eleven_flash_v2_5";

    [Header("Audio Settings")]
    [SerializeField] private AudioSource objAudioSource;
    [SerializeField] private float fltVolume = 1f;

    [Header("TTS Settings")]
    [SerializeField] private bool blnEnableTTS = true;
    [SerializeField] private int intMaxTextLength = 500;

    [Header("Error Handling")]
    [SerializeField] private bool blnShowDebugLogs = true;

    // Private fields
    private bool blnIsProcessing = false;
    private Queue<string> lstTTSQueue = new Queue<string>();
    private Coroutine objTTSQueueCoroutine;

    // Events
    public static event Action<string> OnTTSStarted;
    public static event Action<string> OnTTSCompleted;
    public static event Action<string> OnTTSError;

    private void Awake()
    {
        InitializeElevenLabs();
    }

    /// <summary>
    /// Initialize ElevenLabs TTS system
    /// </summary>
    private void InitializeElevenLabs()
    {
        if (!blnEnableTTS)
        {
            if (blnShowDebugLogs)
                Debug.Log("ElevenLabsTTS: TTS is disabled");
            return;
        }

        try
        {
            // Auto-assign AudioSource if not set
            if (objAudioSource == null)
            {
                objAudioSource = GetComponent<AudioSource>();
                if (objAudioSource == null)
                {
                    objAudioSource = gameObject.AddComponent<AudioSource>();
                }
            }

            // Setup AudioSource
            objAudioSource.volume = fltVolume;
            objAudioSource.playOnAwake = false;

            // Ensure AudioListener exists
            if (FindFirstObjectByType<AudioListener>() == null)
            {
                Debug.LogWarning("ElevenLabsTTS: No AudioListener found in scene. Adding one to Main Camera.");
                var objMainCamera = Camera.main;
                if (objMainCamera != null)
                {
                    objMainCamera.gameObject.AddComponent<AudioListener>();
                }
                else
                {
                    Debug.LogError("ElevenLabsTTS: No Main Camera found. Please add AudioListener manually.");
                }
            }

            if (blnShowDebugLogs)
            {
                Debug.Log($"ElevenLabsTTS: Initialized successfully with voice ID: {strVoiceId}");
            }

            // Test connection disabled to avoid unwanted audio when player meets NPC
            // if (blnTestConnection)
            // {
            //     StartCoroutine(TestElevenLabsConnection());
            // }
        }
        catch (Exception ex)
        {
            Debug.LogError($"ElevenLabsTTS: Failed to initialize: {ex.Message}");
        }
    }

    /// <summary>
    /// Convert text to speech and play audio
    /// </summary>
    /// <param name="strText">Text to convert to speech</param>
    public void SpeakText(string strText)
    {
        if (!blnEnableTTS || string.IsNullOrEmpty(strText))
            return;

        // Truncate text if too long
        if (strText.Length > intMaxTextLength)
        {
            strText = strText.Substring(0, intMaxTextLength) + "...";
        }

        // Add to queue
        lstTTSQueue.Enqueue(strText);

        // Start queue processing if not already running
        if (objTTSQueueCoroutine == null)
        {
            objTTSQueueCoroutine = StartCoroutine(ProcessTTSQueue());
        }
    }

    /// <summary>
    /// Process TTS queue
    /// </summary>
    private IEnumerator ProcessTTSQueue()
    {
        while (lstTTSQueue.Count > 0)
        {
            string strText = lstTTSQueue.Dequeue();

            if (blnIsProcessing)
            {
                // Wait for current TTS to finish
                yield return new WaitForSeconds(0.1f);
                continue;
            }

            blnIsProcessing = true;

            // Trigger TTS started event
            OnTTSStarted?.Invoke(strText);

            if (blnShowDebugLogs)
            {
                Debug.Log($"ElevenLabsTTS: Converting text to speech: {strText.Substring(0, Mathf.Min(50, strText.Length))}...");
            }

            // Convert text to speech
            yield return StartCoroutine(GenerateAndStreamAudio(strText));

            blnIsProcessing = false;
        }

        objTTSQueueCoroutine = null;
    }

    /// <summary>
    /// Generate and stream audio using UnityWebRequest
    /// </summary>
    /// <param name="strText">Text to convert</param>
    private IEnumerator GenerateAndStreamAudio(string strText)
    {
        // Auto-select best voice for Vietnamese if not set
        string strSelectedVoiceId = GetBestVoiceForVietnamese();
        string strUrl = $"https://api.elevenlabs.io/v1/text-to-speech/{strSelectedVoiceId}";

        // Clean text to remove invalid characters
        string strCleanText = CleanTextForJSON(strText);

        // Create JSON payload manually to ensure proper formatting
        string strJson = $"{{\"text\":\"{strCleanText}\",\"model_id\":\"{strModelId}\"}}";

        // Validate JSON structure
        if (!strJson.Contains("\"text\":") || !strJson.Contains("\"model_id\":"))
        {
            Debug.LogError($"ElevenLabsTTS: Invalid JSON structure: {strJson}");
            OnTTSError?.Invoke("Invalid JSON structure");
            yield break;
        }

        if (blnShowDebugLogs)
        {
            Debug.Log($"ElevenLabsTTS: Sending request to {strUrl}");
            Debug.Log($"ElevenLabsTTS: Original text: {strText}");
            Debug.Log($"ElevenLabsTTS: Cleaned text: {strCleanText}");
            Debug.Log($"ElevenLabsTTS: Request data: {strJson}");
        }

        UnityWebRequest objRequest = new UnityWebRequest(strUrl, "POST");
        byte[] arrBodyRaw = System.Text.Encoding.UTF8.GetBytes(strJson);
        objRequest.uploadHandler = new UploadHandlerRaw(arrBodyRaw);
        objRequest.downloadHandler = new DownloadHandlerBuffer();

        objRequest.SetRequestHeader("Content-Type", "application/json");
        objRequest.SetRequestHeader("xi-api-key", strApiKey);

        yield return objRequest.SendWebRequest();

        if (objRequest.result == UnityWebRequest.Result.Success)
        {
            // Create AudioClip from bytes like the reference code
            AudioClip objAudioClip = CreateAudioClipFromBytes(objRequest.downloadHandler.data);

            if (objAudioClip != null)
            {
                if (blnShowDebugLogs)
                {
                    Debug.Log($"ElevenLabsTTS: Received audio clip with length: {objAudioClip.length}s");
                }

                // Play audio
                PlayAudio(objAudioClip);

                // Wait for audio to finish
                yield return new WaitForSeconds(objAudioClip.length);

                // Trigger completion event
                OnTTSCompleted?.Invoke(strText);

                if (blnShowDebugLogs)
                {
                    Debug.Log("ElevenLabsTTS: Audio playback completed");
                }
            }
            else
            {
                Debug.LogError("ElevenLabsTTS: Failed to create audio clip from bytes");
                OnTTSError?.Invoke("Failed to create audio clip from bytes");
            }
        }
        else
        {
            Debug.LogError($"ElevenLabsTTS: Error: {objRequest.error}");
            Debug.LogError($"Response Code: {objRequest.responseCode}");
            Debug.LogError($"Response: {objRequest.downloadHandler.text}");
            OnTTSError?.Invoke($"Network Error: {objRequest.error}");
        }
    }

    /// <summary>
    /// Play audio clip
    /// </summary>
    /// <param name="objAudioClip">Audio clip to play</param>
    private void PlayAudio(AudioClip objAudioClip)
    {
        if (objAudioSource != null)
        {
            objAudioSource.PlayOneShot(objAudioClip);
        }
    }

    /// <summary>
    /// Stop current TTS and clear queue
    /// </summary>
    public void StopTTS()
    {
        if (objAudioSource != null && objAudioSource.isPlaying)
        {
            objAudioSource.Stop();
        }

        lstTTSQueue.Clear();

        if (objTTSQueueCoroutine != null)
        {
            StopCoroutine(objTTSQueueCoroutine);
            objTTSQueueCoroutine = null;
        }

        blnIsProcessing = false;
    }

    /// <summary>
    /// Set voice ID
    /// </summary>
    /// <param name="strNewVoiceId">New voice ID</param>
    public void SetVoiceId(string strNewVoiceId)
    {
        strVoiceId = strNewVoiceId;
        if (blnShowDebugLogs)
        {
            Debug.Log($"ElevenLabsTTS: Voice ID changed to: {strVoiceId}");
        }
    }

    /// <summary>
    /// Set volume
    /// </summary>
    /// <param name="fltNewVolume">New volume (0-1)</param>
    public void SetVolume(float fltNewVolume)
    {
        fltVolume = Mathf.Clamp01(fltNewVolume);
        if (objAudioSource != null)
        {
            objAudioSource.volume = fltVolume;
        }
    }



    /// <summary>
    /// Get the best voice ID for Vietnamese
    /// </summary>
    /// <returns>Best voice ID for Vietnamese</returns>
    private string GetBestVoiceForVietnamese()
    {
        // Force use the best voice for Vietnamese regardless of user setting
        string strBestVoice = "pNInz6obpgDQGcFmaJgB"; // Adam - Excellent for Vietnamese

        if (blnShowDebugLogs)
        {
            Debug.Log($"ElevenLabsTTS: Using best voice for Vietnamese: {strBestVoice}");
            if (!string.IsNullOrEmpty(strVoiceId) && strVoiceId != strBestVoice)
            {
                Debug.Log($"ElevenLabsTTS: Overriding user voice {strVoiceId} with best Vietnamese voice {strBestVoice}");
            }
        }

        return strBestVoice;
    }

    /// <summary>
    /// Create AudioClip from bytes like the reference code
    /// </summary>
    /// <param name="audioData">Audio data bytes</param>
    /// <returns>AudioClip or null if failed</returns>
    private AudioClip CreateAudioClipFromBytes(byte[] audioData)
    {
        if (audioData == null || audioData.Length == 0)
        {
            Debug.LogError("ElevenLabsTTS: Audio data is null or empty");
            return null;
        }

        // ElevenLabs trả về MP3, cần chuyển đổi
        // Đơn giản nhất là lưu file tạm và load
        string tempPath = Application.temporaryCachePath + "/tts_audio.mp3";

        try
        {
            System.IO.File.WriteAllBytes(tempPath, audioData);

            if (blnShowDebugLogs)
            {
                Debug.Log($"ElevenLabsTTS: Saved audio data to {tempPath}, size: {audioData.Length} bytes");
            }

            // Load audio file using UnityWebRequest
            UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + tempPath, AudioType.MPEG);
            www.SendWebRequest();

            while (!www.isDone) { }

            if (www.result == UnityWebRequest.Result.Success)
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);

                if (blnShowDebugLogs)
                {
                    Debug.Log($"ElevenLabsTTS: Successfully created AudioClip with length: {clip.length}s");
                }

                return clip;
            }
            else
            {
                Debug.LogError($"ElevenLabsTTS: Error loading audio: {www.error}");
                return null;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"ElevenLabsTTS: Exception creating audio clip: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Clean text for JSON to remove invalid characters
    /// </summary>
    /// <param name="strText">Text to clean</param>
    /// <returns>Cleaned text</returns>
    private string CleanTextForJSON(string strText)
    {
        if (string.IsNullOrEmpty(strText))
            return "";

        // Remove or replace invalid control characters
        string strCleaned = strText
            .Replace("\r", " ") // Replace carriage return with space
            .Replace("\n", " ") // Replace newline with space
            .Replace("\t", " ") // Replace tab with space
            .Replace("\b", " ") // Replace backspace with space
            .Replace("\f", " ") // Replace form feed with space
            .Replace("\"", "'") // Replace double quotes with single quotes
            .Replace("\\", "/") // Replace backslash with forward slash
            .Replace("&", "and") // Replace ampersand
            .Replace("<", " ") // Replace less than
            .Replace(">", " "); // Replace greater than

        // Remove any remaining control characters
        strCleaned = Regex.Replace(strCleaned, @"[\x00-\x1F\x7F]", " ");

        // Remove multiple spaces
        strCleaned = Regex.Replace(strCleaned, @"\s+", " ");

        // Trim whitespace
        strCleaned = strCleaned.Trim();

        // Ensure text is not empty after cleaning
        if (string.IsNullOrEmpty(strCleaned))
        {
            strCleaned = "Hello"; // Fallback text
            if (blnShowDebugLogs)
            {
                Debug.LogWarning("ElevenLabsTTS: Text was empty after cleaning, using fallback");
            }
        }

        // Limit length to prevent API issues
        if (strCleaned.Length > intMaxTextLength)
        {
            strCleaned = strCleaned.Substring(0, intMaxTextLength);
            if (blnShowDebugLogs)
            {
                Debug.LogWarning($"ElevenLabsTTS: Text truncated to {intMaxTextLength} characters");
            }
        }

        return strCleaned;
    }

    /// <summary>
    /// Test ElevenLabs connection
    /// </summary>
    private IEnumerator TestElevenLabsConnection()
    {
        yield return new WaitForSeconds(1f); // Wait a bit after initialization

        if (blnShowDebugLogs)
        {
            Debug.Log("ElevenLabsTTS: Testing connection...");
        }

        // Test with a simple request
        yield return StartCoroutine(GenerateAndStreamAudio("Test"));
    }
}