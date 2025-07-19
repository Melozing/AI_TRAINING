using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple script to add SFX to any button
/// </summary>
public class ButtonSFX : MonoBehaviour
{
    [SerializeField] private bool blnEnableSFX = true;

    private Button btnButton;

    private void Start()
    {
        // Get button component
        btnButton = GetComponent<Button>();

        if (btnButton != null)
        {
            // Add click listener
            btnButton.onClick.AddListener(OnButtonClick);
        }
    }

    /// <summary>
    /// Handle button click
    /// </summary>
    private void OnButtonClick()
    {
        if (blnEnableSFX && SFXManager.Instance != null)
        {
            SFXManager.Instance.PlayButtonClick();
        }
    }

    private void OnDestroy()
    {
        // Clean up listener
        if (btnButton != null)
        {
            btnButton.onClick.RemoveListener(OnButtonClick);
        }
    }
}