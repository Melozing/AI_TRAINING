using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages NPC chat interactions with ChatGPT
/// Handles trigger detection and chat UI display
/// </summary>
public class NPCChatSystem : MonoBehaviour
{
    [Header("NPC Settings")]
    [SerializeField] private string strNPCName = "Heimerdinger";
    [SerializeField] private string strNPCPersonality = "Bạn là Heimerdinger từ League of Legends. Trả lời ngắn gọn, thông minh và hài hước bằng tiếng Việt. Giữ câu trả lời dưới 300 từ. Không có icon hoặc ký tự đặc biệt, chỉ nói nội dung hội thoại câu trả lời, không nói gì thêm, không viết các từ thể hiện biểu cảm, người hỏi là yasuo tỏng league of legend";

    [Header("UI Components")]
    [SerializeField] private GameObject objChatPanel;
    [SerializeField] private GameObject objInputContainer;
    [SerializeField] private TMP_InputField ipfPlayerInput;
    [SerializeField] private Button btnSendMessage;
    [SerializeField] private TextMeshProUGUI txtNPCReply;
    [SerializeField] private TextMeshProUGUI txtNPCName;

    [Header("ChatGPT Reference")]
    [SerializeField] private ChatGPTManager objChatGPTManager;

    [Header("TTS Reference")]
    [SerializeField] private ElevenLabsTTS objElevenLabsTTS;
    [SerializeField] private bool blnEnableTTS = true;

    [Header("Animation")]
    [SerializeField] private Animator objNPCAnimator;
    [SerializeField] private string strTalkAnimationTrigger = "Talk";
    [SerializeField] private string strIdleAnimationTrigger = "Idle";

    [Header("Trigger Settings")]
    [SerializeField] private float fltTriggerRadius = 3f;
    [SerializeField] private LayerMask objPlayerLayerMask = 1;

    private bool blnPlayerInRange = false;
    private bool blnIsChatting = false;
    private bool blnIsNPCSpeaking = false; // Track if NPC is currently speaking
    private bool blnIsNPCThinking = false; // Track if NPC is currently thinking

    private void Start()
    {
        InitializeNPCChat();
    }

    private void Update()
    {
        CheckPlayerProximity();
        HandleInput();
    }

    /// <summary>
    /// Initialize NPC chat system
    /// </summary>
    private void InitializeNPCChat()
    {
        // Auto-assign ChatGPTManager if not set
        if (objChatGPTManager == null)
            objChatGPTManager = FindFirstObjectByType<ChatGPTManager>();

        // Auto-assign NPC Animator if not set
        if (objNPCAnimator == null)
            objNPCAnimator = GetComponent<Animator>();

        // Setup UI - Hide everything initially
        if (objChatPanel != null)
            objChatPanel.SetActive(false);

        if (objInputContainer != null)
            objInputContainer.SetActive(false);

        if (ipfPlayerInput != null)
        {
            ipfPlayerInput.onSubmit.AddListener(OnInputSubmitted);
            ipfPlayerInput.onValueChanged.AddListener(OnInputValueChanged);
        }

        if (btnSendMessage != null)
        {
            btnSendMessage.onClick.AddListener(OnSendButtonClicked);
        }

        if (txtNPCName != null)
            txtNPCName.text = strNPCName;

        // Subscribe to ChatGPT events
        if (objChatGPTManager != null)
        {
            ChatGPTManager.OnResponseReceived += OnChatGPTResponse;
            ChatGPTManager.OnErrorOccurred += OnChatGPTError;
        }

        // Auto-assign ElevenLabs TTS if not set
        if (objElevenLabsTTS == null)
            objElevenLabsTTS = FindFirstObjectByType<ElevenLabsTTS>();

        // Subscribe to TTS events for animation control
        if (objElevenLabsTTS != null)
        {
            ElevenLabsTTS.OnTTSCompleted += OnTTSCompleted;
            ElevenLabsTTS.OnTTSError += OnTTSError;
        }

        // STT system removed
    }

    /// <summary>
    /// Check if player is within trigger range
    /// </summary>
    private void CheckPlayerProximity()
    {
        Collider[] objColliders = Physics.OverlapSphere(transform.position, fltTriggerRadius, objPlayerLayerMask);

        bool blnWasInRange = blnPlayerInRange;
        blnPlayerInRange = objColliders.Length > 0;

        // If NPC is currently responding (speaking or thinking), don't change UI state
        if (blnIsNPCSpeaking || blnIsNPCThinking)
        {
            return;
        }

        // Show/hide chat panel based on proximity (only when NPC is not responding)
        if (blnPlayerInRange && !blnWasInRange)
        {
            ShowChatPanel();
        }
        else if (!blnPlayerInRange && blnWasInRange)
        {
            // Safe to hide since NPC is not responding
            HideChatPanel();
        }
    }

    /// <summary>
    /// Handle keyboard input
    /// </summary>
    private void HandleInput()
    {
        if (!blnPlayerInRange) return;

        // Press Escape to close chat
        if (Input.GetKeyDown(KeyCode.Escape) && blnIsChatting)
        {
            CloseChat();
        }
    }

