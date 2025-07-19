using UnityEngine;
using System.Collections;

/// <summary>
/// Handles agent movement logic and pathfinding
/// Separates movement concerns from agent behavior
/// </summary>
public class MovementController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float fltMoveSpeed = 5f;
    [SerializeField] private float fltStoppingDistance = 0.1f;
    [SerializeField] private float fltRotationSpeed = 200f;

    private Vector3 objTargetPosition;
    private bool blnIsMoving = false;
    private Rigidbody objRigidbody;

    // Events
    public System.Action<bool> OnMovementStateChanged;
    public System.Action<float> OnSpeedChanged;

    private void Awake()
    {
        objRigidbody = GetComponentInChildren<Rigidbody>();
        if (objRigidbody == null)
        {
            Debug.LogError("MovementController: Rigidbody component not found!");
            enabled = false;
            return;
        }
    }

    private void Start()
    {
        // Delay subscription to avoid initialization conflicts
        StartCoroutine(DelayedInitialization());
    }

    private System.Collections.IEnumerator DelayedInitialization()
    {
        // Wait for one frame to ensure all components are initialized
        yield return null;

        // Subscribe to input events
        InputManager.OnMoveToPosition += MoveToPosition;
        InputManager.OnStopMoving += StopMovement;
    }

    private void OnDestroy()
    {
        // Unsubscribe from input events
        InputManager.OnMoveToPosition -= MoveToPosition;
        InputManager.OnStopMoving -= StopMovement;
    }

    private void Update()
    {
        if (blnIsMoving)
        {
            UpdateMovement();
        }
    }

    /// <summary>
    /// Move agent to specified position
    /// </summary>
    /// <param name="objPosition">Target position to move to</param>
    public void MoveToPosition(Vector3 objPosition)
    {
        objTargetPosition = objPosition;

        // Start moving if not already moving
        if (!blnIsMoving)
        {
            blnIsMoving = true;
            OnMovementStateChanged?.Invoke(true);
        }
    }

    /// <summary>
    /// Stop current movement
    /// </summary>
    public void StopMovement()
    {
        blnIsMoving = false;
        objRigidbody.velocity = Vector3.zero;
        OnMovementStateChanged?.Invoke(false);
        OnSpeedChanged?.Invoke(0f);
    }

    /// <summary>
    /// Update movement logic each frame
    /// </summary>
    private void UpdateMovement()
    {
        Vector3 objDirection = (objTargetPosition - transform.position).normalized;
        float fltDistance = Vector3.Distance(transform.position, objTargetPosition);

        if (fltDistance <= fltStoppingDistance)
        {
            StopMovement();
            return;
        }

        // Rotate towards target
        if (objDirection != Vector3.zero)
        {
            Quaternion objTargetRotation = Quaternion.LookRotation(objDirection);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, objTargetRotation, fltRotationSpeed * Time.deltaTime);
        }

        // Move towards target
        Vector3 objVelocity = objDirection * fltMoveSpeed;
        objRigidbody.velocity = new Vector3(objVelocity.x, objRigidbody.velocity.y, objVelocity.z);

        // Notify speed change
        OnSpeedChanged?.Invoke(fltMoveSpeed);
    }

    /// <summary>
    /// Check if agent is currently moving
    /// </summary>
    /// <returns>True if moving, false otherwise</returns>
    public bool IsMoving()
    {
        return blnIsMoving;
    }

    /// <summary>
    /// Get current movement speed
    /// </summary>
    /// <returns>Current movement speed</returns>
    public float GetCurrentSpeed()
    {
        return blnIsMoving ? fltMoveSpeed : 0f;
    }
}