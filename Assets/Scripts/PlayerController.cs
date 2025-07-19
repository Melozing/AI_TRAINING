using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;

/// <summary>
/// Controls player movement and skills with SFX integration
/// Uses click-to-move system
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float fltMoveSpeed = 5f;
    [SerializeField] private float fltRotationSpeed = 200f;
    [SerializeField] private float fltStoppingDistance = 0.1f; // Khoảng cách để dừng
    [SerializeField] private float fltMovementSmoothing = 5f; // Làm mượt chuyển động
    [SerializeField] private bool blnEnableDragMovement = true; // Cho phép thay đổi vị trí khi giữ chuột
    [SerializeField] private float fltDragUpdateInterval = 0.1f; // Thời gian cập nhật vị trí khi drag

    [Header("Animation")]
    [SerializeField] private Animator objPlayerAnimator;
    [SerializeField] private string strMovementSpeedParam = "Speed";
    [SerializeField] private string strSkillQTrigger = "SkillQ";
    [SerializeField] private string strSkillRTrigger = "SkillR";

    [Header("Footstep Sync")]
    [SerializeField] private bool blnEnableFootstepSync = true;
    [SerializeField] private float fltFootstepInterval = 0.5f; // Thời gian giữa các bước chân (có thể điều chỉnh theo animation)

    [Header("Skill Cooldowns")]
    [SerializeField] private float fltSkillQCooldown = 1.0f;
    [SerializeField] private float fltSkillRCooldown = 5.0f;
    [SerializeField] private bool blnShowDebugLogs = true;

    [Header("SFX")]
    [SerializeField] private bool blnEnableSFX = true;

    [Header("Chat System")]
    [SerializeField] private NPCChatSystem objNPCChatSystem;

    // Private fields
    private Vector3 lastPosition;
    private Vector3 targetPosition;
    private bool blnIsMoving = false;
    private bool blnHasTarget = false;
    private bool blnWasMoving = false; // Track previous movement state for animation
    private Vector3 currentVelocity; // For smooth movement

    // Skill cooldown tracking
    private float fltLastSkillQTime = 0f;
    private float fltLastSkillRTime = 0f;

    // Footstep tracking
    private float fltLastFootstepTime = 0f;
    private float fltMovementStartTime = 0f;

    // Drag movement tracking
    private float fltLastDragUpdateTime = 0f;
    private bool blnIsDragging = false;

    // Skill state tracking
    private bool blnIsUsingSkill = false;
    private float fltSkillAnimationDuration = 1.0f; // Thời gian animation skill

    private void Start()
    {
        // Auto-assign animator if not set
        if (objPlayerAnimator == null)
            objPlayerAnimator = GetComponent<Animator>();

        // Auto-assign NPCChatSystem if not set
        if (objNPCChatSystem == null)
            objNPCChatSystem = FindFirstObjectByType<NPCChatSystem>();

        lastPosition = transform.position;
        targetPosition = transform.position;
    }

    private void Update()
    {
        HandleMovement();
        HandleSkills();
        UpdateAnimations();
    }

    /// <summary>
    /// Handle player movement with click-to-move and drag support
    /// </summary>
    private void HandleMovement()
    {
        // Check if mouse is over UI element - prevent movement if so
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return; // Don't handle movement when clicking on UI
        }

        // Handle mouse click and drag
        if (Input.GetMouseButtonDown(0)) // Left click
        {
            SetTargetPosition();
            blnIsDragging = true;
            fltLastDragUpdateTime = Time.time;
        }
        else if (Input.GetMouseButtonUp(0)) // Mouse release
        {
            blnIsDragging = false;
        }
        else if (Input.GetMouseButton(0) && blnIsDragging && blnEnableDragMovement) // Mouse held down
        {
            // Update target position while dragging (with interval to avoid too frequent updates)
            if (Time.time - fltLastDragUpdateTime >= fltDragUpdateInterval)
            {
                SetTargetPosition();
                fltLastDragUpdateTime = Time.time;

                if (blnShowDebugLogs)
                {
                    Debug.Log($"PlayerController: Updated target position while dragging");
                }
            }
        }

        // Move towards target if we have one
        if (blnHasTarget)
        {
            MoveTowardsTarget();
        }
    }

    /// <summary>
    /// Set target position from mouse click
    /// </summary>
    private void SetTargetPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            targetPosition = hit.point;
            targetPosition.y = transform.position.y; // Keep same Y level
            blnHasTarget = true;
        }
    }

    /// <summary>
    /// Move player towards target position
    /// </summary>
    private void MoveTowardsTarget()
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        float distanceToTarget = Vector3.Distance(transform.position, targetPosition);

        // Check if we've reached the target
        if (distanceToTarget <= fltStoppingDistance)
        {
            // Reached target, stop moving
            blnIsMoving = false;
            blnHasTarget = false;
            currentVelocity = Vector3.zero;
            return;
        }

        // Smooth movement towards target
        Vector3 targetVelocity = direction * fltMoveSpeed;
        currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, Time.deltaTime * fltMovementSmoothing);

        // Move player
        transform.Translate(currentVelocity * Time.deltaTime, Space.World);

        // Rotate towards movement direction
        if (direction != Vector3.zero)
        {
            Quaternion toRotation = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, toRotation, fltRotationSpeed * Time.deltaTime);
        }

        blnIsMoving = true;
    }

    /// <summary>
    /// Handle player skills
    /// </summary>
    private void HandleSkills()
    {
        // Check if player is chatting - don't use skills if chatting
        if (IsPlayerChatting())
        {
            return;
        }

        // Skill Q
        if (Input.GetKeyDown(KeyCode.Q))
        {
            UseSkillQ();
        }

        // Skill R
        if (Input.GetKeyDown(KeyCode.R))
        {
            UseSkillR();
        }
    }

    /// <summary>
    /// Use Skill Q
    /// </summary>
    private void UseSkillQ()
    {
        // Check cooldown
        if (Time.time - fltLastSkillQTime < fltSkillQCooldown)
        {
            if (blnShowDebugLogs)
            {
                Debug.Log($"PlayerController: Skill Q on cooldown. Remaining: {fltSkillQCooldown - (Time.time - fltLastSkillQTime):F1}s");
            }
            return; // Still on cooldown
        }

        // Update last use time
        fltLastSkillQTime = Time.time;

        // Set skill state to prevent movement sounds
        blnIsUsingSkill = true;
        StartCoroutine(ResetSkillStateAfterAnimation());

        if (blnShowDebugLogs)
        {
            Debug.Log("PlayerController: Using Skill Q - Movement sounds disabled");
        }

        // Trigger animation
        if (objPlayerAnimator != null)
        {
            objPlayerAnimator.SetTrigger(strSkillQTrigger);
        }

        // Play SFX
        if (blnEnableSFX && SFXManager.Instance != null)
        {
            SFXManager.Instance.PlayPlayerSkillQ();
        }
    }

    /// <summary>
    /// Use Skill R
    /// </summary>
    private void UseSkillR()
    {
        // Check cooldown
        if (Time.time - fltLastSkillRTime < fltSkillRCooldown)
        {
            if (blnShowDebugLogs)
            {
                Debug.Log($"PlayerController: Skill R on cooldown. Remaining: {fltSkillRCooldown - (Time.time - fltLastSkillRTime):F1}s");
            }
            return; // Still on cooldown
        }

        // Update last use time
        fltLastSkillRTime = Time.time;

        // Set skill state to prevent movement sounds
        blnIsUsingSkill = true;
        StartCoroutine(ResetSkillStateAfterAnimation());

        if (blnShowDebugLogs)
        {
            Debug.Log("PlayerController: Using Skill R - Movement sounds disabled");
        }

        // Trigger animation
        if (objPlayerAnimator != null)
        {
            objPlayerAnimator.SetTrigger(strSkillRTrigger);
        }

        // Play SFX
        if (blnEnableSFX && SFXManager.Instance != null)
        {
            SFXManager.Instance.PlayPlayerSkillR();
        }
    }

    /// <summary>
    /// Update animations and play movement SFX
    /// </summary>
    private void UpdateAnimations()
    {
        if (objPlayerAnimator == null) return;

        // Calculate movement speed
        float speed = Vector3.Distance(transform.position, lastPosition) / Time.deltaTime;
        lastPosition = transform.position;

        // Only update animation when movement state actually changes
        if (blnIsMoving != blnWasMoving)
        {
            // Movement state changed - update animation
            if (blnIsMoving)
            {
                // Started moving
                objPlayerAnimator.SetFloat(strMovementSpeedParam, 1f);
                fltMovementStartTime = Time.time;
                fltLastFootstepTime = Time.time - fltFootstepInterval; // Reset footstep timer

                // Play first footstep sound (only if not using skill)
                if (blnEnableSFX && blnEnableFootstepSync && SFXManager.Instance != null && !blnIsUsingSkill)
                {
                    if (blnShowDebugLogs)
                    {
                        Debug.Log($"PlayerController: Playing first footstep - Speed: {speed:F2}");
                    }
                    SFXManager.Instance.PlayPlayerMovement(speed);
                    fltLastFootstepTime = Time.time;
                }
                else if (blnIsUsingSkill && blnShowDebugLogs)
                {
                    Debug.Log($"PlayerController: Skipping footstep sound (using skill) - Speed: {speed:F2}");
                }
            }
            else
            {
                // Stopped moving
                objPlayerAnimator.SetFloat(strMovementSpeedParam, 0f);
            }

            blnWasMoving = blnIsMoving;
        }
        else if (blnIsMoving)
        {
            // Still moving - keep animation and play synchronized footsteps
            objPlayerAnimator.SetFloat(strMovementSpeedParam, 1f);

            // Play footstep sounds at regular intervals (only if not using skill)
            if (blnEnableSFX && blnEnableFootstepSync && SFXManager.Instance != null && !blnIsUsingSkill)
            {
                float timeSinceLastFootstep = Time.time - fltLastFootstepTime;

                if (timeSinceLastFootstep >= fltFootstepInterval)
                {
                    if (blnShowDebugLogs)
                    {
                        Debug.Log($"PlayerController: Playing synchronized footstep - Speed: {speed:F2}, Interval: {timeSinceLastFootstep:F2}s");
                    }
                    SFXManager.Instance.PlayPlayerMovement(speed);
                    fltLastFootstepTime = Time.time;
                }
            }
            else if (blnIsUsingSkill && blnShowDebugLogs)
            {
                // Log skipped footsteps less frequently to avoid spam
                float timeSinceLastLog = Time.time - fltLastFootstepTime;
                if (timeSinceLastLog >= fltFootstepInterval)
                {
                    Debug.Log($"PlayerController: Skipping footstep sound (using skill) - Speed: {speed:F2}");
                    fltLastFootstepTime = Time.time;
                }
            }
        }
    }

    /// <summary>
    /// Stop player movement (can be called from other scripts)
    /// </summary>
    public void StopMovement()
    {
        blnIsMoving = false;
        blnHasTarget = false;
    }

    /// <summary>
    /// Check if player is currently moving
    /// </summary>
    /// <returns>True if player is moving</returns>
    public bool IsMoving()
    {
        return blnIsMoving;
    }

    /// <summary>
    /// Check if player is currently chatting with NPC
    /// </summary>
    /// <returns>True if player is chatting</returns>
    private bool IsPlayerChatting()
    {
        if (objNPCChatSystem == null)
            return false;

        return objNPCChatSystem.IsPlayerChatting() || objNPCChatSystem.IsInputFieldActive();
    }

    /// <summary>
    /// Check if Skill Q is on cooldown
    /// </summary>
    /// <returns>True if skill is on cooldown</returns>
    public bool IsSkillQOnCooldown()
    {
        return Time.time - fltLastSkillQTime < fltSkillQCooldown;
    }

    /// <summary>
    /// Check if Skill R is on cooldown
    /// </summary>
    /// <returns>True if skill is on cooldown</returns>
    public bool IsSkillROnCooldown()
    {
        return Time.time - fltLastSkillRTime < fltSkillRCooldown;
    }

    /// <summary>
    /// Get remaining cooldown time for Skill Q
    /// </summary>
    /// <returns>Remaining cooldown time in seconds</returns>
    public float GetSkillQCooldownRemaining()
    {
        float remaining = fltSkillQCooldown - (Time.time - fltLastSkillQTime);
        return Mathf.Max(0f, remaining);
    }

    /// <summary>
    /// Get remaining cooldown time for Skill R
    /// </summary>
    /// <returns>Remaining cooldown time in seconds</returns>
    public float GetSkillRCooldownRemaining()
    {
        float remaining = fltSkillRCooldown - (Time.time - fltLastSkillRTime);
        return Mathf.Max(0f, remaining);
    }

    /// <summary>
    /// Get Skill Q cooldown duration
    /// </summary>
    /// <returns>Cooldown duration for Skill Q</returns>
    public float GetSkillQCooldown()
    {
        return fltSkillQCooldown;
    }

    /// <summary>
    /// Get Skill R cooldown duration
    /// </summary>
    /// <returns>Cooldown duration for Skill R</returns>
    public float GetSkillRCooldown()
    {
        return fltSkillRCooldown;
    }

    /// <summary>
    /// Set footstep interval (for animation sync)
    /// </summary>
    /// <param name="interval">New footstep interval in seconds</param>
    public void SetFootstepInterval(float interval)
    {
        fltFootstepInterval = Mathf.Max(0.1f, interval); // Minimum 0.1s

        if (blnShowDebugLogs)
        {
            Debug.Log($"PlayerController: Footstep interval set to {fltFootstepInterval:F2}s");
        }
    }

    /// <summary>
    /// Get current footstep interval
    /// </summary>
    /// <returns>Current footstep interval</returns>
    public float GetFootstepInterval()
    {
        return fltFootstepInterval;
    }

    /// <summary>
    /// Enable/disable footstep sync
    /// </summary>
    /// <param name="enable">True to enable footstep sync</param>
    public void SetFootstepSync(bool enable)
    {
        blnEnableFootstepSync = enable;

        if (blnShowDebugLogs)
        {
            Debug.Log($"PlayerController: Footstep sync {(enable ? "enabled" : "disabled")}");
        }
    }

    /// <summary>
    /// Enable/disable drag movement
    /// </summary>
    /// <param name="enable">True to enable drag movement</param>
    public void SetDragMovement(bool enable)
    {
        blnEnableDragMovement = enable;

        if (blnShowDebugLogs)
        {
            Debug.Log($"PlayerController: Drag movement {(enable ? "enabled" : "disabled")}");
        }
    }

    /// <summary>
    /// Set drag update interval
    /// </summary>
    /// <param name="interval">New drag update interval in seconds</param>
    public void SetDragUpdateInterval(float interval)
    {
        fltDragUpdateInterval = Mathf.Max(0.05f, interval); // Minimum 0.05s

        if (blnShowDebugLogs)
        {
            Debug.Log($"PlayerController: Drag update interval set to {fltDragUpdateInterval:F2}s");
        }
    }

    /// <summary>
    /// Check if player is currently dragging
    /// </summary>
    /// <returns>True if player is dragging</returns>
    public bool IsDragging()
    {
        return blnIsDragging;
    }

    /// <summary>
    /// Reset skill state after animation duration
    /// </summary>
    private IEnumerator ResetSkillStateAfterAnimation()
    {
        yield return new WaitForSeconds(fltSkillAnimationDuration);

        blnIsUsingSkill = false;

        if (blnShowDebugLogs)
        {
            Debug.Log("PlayerController: Skill animation finished - Movement sounds re-enabled");
        }
    }

    /// <summary>
    /// Check if player is currently using a skill
    /// </summary>
    /// <returns>True if player is using skill</returns>
    public bool IsUsingSkill()
    {
        return blnIsUsingSkill;
    }

    /// <summary>
    /// Set skill animation duration
    /// </summary>
    /// <param name="duration">Animation duration in seconds</param>
    public void SetSkillAnimationDuration(float duration)
    {
        fltSkillAnimationDuration = Mathf.Max(0.1f, duration);

        if (blnShowDebugLogs)
        {
            Debug.Log($"PlayerController: Skill animation duration set to {fltSkillAnimationDuration:F2}s");
        }
    }
}