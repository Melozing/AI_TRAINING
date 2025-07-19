using UnityEngine;
using System;
using UnityEngine.EventSystems;

/// <summary>
/// Manages all input events and provides a centralized input system
/// Uses Observer pattern to decouple input from game logic
/// </summary>
public class InputManager : MonoBehaviour
{
    // Events for different input actions
    public static event Action<Vector3> OnMoveToPosition;
    public static event Action OnStopMoving;
    public static event Action OnSkillQPressed;
    public static event Action OnSkillRPressed;

    [Header("Input Settings")]
    [SerializeField] private LayerMask objGroundLayerMask = 1;
    [SerializeField] private Camera objMainCamera;
    [SerializeField] private bool blnUseHoldToMove = true; // True: hold mouse, False: click to move

    private void Awake()
    {
        // Get main camera if not assigned
        if (objMainCamera == null)
            objMainCamera = Camera.main;

        if (objMainCamera == null)
        {
            Debug.LogError("InputManager: Main camera not found!");
            enabled = false;
            return;
        }
    }

    private void Update()
    {
        HandleMouseInput();
        HandleKeyboardInput();
    }

    /// <summary>
    /// Handle mouse input for movement
    /// </summary>
    private void HandleMouseInput()
    {
        // Check if mouse is over UI element - prevent movement if so
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return; // Don't handle movement when clicking on UI
        }

        if (blnUseHoldToMove)
        {
            // Hold mouse to move continuously
            if (Input.GetMouseButton(0)) // Left mouse button held down
            {
                Vector3 objTargetPosition = GetMouseWorldPosition();
                if (objTargetPosition != Vector3.zero)
                {
                    OnMoveToPosition?.Invoke(objTargetPosition);
                }
            }
            else if (Input.GetMouseButtonUp(0)) // Mouse button released
            {
                // Stop moving when mouse is released
                OnStopMoving?.Invoke();
            }
        }
        else
        {
            // Click to move once
            if (Input.GetMouseButtonDown(0)) // Left click
            {
                Vector3 objTargetPosition = GetMouseWorldPosition();
                if (objTargetPosition != Vector3.zero)
                {
                    OnMoveToPosition?.Invoke(objTargetPosition);
                }
            }
        }
    }

    /// <summary>
    /// Handle keyboard input for skills
    /// </summary>
    private void HandleKeyboardInput()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            OnSkillQPressed?.Invoke();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            OnSkillRPressed?.Invoke();
        }
    }

    /// <summary>
    /// Convert mouse screen position to world position
    /// </summary>
    /// <returns>World position where mouse clicked, or Vector3.zero if invalid</returns>
    private Vector3 GetMouseWorldPosition()
    {
        Ray objRay = objMainCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(objRay, out RaycastHit objHit, Mathf.Infinity, objGroundLayerMask))
        {
            return objHit.point;
        }

        return Vector3.zero;
    }
}