    /// <summary>
    /// Show chat panel when player is in range
    /// </summary>
    private void ShowChatPanel()
    {
        if (objChatPanel != null)
        {
            objChatPanel.SetActive(true);
        }

        // Show input container immediately when player is in range
        if (objInputContainer != null)
        {
            objInputContainer.SetActive(true);
        }

        // Focus on input field
        if (ipfPlayerInput != null)
        {
            ipfPlayerInput.Select();
        }

        // Play NPC approach sound
        if (SFXManager.Instance != null)
        {
            SFXManager.Instance.PlayNPCApproach();
        }

        // Start chat
        StartChat();
    }

    /// <summary>
    /// Hide chat panel when player leaves range
    /// </summary>
    private void HideChatPanel()
    {
        if (objChatPanel != null)
        {
            objChatPanel.SetActive(false);
        }

        // Hide input container
        if (objInputContainer != null)
        {
            objInputContainer.SetActive(false);
        }

        // Play NPC close sound
        if (SFXManager.Instance != null)
        {
            SFXManager.Instance.PlayNPCClose();
        }

        if (blnIsChatting)
        {
            CloseChat();
        }
    }

    /// <summary>
    /// Start chat interaction
    /// </summary>
    private void StartChat()
    {
        blnIsChatting = true;

        txtNPCReply.text = "Xin chào! Tôi là " + strNPCName + ". Bạn muốn hỏi gì?";
    }

    /// <summary>
    /// Close chat interaction
    /// </summary>
    private void CloseChat()
    {
        blnIsChatting = false;

        if (ipfPlayerInput != null)
        {
            ipfPlayerInput.text = "";
        }

        if (objInputContainer != null)
        {
            objInputContainer.SetActive(false);
        }

        // Play NPC close sound if not already played in HideChatPanel
        if (blnPlayerInRange && SFXManager.Instance != null)
        {
            SFXManager.Instance.PlayNPCClose();
        }
    }

    /// <summary>
    /// Handle send button click
    /// </summary>
    private void OnSendButtonClicked()
    {
        // Play button click sound
        if (SFXManager.Instance != null)
        {
            SFXManager.Instance.PlayButtonClick();
        }

        SendPlayerMessage();
    }

    /// <summary>
    /// Handle input field submission (Enter key)
    /// </summary>
    /// <param name="strMessage">Submitted message</param>
    private void OnInputSubmitted(string strMessage)
    {
        SendPlayerMessage();
    }

    /// <summary>
    /// Handle input field value changes
    /// </summary>
    /// <param name="strValue">Current input value</param>
    private void OnInputValueChanged(string strValue)
    {
        if (btnSendMessage != null)
        {
            btnSendMessage.interactable = !string.IsNullOrEmpty(strValue) && !objChatGPTManager.IsProcessing();
        }
    }

    // STT methods removed

    /// <summary>
    /// Send player message to ChatGPT
    /// </summary>
    private void SendPlayerMessage()
    {
        if (objChatGPTManager == null || ipfPlayerInput == null) return;

        string strMessage = ipfPlayerInput.text.Trim();
        if (string.IsNullOrEmpty(strMessage)) return;

        // Create personality prompt
        string strFullMessage = $"{strNPCPersonality}\n\nNgười chơi hỏi: {strMessage}";

        // Set NPC thinking state
        blnIsNPCThinking = true;
        Debug.Log("NPCChatSystem: NPC started thinking - UI locked");

        // Send to ChatGPT
        objChatGPTManager.SendChatMessage(strFullMessage);

        // Clear input and show loading
        ipfPlayerInput.text = "";
        txtNPCReply.text = $"{strNPCName} đang suy nghĩ...";

        // Disable input while processing
        if (ipfPlayerInput != null)
            ipfPlayerInput.interactable = false;
        if (btnSendMessage != null)
            btnSendMessage.interactable = false;

        // Play thinking sound
        if (SFXManager.Instance != null)
        {
            SFXManager.Instance.PlayNPCThinking();
        }
    }

    /// <summary>
    /// Handle ChatGPT response
    /// </summary>
    /// <param name="strResponse">ChatGPT response</param>
    private void OnChatGPTResponse(string strResponse)
    {
        if (txtNPCReply != null)
        {
            txtNPCReply.text = $"{strNPCName}: {strResponse}";
        }

        // Reset thinking state and set speaking state
        blnIsNPCThinking = false;
        blnIsNPCSpeaking = true;
        Debug.Log("NPCChatSystem: NPC started speaking - UI remains locked");

        // Stop thinking sound when NPC starts speaking
        if (SFXManager.Instance != null)
        {
            SFXManager.Instance.StopNPCThinking();
        }

        // Start talking animation
        StartTalkingAnimation();

        // Disable input while NPC is speaking
        if (ipfPlayerInput != null)
            ipfPlayerInput.interactable = false;
        if (btnSendMessage != null)
            btnSendMessage.interactable = false;

        // Play TTS if enabled
        if (blnEnableTTS && objElevenLabsTTS != null)
        {
            objElevenLabsTTS.SpeakText(strResponse);
        }
    }

