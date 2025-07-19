using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Manages agent reset functionality
/// Provides UI button to reset all agents in the scene
/// </summary>
public class AgentResetManager : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Button btnResetAgents;
    [SerializeField] private TextMeshProUGUI txtResetButton;

    [Header("Reset Settings")]
    [SerializeField] private bool blnShowDebugLogs = true;
    [SerializeField] private string strResetButtonText = "Reset Agents";
    [SerializeField] private string strResettingText = "Resetting...";

    private List<PushAgentBasic> lstAgents = new List<PushAgentBasic>();

    private void Start()
    {
        InitializeResetManager();
    }

    private void Update()
    {
        // Keyboard shortcut: Press R to reset agents
        if (Input.GetKeyDown(KeyCode.R))
        {
            OnResetButtonClicked();
        }
    }

    /// <summary>
    /// Initialize the reset manager
    /// </summary>
    private void InitializeResetManager()
    {
        // Find all agents in the scene
        FindAllAgents();

        // Setup UI button
        if (btnResetAgents != null)
        {
            btnResetAgents.onClick.AddListener(OnResetButtonClicked);
        }

        if (txtResetButton != null)
        {
            txtResetButton.text = strResetButtonText;
        }

        if (blnShowDebugLogs)
        {
            Debug.Log($"AgentResetManager: Found {lstAgents.Count} agents in scene");
        }
    }

    /// <summary>
    /// Find all agents in the scene
    /// </summary>
    private void FindAllAgents()
    {
        lstAgents.Clear();
        PushAgentBasic[] agents = FindObjectsByType<PushAgentBasic>(FindObjectsSortMode.None);
        lstAgents.AddRange(agents);
    }

    /// <summary>
    /// Handle reset button click
    /// </summary>
    private void OnResetButtonClicked()
    {
        if (blnShowDebugLogs)
        {
            Debug.Log("AgentResetManager: Reset button clicked");
        }

        // Update button text
        if (txtResetButton != null)
        {
            txtResetButton.text = strResettingText;
        }

        // Disable button during reset
        if (btnResetAgents != null)
        {
            btnResetAgents.interactable = false;
        }

        // Reset all agents
        ResetAllAgents();

        // Re-enable button after a short delay
        Invoke(nameof(ReenableResetButton), 1f);
    }

    /// <summary>
    /// Reset all agents in the scene
    /// </summary>
    private void ResetAllAgents()
    {
        if (blnShowDebugLogs)
        {
            Debug.Log($"AgentResetManager: Resetting {lstAgents.Count} agents");
        }

        foreach (PushAgentBasic agent in lstAgents)
        {
            if (agent != null)
            {
                // Force end current episode and start new one
                agent.EndEpisode();

                if (blnShowDebugLogs)
                {
                    Debug.Log($"AgentResetManager: Reset agent {agent.name}");
                }
            }
        }
    }

    /// <summary>
    /// Re-enable the reset button
    /// </summary>
    private void ReenableResetButton()
    {
        if (btnResetAgents != null)
        {
            btnResetAgents.interactable = true;
        }

        if (txtResetButton != null)
        {
            txtResetButton.text = strResetButtonText;
        }

        if (blnShowDebugLogs)
        {
            Debug.Log("AgentResetManager: Reset completed");
        }
    }

    /// <summary>
    /// Manually reset agents (can be called from other scripts)
    /// </summary>
    public void ResetAgents()
    {
        OnResetButtonClicked();
    }

    /// <summary>
    /// Refresh agent list (useful if agents are spawned dynamically)
    /// </summary>
    public void RefreshAgentList()
    {
        FindAllAgents();

        if (blnShowDebugLogs)
        {
            Debug.Log($"AgentResetManager: Refreshed agent list, found {lstAgents.Count} agents");
        }
    }

    private void OnDestroy()
    {
        // Clean up button listener
        if (btnResetAgents != null)
        {
            btnResetAgents.onClick.RemoveListener(OnResetButtonClicked);
        }
    }
}