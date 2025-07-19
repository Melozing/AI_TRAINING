using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Simple script to attach to a UI button for resetting agents
/// </summary>
public class ResetAgentButton : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button btnReset;
    [SerializeField] private TextMeshProUGUI txtButtonText;

    [Header("Settings")]
    [SerializeField] private string strDefaultText = "Reset Agents";
    [SerializeField] private string strResettingText = "Resetting...";
    [SerializeField] private bool blnShowDebugLogs = true;

    private void Start()
    {
        SetupButton();
    }

    /// <summary>
    /// Setup the reset button
    /// </summary>
    private void SetupButton()
    {
        // Auto-assign button if not set
        if (btnReset == null)
            btnReset = GetComponent<Button>();

        // Auto-assign text if not set
        if (txtButtonText == null)
            txtButtonText = GetComponentInChildren<TextMeshProUGUI>();

        // Setup button listener
        if (btnReset != null)
        {
            btnReset.onClick.AddListener(ResetAgents);
        }

        // Set initial text
        if (txtButtonText != null)
        {
            txtButtonText.text = strDefaultText;
        }

        if (blnShowDebugLogs)
        {
            Debug.Log("ResetAgentButton: Button setup completed");
        }
    }

    /// <summary>
    /// Reset all agents in the scene
    /// </summary>
    public void ResetAgents()
    {
        if (blnShowDebugLogs)
        {
            Debug.Log("ResetAgentButton: Resetting all agents");
        }

        // Play button click sound
        if (SFXManager.Instance != null)
        {
            SFXManager.Instance.PlayButtonClick();
        }

        // Update button text
        if (txtButtonText != null)
        {
            txtButtonText.text = strResettingText;
        }

        // Disable button during reset
        if (btnReset != null)
        {
            btnReset.interactable = false;
        }

        // Find and reset all agents
        PushAgentBasic[] agents = FindObjectsByType<PushAgentBasic>(FindObjectsSortMode.None);

        foreach (PushAgentBasic agent in agents)
        {
            if (agent != null)
            {
                agent.EndEpisode();

                if (blnShowDebugLogs)
                {
                    Debug.Log($"ResetAgentButton: Reset agent {agent.name}");
                }
            }
        }

        // Re-enable button after delay
        Invoke(nameof(ReenableButton), 1f);
    }

    /// <summary>
    /// Re-enable the button
    /// </summary>
    private void ReenableButton()
    {
        if (btnReset != null)
        {
            btnReset.interactable = true;
        }

        if (txtButtonText != null)
        {
            txtButtonText.text = strDefaultText;
        }

        if (blnShowDebugLogs)
        {
            Debug.Log("ResetAgentButton: Reset completed");
        }
    }

    private void OnDestroy()
    {
        // Clean up button listener
        if (btnReset != null)
        {
            btnReset.onClick.RemoveListener(ResetAgents);
        }
    }
}