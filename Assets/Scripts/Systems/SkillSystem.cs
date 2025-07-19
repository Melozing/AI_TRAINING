using UnityEngine;
using System.Collections;

/// <summary>
/// Manages agent skills using Strategy pattern
/// Handles skill execution and cooldowns
/// </summary>
public class SkillSystem : MonoBehaviour
{
    [Header("Skill Settings")]
    [SerializeField] private float fltSkillQCooldown = 5f;
    [SerializeField] private float fltSkillRCooldown = 10f;

    private bool blnSkillQReady = true;
    private bool blnSkillRReady = true;

    // Events
    public System.Action OnSkillQExecuted;
    public System.Action OnSkillRExecuted;

    private void Start()
    {
        // Subscribe to input events
        InputManager.OnSkillQPressed += ExecuteSkillQ;
        InputManager.OnSkillRPressed += ExecuteSkillR;
    }

    private void OnDestroy()
    {
        // Unsubscribe from input events
        InputManager.OnSkillQPressed -= ExecuteSkillQ;
        InputManager.OnSkillRPressed -= ExecuteSkillR;
    }

    /// <summary>
    /// Execute Skill Q
    /// </summary>
    private void ExecuteSkillQ()
    {
        if (!blnSkillQReady) return;

        // Trigger event for animation
        OnSkillQExecuted?.Invoke();

        // Start cooldown
        StartCoroutine(SkillQCooldown());
    }

    /// <summary>
    /// Execute Skill R
    /// </summary>
    private void ExecuteSkillR()
    {
        if (!blnSkillRReady) return;

        // Trigger event for animation
        OnSkillRExecuted?.Invoke();

        // Start cooldown
        StartCoroutine(SkillRCooldown());
    }

    /// <summary>
    /// Skill Q cooldown coroutine
    /// </summary>
    private IEnumerator SkillQCooldown()
    {
        blnSkillQReady = false;
        yield return new WaitForSeconds(fltSkillQCooldown);
        blnSkillQReady = true;
    }

    /// <summary>
    /// Skill R cooldown coroutine
    /// </summary>
    private IEnumerator SkillRCooldown()
    {
        blnSkillRReady = false;
        yield return new WaitForSeconds(fltSkillRCooldown);
        blnSkillRReady = true;
    }

    /// <summary>
    /// Check if Skill Q is ready
    /// </summary>
    /// <returns>True if ready, false otherwise</returns>
    public bool IsSkillQReady()
    {
        return blnSkillQReady;
    }

    /// <summary>
    /// Check if Skill R is ready
    /// </summary>
    /// <returns>True if ready, false otherwise</returns>
    public bool IsSkillRReady()
    {
        return blnSkillRReady;
    }
}