using UnityEngine;

/// <summary>
/// Manages player character animations using State pattern
/// Handles different animation states: Idle, Moving, SkillQ, SkillR
/// </summary>
public class PlayerAnimationController : MonoBehaviour
{
    [Header("Animation Settings")]
    public Animator animator;

    // Animation parameter names
    private readonly string SPEED_PARAM = "Speed";
    private readonly string SKILL_Q_PARAM = "SkillQ";
    private readonly string SKILL_R_PARAM = "SkillR";

    // Animation state tracking
    public enum AnimationState
    {
        Idle,
        Moving,
        SkillQ,
        SkillR
    }

    private AnimationState objCurrentState = AnimationState.Idle;
    private float fltCurrentSpeed = 0f;

    void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (animator == null)
        {
            Debug.LogError("PlayerAnimationController: Animator component not found!");
            enabled = false;
            return;
        }
    }

    /// <summary>
    /// Update movement animation based on speed
    /// </summary>
    /// <param name="fltSpeed">Movement speed (0-1 normalized)</param>
    public void UpdateMovementAnimation(float fltSpeed)
    {
        fltCurrentSpeed = fltSpeed;
        animator.SetFloat(SPEED_PARAM, fltSpeed);

        // Update state based on speed
        if (fltSpeed > 0.1f && objCurrentState != AnimationState.SkillQ && objCurrentState != AnimationState.SkillR)
        {
            SetState(AnimationState.Moving);
        }
        else if (fltSpeed <= 0.1f && objCurrentState == AnimationState.Moving)
        {
            SetState(AnimationState.Idle);
        }
    }

    /// <summary>
    /// Trigger Skill Q animation
    /// </summary>
    public void TriggerSkillQAnimation()
    {
        if (objCurrentState != AnimationState.SkillQ)
        {
            SetState(AnimationState.SkillQ);
            animator.SetTrigger(SKILL_Q_PARAM);

            // Reset to idle after animation
            StartCoroutine(ResetToIdleAfterSkill());
        }
    }

    /// <summary>
    /// Trigger Skill R animation
    /// </summary>
    public void TriggerSkillRAnimation()
    {
        if (objCurrentState != AnimationState.SkillR)
        {
            SetState(AnimationState.SkillR);
            animator.SetTrigger(SKILL_R_PARAM);

            // Reset to idle after animation
            StartCoroutine(ResetToIdleAfterSkill());
        }
    }

    /// <summary>
    /// Force idle state
    /// </summary>
    public void SetIdleState()
    {
        SetState(AnimationState.Idle);
        UpdateMovementAnimation(0f);
    }

    /// <summary>
    /// Set current animation state
    /// </summary>
    /// <param name="objState">New animation state</param>
    private void SetState(AnimationState objState)
    {
        objCurrentState = objState;
    }

    /// <summary>
    /// Reset to idle state after skill animation
    /// </summary>
    private System.Collections.IEnumerator ResetToIdleAfterSkill()
    {
        yield return new WaitForSeconds(1f); // Adjust based on skill animation length
        if (objCurrentState == AnimationState.SkillQ || objCurrentState == AnimationState.SkillR)
        {
            SetIdleState();
        }
    }

    /// <summary>
    /// Get current animation state
    /// </summary>
    /// <returns>Current animation state</returns>
    public AnimationState GetCurrentState()
    {
        return objCurrentState;
    }
}