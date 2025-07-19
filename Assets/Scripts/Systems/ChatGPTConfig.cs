using UnityEngine;

/// <summary>
/// ScriptableObject for ChatGPT configuration settings
/// Centralizes all ChatGPT-related settings
/// </summary>
[CreateAssetMenu(fileName = "ChatGPTConfig", menuName = "AI Training/ChatGPT Config")]
public class ChatGPTConfig : ScriptableObject
{
    [Header("API Settings")]
    [SerializeField] private string strApiKey = "";
    [SerializeField] private string strModel = "deepseek/deepseek-chat-v3-0324:free";
    [SerializeField] private int intMaxTokens = 400;
    [SerializeField] private float fltTemperature = 0.7f;

    [Header("OpenRouter Settings")]
    [SerializeField] private bool blnUseOpenRouter = true;
    [SerializeField] private string strBaseUrl = "https://openrouter.ai/api/v1";
    [SerializeField] private string strSiteUrl = "https://your-site.com";
    [SerializeField] private string strSiteName = "AI Training Game";

    [Header("Conversation Settings")]
    [SerializeField] private int intMaxConversationLength = 10;
    [SerializeField] private bool blnIncludeSystemMessage = true;
    [SerializeField] private string strSystemMessage = "You are a helpful assistant.";

    [Header("UI Settings")]
    [SerializeField] private float fltResponseTimeout = 30f;
    [SerializeField] private bool blnShowDebugLogs = false;

    // Properties
    public string ApiKey => strApiKey;
    public string Model => strModel;
    public int MaxTokens => intMaxTokens;
    public float Temperature => fltTemperature;
    public bool UseOpenRouter => blnUseOpenRouter;
    public string BaseUrl => strBaseUrl;
    public string SiteUrl => strSiteUrl;
    public string SiteName => strSiteName;
    public int MaxConversationLength => intMaxConversationLength;
    public bool IncludeSystemMessage => blnIncludeSystemMessage;
    public string SystemMessage => strSystemMessage;
    public float ResponseTimeout => fltResponseTimeout;
    public bool ShowDebugLogs => blnShowDebugLogs;

    /// <summary>
    /// Validate configuration settings
    /// </summary>
    /// <returns>True if configuration is valid</returns>
    public bool IsValid()
    {
        if (string.IsNullOrEmpty(strApiKey))
        {
            Debug.LogError("ChatGPT Config: API Key is missing! Please set your OpenRouter API key.");
            return false;
        }

        if (string.IsNullOrEmpty(strModel))
        {
            Debug.LogError("ChatGPT Config: Model is not specified!");
            return false;
        }

        if (intMaxTokens <= 0 || intMaxTokens > 4000)
        {
            Debug.LogError("ChatGPT Config: Max tokens must be between 1 and 4000!");
            return false;
        }

        if (fltTemperature < 0f || fltTemperature > 2f)
        {
            Debug.LogError("ChatGPT Config: Temperature must be between 0 and 2!");
            return false;
        }

        if (blnUseOpenRouter && string.IsNullOrEmpty(strBaseUrl))
        {
            Debug.LogError("ChatGPT Config: Base URL is required for OpenRouter!");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Get configuration summary for debugging
    /// </summary>
    /// <returns>Configuration summary string</returns>
    public string GetConfigSummary()
    {
        string strProvider = blnUseOpenRouter ? "OpenRouter" : "OpenAI";
        return $"Provider: {strProvider}, Model: {strModel}, Max Tokens: {intMaxTokens}, Temperature: {fltTemperature}, Max Conversation Length: {intMaxConversationLength}";
    }
}