using UnityEngine;

public class AgentAnimationController : MonoBehaviour
{
    [Header("Animation Settings")]
    public Animator animator;

    // Animation parameter names
    private readonly string SPEED_PARAM = "Speed";
    private readonly string IS_PUSHING_PARAM = "IsPushing";

    // Animation states
    private bool isPushing = false;
    private float currentSpeed = 0f;

    void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    /// <summary>
    /// Update animation based on agent movement speed
    /// </summary>
    public void UpdateMovementAnimation(float speed)
    {
        currentSpeed = speed;
        animator.SetFloat(SPEED_PARAM, speed);
    }

    /// <summary>
    /// Set pushing animation state
    /// </summary>
    public void SetPushingState(bool pushing)
    {
        if (isPushing != pushing)
        {
            isPushing = pushing;
            animator.SetBool(IS_PUSHING_PARAM, pushing);
        }
    }

    /// <summary>
    /// Force idle state
    /// </summary>
    public void SetIdleState()
    {
        UpdateMovementAnimation(0f);
        SetPushingState(false);
    }
}