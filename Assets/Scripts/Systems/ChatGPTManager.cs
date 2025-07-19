using UnityEngine;
using OpenAI;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

/// <summary>
/// Manages ChatGPT API interactions and conversation history
/// Handles core logic for OpenAI API communication
/// </summary>
public class ChatGPTManager : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private ChatGPTConfig objConfig;
    [SerializeField] private string apiKey;

    [Header("Retry Settings")]
    [SerializeField] private int intMaxRetries = 3;
    [SerializeField] private float fltRetryDelay = 1f;
    [SerializeField] private bool blnEnableRetry = true;

    [Header("Events")]
    [SerializeField] private ChatGPTResponseEvent objOnResponseReceived;
    [SerializeField] private ChatGPTResponseEvent objOnErrorOccurred;

    // Events for other scripts to subscribe to
    public static event Action<string> OnResponseReceived;
    public static event Action<string> OnErrorOccurred;

    // Private fields
    private OpenAIApi objOpenAIApi;
    private HttpClient objHttpClient;
    private List<ChatMessage> lstConversationHistory;
    private bool blnIsProcessing = false;

    private void Awake()
    {
        InitializeChatGPT();
    }

    /// <summary>
    /// Initialize ChatGPT system
    /// </summary>
    private void InitializeChatGPT()
    {
        // Auto-assign config if not set
        if (objConfig == null)
        {
            objConfig = Resources.Load<ChatGPTConfig>("ChatGPTConfig");
            if (objConfig == null)
            {
                Debug.LogError("ChatGPTManager: No ChatGPTConfig found! Please create one in Resources folder.");
                return;
            }
        }

        // Validate configuration
        if (!objConfig.IsValid())
        {
            Debug.LogError("ChatGPTManager: Invalid configuration! Please check ChatGPTConfig settings.");
            return;
        }

        // Initialize based on provider
        if (objConfig.UseOpenRouter)
        {
            InitializeOpenRouter();
        }
        else
        {
            InitializeOpenAI();
        }

        lstConversationHistory = new List<ChatMessage>();

        if (objConfig.ShowDebugLogs)
        {
            Debug.Log($"ChatGPTManager: Initialized successfully. {objConfig.GetConfigSummary()}");
        }
    }

    /// <summary>
    /// Initialize OpenRouter API
    /// </summary>
    private void InitializeOpenRouter()
    {
        try
        {
            objHttpClient = new HttpClient();
            objHttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {objConfig.ApiKey}");
            objHttpClient.DefaultRequestHeaders.Add("HTTP-Referer", objConfig.SiteUrl);
            objHttpClient.DefaultRequestHeaders.Add("X-Title", objConfig.SiteName);

            if (objConfig.ShowDebugLogs)
            {
                Debug.Log($"ChatGPTManager: OpenRouter initialized with base URL: {objConfig.BaseUrl}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"ChatGPTManager: Failed to initialize OpenRouter: {ex.Message}");
        }
    }

    /// <summary>
    /// Initialize OpenAI API
    /// </summary>
    private void InitializeOpenAI()
    {
        try
        {
            // Hardcode your API key here
            string hardcodedApiKey = apiKey;

            // Create OpenAIApi with hardcoded API key
            objOpenAIApi = new OpenAIApi(hardcodedApiKey);

            if (objConfig.ShowDebugLogs)
            {
                Debug.Log($"ChatGPTManager: OpenAI API initialized with hardcoded key: {hardcodedApiKey.Substring(0, 7)}...");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"ChatGPTManager: Failed to initialize OpenAI API: {ex.Message}");
        }
    }

    /// <summary>
    /// Send chat message to ChatGPT
    /// </summary>
    /// <param name="strMessage">Message to send</param>
    public async void SendChatMessage(string strMessage)
    {
        if (blnIsProcessing)
        {
            Debug.LogWarning("ChatGPTManager: Already processing a request. Please wait.");
            return;
        }

        if (string.IsNullOrEmpty(strMessage) || strMessage.Trim().Length < 3)
        {
            Debug.LogWarning("ChatGPTManager: Message is too short! Minimum 3 characters required.");
            return;
        }

        blnIsProcessing = true;

        try
        {
            if (objConfig.UseOpenRouter)
            {
                await ProcessOpenRouterRequest(strMessage);
            }
            else
            {
                await ProcessOpenAIRequest(strMessage);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"ChatGPTManager: Error processing chat request: {ex.Message}");
            HandleError($"Error: {ex.Message}");
        }
        finally
        {
            blnIsProcessing = false;
        }
    }

    /// <summary>
    /// Process chat request with OpenRouter
    /// </summary>
    /// <param name="strMessage">User message</param>
    private async Task ProcessOpenRouterRequest(string strMessage)
    {
        if (objHttpClient == null)
        {
            HandleError("OpenRouter HTTP client not initialized");
            return;
        }

        // Add user message to history
        AddMessageToHistory("user", strMessage);

        // Create request payload with proper message format
        var lstMessages = new List<object>();
        foreach (var objMsg in lstConversationHistory)
        {
            lstMessages.Add(new
            {
                role = objMsg.Role,
                content = objMsg.Content
            });
        }

        var objRequest = new
        {
            model = objConfig.Model,
            messages = lstMessages,
            max_tokens = objConfig.MaxTokens,
            temperature = objConfig.Temperature
        };

        var strJson = JsonConvert.SerializeObject(objRequest);
        var objContent = new StringContent(strJson, Encoding.UTF8, "application/json");

        if (objConfig.ShowDebugLogs)
        {
            Debug.Log($"ChatGPTManager: Sending OpenRouter request with {lstConversationHistory.Count} messages");
        }

        // Send request with retry mechanism
        await SendRequestWithRetry(objContent);
    }

    /// <summary>
    /// Send request with retry mechanism
    /// </summary>
    /// <param name="objContent">Request content</param>
    private async Task SendRequestWithRetry(StringContent objContent)
    {
        int intRetryCount = 0;

        while (intRetryCount <= intMaxRetries)
        {
            try
            {
                // Send request
                var objResponse = await objHttpClient.PostAsync($"{objConfig.BaseUrl}/chat/completions", objContent);
                var strResponseContent = await objResponse.Content.ReadAsStringAsync();

                if (objConfig.ShowDebugLogs)
                {
                    Debug.Log($"ChatGPTManager: OpenRouter response status: {objResponse.StatusCode} (Attempt {intRetryCount + 1})");
                    Debug.Log($"ChatGPTManager: OpenRouter response content: {strResponseContent}");
                }

                if (objResponse.IsSuccessStatusCode)
                {
                    ProcessOpenRouterResponse(strResponseContent);
                    return; // Success, exit retry loop
                }
                else
                {
                    // Check if error is retryable
                    if (IsRetryableError(objResponse.StatusCode) && blnEnableRetry && intRetryCount < intMaxRetries)
                    {
                        intRetryCount++;
                        if (objConfig.ShowDebugLogs)
                        {
                            Debug.LogWarning($"ChatGPTManager: Retryable error, attempting retry {intRetryCount}/{intMaxRetries} in {fltRetryDelay} seconds...");
                        }

                        await Task.Delay((int)(fltRetryDelay * 1000)); // Convert to milliseconds
                        continue;
                    }
                    else
                    {
                        // Non-retryable error or max retries reached
                        HandleError($"OpenRouter API error: {objResponse.StatusCode} - {strResponseContent}");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                if (blnEnableRetry && intRetryCount < intMaxRetries)
                {
                    intRetryCount++;
                    if (objConfig.ShowDebugLogs)
                    {
                        Debug.LogWarning($"ChatGPTManager: Network error, attempting retry {intRetryCount}/{intMaxRetries} in {fltRetryDelay} seconds... Error: {ex.Message}");
                    }

                    await Task.Delay((int)(fltRetryDelay * 1000));
                    continue;
                }
                else
                {
                    HandleError($"Network error after {intRetryCount} retries: {ex.Message}");
                    return;
                }
            }
        }
    }

    /// <summary>
    /// Check if error is retryable
    /// </summary>
    /// <param name="objStatusCode">HTTP status code</param>
    /// <returns>True if error is retryable</returns>
    private bool IsRetryableError(System.Net.HttpStatusCode objStatusCode)
    {
        int intStatusCode = (int)objStatusCode;
        // Retry on server errors (5xx) and some client errors (429 - rate limit)
        return intStatusCode >= 500 || intStatusCode == 429;
    }

    /// <summary>
    /// Process OpenRouter response
    /// </summary>
    /// <param name="strResponseContent">Response JSON</param>
    private void ProcessOpenRouterResponse(string strResponseContent)
    {
        try
        {
            var objResponse = JsonConvert.DeserializeObject<OpenRouterResponse>(strResponseContent);

            if (objResponse?.choices == null || objResponse.choices.Length == 0)
            {
                HandleError("No response received from OpenRouter");
                return;
            }

            string strResponse = objResponse.choices[0].message.content;

            if (string.IsNullOrEmpty(strResponse))
            {
                HandleError("Empty response from OpenRouter");
                return;
            }

            // Add assistant response to history
            AddMessageToHistory("assistant", strResponse);

            // Trigger response events
            TriggerResponseEvents(strResponse);

            if (objConfig.ShowDebugLogs)
            {
                Debug.Log($"ChatGPTManager: Received OpenRouter response ({strResponse.Length} characters)");
            }
        }
        catch (Exception ex)
        {
            HandleError($"Failed to parse OpenRouter response: {ex.Message}");
        }
    }

    /// <summary>
    /// Process chat request with OpenAI
    /// </summary>
    /// <param name="strMessage">User message</param>
    private async Task ProcessOpenAIRequest(string strMessage)
    {
        if (objOpenAIApi == null)
        {
            HandleError("OpenAI API not initialized");
            return;
        }

        // Add user message to history
        AddMessageToHistory("user", strMessage);

        // Create chat request
        var objRequest = CreateChatRequest();

        if (objConfig.ShowDebugLogs)
        {
            Debug.Log($"ChatGPTManager: Sending OpenAI request with {objRequest.Messages.Count} messages");
        }

        // Send request to OpenAI
        var objResponse = await objOpenAIApi.CreateChatCompletion(objRequest);

        // Process response
        ProcessOpenAIResponse(objResponse);
    }

    /// <summary>
    /// Process OpenAI response
    /// </summary>
    /// <param name="objResponse">OpenAI response</param>
    private void ProcessOpenAIResponse(CreateChatCompletionResponse objResponse)
    {
        if (objResponse.Choices == null || objResponse.Choices.Count == 0)
        {
            HandleError("No response received from OpenAI");
            return;
        }

        string strResponse = objResponse.Choices[0].Message.Content;

        if (string.IsNullOrEmpty(strResponse))
        {
            HandleError("Empty response from OpenAI");
            return;
        }

        // Add assistant response to history
        AddMessageToHistory("assistant", strResponse);

        // Trigger response events
        TriggerResponseEvents(strResponse);

        if (objConfig.ShowDebugLogs)
        {
            Debug.Log($"ChatGPTManager: Received OpenAI response ({strResponse.Length} characters)");
        }
    }

    /// <summary>
    /// Add message to conversation history
    /// </summary>
    /// <param name="strRole">Message role (user/assistant/system)</param>
    /// <param name="strContent">Message content</param>
    private void AddMessageToHistory(string strRole, string strContent)
    {
        var objMessage = new ChatMessage
        {
            Role = strRole,
            Content = strContent
        };

        lstConversationHistory.Add(objMessage);

        // Limit conversation length
        if (lstConversationHistory.Count > objConfig.MaxConversationLength)
        {
            lstConversationHistory.RemoveAt(0);
        }
    }

    /// <summary>
    /// Create chat completion request for OpenAI
    /// </summary>
    /// <returns>Chat completion request</returns>
    private CreateChatCompletionRequest CreateChatRequest()
    {
        var lstMessages = new List<ChatMessage>();

        // Add system message if enabled
        if (objConfig.IncludeSystemMessage && !string.IsNullOrEmpty(objConfig.SystemMessage))
        {
            lstMessages.Add(new ChatMessage
            {
                Role = "system",
                Content = objConfig.SystemMessage
            });
        }

        // Add conversation history
        lstMessages.AddRange(lstConversationHistory);

        return new CreateChatCompletionRequest
        {
            Model = objConfig.Model,
            Messages = lstMessages,
            MaxTokens = objConfig.MaxTokens,
            Temperature = objConfig.Temperature
        };
    }

    /// <summary>
    /// Trigger response events
    /// </summary>
    /// <param name="strResponse">ChatGPT response</param>
    private void TriggerResponseEvents(string strResponse)
    {
        // Trigger UnityEvent
        if (objOnResponseReceived != null)
        {
            objOnResponseReceived.Invoke(strResponse);
        }

        // Trigger static event
        OnResponseReceived?.Invoke(strResponse);
    }

    /// <summary>
    /// Handle errors
    /// </summary>
    /// <param name="strError">Error message</param>
    private void HandleError(string strError)
    {
        Debug.LogError($"ChatGPTManager: {strError}");

        // Provide user-friendly error message
        string strUserFriendlyError = GetUserFriendlyError(strError);

        // Trigger error events
        if (objOnErrorOccurred != null)
        {
            objOnErrorOccurred.Invoke(strUserFriendlyError);
        }

        OnErrorOccurred?.Invoke(strUserFriendlyError);
    }

    /// <summary>
    /// Convert technical error to user-friendly message
    /// </summary>
    /// <param name="strTechnicalError">Technical error message</param>
    /// <returns>User-friendly error message</returns>
    private string GetUserFriendlyError(string strTechnicalError)
    {
        if (strTechnicalError.Contains("500") || strTechnicalError.Contains("Internal Server Error"))
        {
            return "Máy chủ đang bận, vui lòng thử lại sau.";
        }
        else if (strTechnicalError.Contains("429") || strTechnicalError.Contains("rate limit"))
        {
            return "Quá nhiều yêu cầu, vui lòng chờ một chút.";
        }
        else if (strTechnicalError.Contains("401") || strTechnicalError.Contains("unauthorized"))
        {
            return "Lỗi xác thực, vui lòng kiểm tra cấu hình.";
        }
        else if (strTechnicalError.Contains("Network error"))
        {
            return "Lỗi kết nối mạng, vui lòng kiểm tra internet.";
        }
        else
        {
            return "Có lỗi xảy ra, vui lòng thử lại.";
        }
    }

    /// <summary>
    /// Clear conversation history
    /// </summary>
    public void ClearConversation()
    {
        lstConversationHistory.Clear();

        if (objConfig.ShowDebugLogs)
        {
            Debug.Log("ChatGPTManager: Conversation history cleared");
        }
    }

    /// <summary>
    /// Get current conversation length
    /// </summary>
    /// <returns>Number of messages in conversation</returns>
    public int GetConversationLength()
    {
        return lstConversationHistory.Count;
    }

    /// <summary>
    /// Check if currently processing a request
    /// </summary>
    /// <returns>True if processing, false otherwise</returns>
    public bool IsProcessing()
    {
        return blnIsProcessing;
    }

    /// <summary>
    /// Backward compatibility method
    /// </summary>
    /// <param name="strMessage">Message to send</param>
    public new void SendMessage(string strMessage)
    {
        SendChatMessage(strMessage);
    }

    private void OnDestroy()
    {
        objHttpClient?.Dispose();
    }
}

/// <summary>
/// OpenRouter response model
/// </summary>
[System.Serializable]
public class OpenRouterResponse
{
    public OpenRouterChoice[] choices;
}

[System.Serializable]
public class OpenRouterChoice
{
    public OpenRouterMessage message;
}

[System.Serializable]
public class OpenRouterMessage
{
    public string content;
}

/// <summary>
/// UnityEvent for ChatGPT responses
/// </summary>
[System.Serializable]
public class ChatGPTResponseEvent : UnityEngine.Events.UnityEvent<string> { }