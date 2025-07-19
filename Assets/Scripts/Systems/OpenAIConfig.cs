using UnityEngine;

/// <summary>
/// ScriptableObject để lưu OpenAI API configuration
/// </summary>
[CreateAssetMenu(fileName = "OpenAIConfig", menuName = "AI Training/OpenAI Config")]
public class OpenAIConfig : ScriptableObject
{
    [Header("OpenAI API Settings")]
    [SerializeField] private string strApiKey = "sk-your-openai-api-key-here";
    [SerializeField] private string strOrganization = "org-your-organization-id-here";

    [Header("Settings")]
    [SerializeField] private bool blnShowDebugLogs = true;

    /// <summary>
    /// Get API Key
    /// </summary>
    public string ApiKey => strApiKey;

    /// <summary>
    /// Get Organization ID
    /// </summary>
    public string Organization => strOrganization;

    /// <summary>
    /// Check if debug logs are enabled
    /// </summary>
    public bool ShowDebugLogs => blnShowDebugLogs;

    /// <summary>
    /// Validate configuration
    /// </summary>
    public bool IsValid()
    {
        bool isValid = !string.IsNullOrEmpty(strApiKey) && strApiKey.StartsWith("sk-");

        if (blnShowDebugLogs)
        {
            if (isValid)
            {
                Debug.Log("OpenAIConfig: Configuration is valid");
            }
            else
            {
                Debug.LogError("OpenAIConfig: Invalid API key. Must start with 'sk-'");
            }
        }

        return isValid;
    }

    /// <summary>
    /// Get masked API key for display (shows only first 7 and last 4 characters)
    /// </summary>
    public string GetMaskedApiKey()
    {
        if (string.IsNullOrEmpty(strApiKey) || strApiKey.Length < 11)
            return "Invalid";

        return $"{strApiKey.Substring(0, 7)}...{strApiKey.Substring(strApiKey.Length - 4)}";
    }
}