    /// <summary>
    /// Handle ChatGPT error
    /// </summary>
    /// <param name="strError">Error message</param>
    private void OnChatGPTError(string strError)
    {
        if (txtNPCReply != null)
        {
            txtNPCReply.text = $"{strNPCName}: Xin lỗi, tôi gặp vấn đề kỹ thuật. Vui lòng thử lại sau.";
        }

        // Reset NPC states
        blnIsNPCSpeaking = false;
        blnIsNPCThinking = false;
        Debug.Log("NPCChatSystem: ChatGPT error - UI unlocked");

        // Stop thinking sound on error
        if (SFXManager.Instance != null)
        {
            SFXManager.Instance.StopNPCThinking();
        }

        // Re-enable input
        if (ipfPlayerInput != null)
            ipfPlayerInput.interactable = true;
        if (btnSendMessage != null)
            btnSendMessage.interactable = true;

        // Check if player is still in range, if not hide the panel
        if (!blnPlayerInRange)
        {
            HideChatPanel();
        }
    }

    /// <summary>
    /// Handle TTS completion
    /// </summary>
    /// <param name="strText">Text that was spoken</param>
    private void OnTTSCompleted(string strText)
    {
        // Stop talking animation when TTS finishes
        StopTalkingAnimation();

        // Reset NPC speaking state
        blnIsNPCSpeaking = false;

        // Re-enable input after NPC finishes speaking
        if (ipfPlayerInput != null)
            ipfPlayerInput.interactable = true;
        if (btnSendMessage != null)
            btnSendMessage.interactable = true;

        // Check if player is still in range, if not hide the panel
        if (!blnPlayerInRange && !blnIsNPCThinking)
        {
            HideChatPanel();
        }
    }

    /// <summary>
    /// Handle TTS error
    /// </summary>
    /// <param name="strError">Error message</param>
    private void OnTTSError(string strError)
    {
        // Stop talking animation on error
        StopTalkingAnimation();

        // Reset NPC speaking state
        blnIsNPCSpeaking = false;
        Debug.Log("NPCChatSystem: NPC speaking error - UI unlocked");

        // Re-enable input after error
        if (ipfPlayerInput != null)
            ipfPlayerInput.interactable = true;
        if (btnSendMessage != null)
            btnSendMessage.interactable = true;

        // Check if player is still in range, if not hide the panel
        if (!blnPlayerInRange && !blnIsNPCThinking)
        {
            HideChatPanel();
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        ChatGPTManager.OnResponseReceived -= OnChatGPTResponse;
        ChatGPTManager.OnErrorOccurred -= OnChatGPTError;

        // Unsubscribe from TTS events
        if (objElevenLabsTTS != null)
        {
            ElevenLabsTTS.OnTTSCompleted -= OnTTSCompleted;
            ElevenLabsTTS.OnTTSError -= OnTTSError;
        }

        // STT system removed

    }

    /// <summary>
    /// Start talking animation
    /// </summary>
    private void StartTalkingAnimation()
    {
        if (objNPCAnimator != null && !string.IsNullOrEmpty(strTalkAnimationTrigger))
        {
            objNPCAnimator.SetTrigger(strTalkAnimationTrigger);
        }
    }

    /// <summary>
    /// Stop talking animation and return to idle
    /// </summary>
    private void StopTalkingAnimation()
    {
        if (objNPCAnimator != null && !string.IsNullOrEmpty(strIdleAnimationTrigger))
        {
            objNPCAnimator.SetTrigger(strIdleAnimationTrigger);
        }
    }

    /// <summary>
    /// Check if player is currently chatting with NPC
    /// </summary>
    /// <returns>True if player is chatting</returns>
    public bool IsPlayerChatting()
    {
        return blnIsChatting || blnIsNPCSpeaking || blnIsNPCThinking;
    }

    /// <summary>
    /// Check if input field is currently focused/active
    /// </summary>
    /// <returns>True if input field is active and focused</returns>
    public bool IsInputFieldActive()
    {
        return blnPlayerInRange && objInputContainer != null && objInputContainer.activeInHierarchy;
    }

    /// <summary>
    /// Check if NPC is currently responding (speaking or thinking)
    /// </summary>
    /// <returns>True if NPC is responding</returns>
    public bool IsNPCResponding()
    {
        return blnIsNPCSpeaking || blnIsNPCThinking;
    }

    /// <summary>
    /// Check if NPC is currently speaking (TTS playing)
    /// </summary>
    /// <returns>True if NPC is speaking</returns>
    public bool IsNPCSpeaking()
    {
        return blnIsNPCSpeaking;
    }

    /// <summary>
    /// Check if NPC is currently thinking (waiting for ChatGPT response)
    /// </summary>
    /// <returns>True if NPC is thinking</returns>
    public bool IsNPCThinking()
    {
        return blnIsNPCThinking;
    }

    /// <summary>
    /// Force close chat UI (use with caution)
    /// </summary>
    public void ForceCloseChat()
    {
        blnIsNPCSpeaking = false;
        blnIsNPCThinking = false;
        HideChatPanel();
    }

    /// <summary>
    /// Draw trigger radius in editor
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, fltTriggerRadius);
    }
}