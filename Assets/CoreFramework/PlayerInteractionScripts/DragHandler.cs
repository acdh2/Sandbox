using System;
using UnityEngine;

/// <summary>
/// Represents the current state of the drag operation.
/// </summary>
[System.Serializable]
public enum DragState
{
    Idle,
    Dragging
}

/// <summary>
/// Handles dragging and placing a selected object on a grid using mouse input.
/// Requires a SelectionHandler component to function properly.
/// </summary>
[RequireComponent(typeof(SelectionHandler))]
public class DragHandler : MonoBehaviour
{
    /// <summary>
    /// Flags to control which axes allow rotation.
    /// </summary>
    [Serializable]
    public struct RotationAxisFlags
    {
        public bool X;
        public bool Y;
        public bool Z;
    }

    [Header("Grid Settings")]
    public Vector3 gridSize = Vector3.one;
    public Vector3 gridCenter = Vector3.zero;

    [Header("Placement Constraints")]
    public float minY = 0f;

    [Header("Rotation Settings")]
    public RotationAxisFlags allowRotation = new RotationAxisFlags { X = true, Y = true, Z = true };
    public Vector3 rotationSnapDegrees = Vector3.zero;

    [Header("Movement Settings")]
    public bool smoothMovement = false;
    public float moveResponsiveness = 100f;

    private Camera cam;
    private Transform selectedTransform;
    private Vector3 localOffset;
    private Quaternion rotationOffset;
    private Vector3 targetPosition;
    private Quaternion targetRotation;

    private SelectionHandler selectionHandler;

    private DragState currentState = DragState.Idle;
    public DragState CurrentState => currentState;

    private void Start()
    {
        cam = Camera.main;
        selectionHandler = GetComponent<SelectionHandler>();
    }

    private void Update()
    {
        HandleInput();
        HandleRotation();
    }

    /// <summary>
    /// Handles user input based on current drag state.
    /// </summary>
    private void HandleInput()
    {
        switch (currentState)
        {
            case DragState.Idle:
                if (InputSystem.GetPointerDown())
                    TryStartDragging();
                break;

            case DragState.Dragging:
                if (selectedTransform == null || InputSystem.GetPointerUp())
                    StopDragging();
                else if (InputSystem.GetPointerHeld())
                {
                    UpdateTargetTransform();
                    ApplyTransformToSelection();
                }
                break;
        }
    }

    /// <summary>
    /// Attempts to begin dragging the currently selected object.
    /// </summary>
    private void TryStartDragging()
    {
        GameObject selectedObject = selectionHandler.CurrentSelection;
        if (selectedObject == null) return;

        var selectable = selectedObject.GetComponent<Selectable>();
        if (selectable == null || !selectable.IsDraggable) return;

        selectionHandler.LockSelection();
        InitializeSelection(selectedObject);
        OnGrabEvent(selectedObject);

        currentState = DragState.Dragging;
    }

    /// <summary>
    /// Initializes offsets and rotation data for dragging the object.
    /// </summary>
    /// <param name="selectedObject">The object to initialize.</param>
    private void InitializeSelection(GameObject selectedObject)
    {
        selectedTransform = selectedObject.transform.root;
        localOffset = cam.transform.InverseTransformPoint(selectedTransform.position);

        if (allowRotation.X || allowRotation.Y || allowRotation.Z)
            rotationOffset = Quaternion.Inverse(cam.transform.rotation) * selectedTransform.rotation;
    }

    /// <summary>
    /// Stops dragging and unlocks the selection.
    /// </summary>
    private void StopDragging()
    {
        if (selectedTransform != null)
            OnReleaseEvent(selectedTransform.gameObject);

        selectedTransform = null;
        selectionHandler.UnlockSelection();
        currentState = DragState.Idle;
    }

    /// <summary>
    /// Raises grab event if the object implements IGrabbable.
    /// </summary>
    /// <param name="targetObject">Object being grabbed.</param>
    private void OnGrabEvent(GameObject targetObject)
    {
        if (targetObject != null)
        {
            foreach (IGrabbable grabbable in targetObject.transform.root.GetComponentsInChildren<IGrabbable>())
            {
                grabbable.OnGrab();
            }
        }
    }

    /// <summary>
    /// Raises release event if the object implements IGrabbable.
    /// </summary>
    /// <param name="targetObject">Object being released.</param>
    private void OnReleaseEvent(GameObject targetObject)
    {
        if (targetObject != null)
        {
            foreach (IGrabbable grabbable in targetObject.transform.root.GetComponentsInChildren<IGrabbable>())
            {
                grabbable.OnRelease();
            }
        }
    }

    /// <summary>
    /// Handles rotation input commands.
    /// </summary>
    private void HandleRotation()
    {
        if (InputSystem.GetButtonDown(InputButton.Rotate1))
        {
            RotateSelected(0, 90, 0);
        }
        if (InputSystem.GetButtonDown(InputButton.Rotate2))
        {
            RotateSelectedTowardCamera();
        }
    }

    /// <summary>
    /// Rotates the selected object by specified Euler angles around its pivot point.
    /// </summary>
    private void RotateSelected(float x, float y, float z)
    {
        GameObject selectedObject = selectionHandler.CurrentSelection;
        if (selectedObject == null) return;

        Transform selectedRoot = selectedObject.transform.root;
        if (selectedRoot == null) return;

        Vector3 pivot = selectedObject.transform.position;
        Quaternion deltaRotation = Quaternion.Euler(x, y, z);

        // Rotate position around pivot
        Vector3 direction = selectedRoot.position - pivot;
        selectedRoot.position = pivot + deltaRotation * direction;

        // Apply rotation
        selectedRoot.rotation = deltaRotation * selectedRoot.rotation;

        // Re-initialize offsets after rotation
        InitializeSelection(selectedObject);
    }

    /// <summary>
    /// Rotates the selected object to face the camera on the horizontal plane.
    /// </summary>
    private void RotateSelectedTowardCamera()
    {
        GameObject selectedObject = selectionHandler.CurrentSelection;
        if (selectedObject == null) return;

        Transform selectedRoot = selectedObject.transform.root;
        if (selectedRoot == null) return;

        Vector3 toCamera = Camera.main.transform.position - selectedRoot.position;
        toCamera.y = 0; // Only horizontal rotation
        toCamera.Normalize();

        float dotForward = Vector3.Dot(toCamera, Vector3.forward);
        float dotRight = Vector3.Dot(toCamera, Vector3.right);

        Vector3 euler = Vector3.zero;

        if (Mathf.Abs(dotForward) > Mathf.Abs(dotRight))
            euler = dotForward > 0 ? new Vector3(90, 0, 0) : new Vector3(-90, 0, 0);
        else
            euler = dotRight < 0 ? new Vector3(0, 0, 90) : new Vector3(0, 0, -90);

        RotateSelected(euler.x, euler.y, euler.z);
    }

    /// <summary>
    /// Updates target position and rotation of the selected object based on input and constraints.
    /// </summary>
    private void UpdateTargetTransform()
    {
        if (selectedTransform == null) return;

        Vector3 worldTargetPosition = cam.transform.TransformPoint(localOffset);
        targetPosition = SnapToGrid(worldTargetPosition);

        if (allowRotation.X || allowRotation.Y || allowRotation.Z)
            targetRotation = GetDragRotation();
        else
            targetRotation = Quaternion.Euler(GetSnappedRotation(selectedTransform.rotation.eulerAngles));
    }

    /// <summary>
    /// Applies calculated position and rotation to the selected object, optionally with smoothing.
    /// </summary>
    private void ApplyTransformToSelection()
    {
        if (selectedTransform == null) return;

        if (smoothMovement)
        {
            selectedTransform.position = Vector3.Lerp(selectedTransform.position, targetPosition, moveResponsiveness * Time.deltaTime);
            Quaternion interpolatedRotation = Quaternion.Slerp(selectedTransform.rotation, targetRotation, moveResponsiveness * Time.deltaTime);

            if (IsNormalized(interpolatedRotation))
                selectedTransform.rotation = interpolatedRotation;
        }
        else
        {
            selectedTransform.position = targetPosition;
            if (IsNormalized(targetRotation))
                selectedTransform.rotation = targetRotation;
        }

        ApplyPlacementConstraints();
    }

    /// <summary>
    /// Ensures the selected object's bottom stays above the minimum Y position.
    /// </summary>
    private void ApplyPlacementConstraints()
    {
        if (selectedTransform == null) return;

        Renderer[] renderers = selectedTransform.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds combinedBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            combinedBounds.Encapsulate(renderers[i].bounds);

        float offsetY = minY - combinedBounds.min.y;
        if (offsetY > 0f)
        {
            Vector3 newPosition = selectedTransform.position + new Vector3(0f, offsetY, 0f);
            selectedTransform.position = newPosition;
            targetPosition = newPosition;

            // Optional: adjust localOffset if needed to prevent snapping issues next frame
            // localOffset += cam.transform.InverseTransformVector(Vector3.up * offsetY);
        }
    }

    /// <summary>
    /// Computes the rotation of the dragged object relative to the camera, applying allowed axes and snapping.
    /// </summary>
    private Quaternion GetDragRotation()
    {
        Quaternion rawRotation = cam.transform.rotation * rotationOffset;
        Vector3 cameraEuler = rawRotation.eulerAngles;
        Vector3 currentEuler = selectedTransform.rotation.eulerAngles;

        Vector3 resultEuler = currentEuler;

        if (allowRotation.X)
            resultEuler.x = cameraEuler.x;
        if (allowRotation.Y)
            resultEuler.y = cameraEuler.y;
        if (allowRotation.Z)
            resultEuler.z = cameraEuler.z;

        return Quaternion.Euler(GetSnappedRotation(resultEuler));
    }

    /// <summary>
    /// Snaps each component of a rotation vector to configured increments.
    /// </summary>
    private Vector3 GetSnappedRotation(Vector3 rotation)
    {
        if (rotationSnapDegrees.x > 0f)
            rotation.x = Mathf.Round(rotation.x / rotationSnapDegrees.x) * rotationSnapDegrees.x;
        if (rotationSnapDegrees.y > 0f)
            rotation.y = Mathf.Round(rotation.y / rotationSnapDegrees.y) * rotationSnapDegrees.y;
        if (rotationSnapDegrees.z > 0f)
            rotation.z = Mathf.Round(rotation.z / rotationSnapDegrees.z) * rotationSnapDegrees.z;

        return rotation;
    }

    /// <summary>
    /// Snaps a given position to the defined grid, and clamps Y to minimum allowed.
    /// </summary>
    private Vector3 SnapToGrid(Vector3 position)
    {
        Vector3 offset = position - gridCenter;

        if (gridSize.x > 0)
            offset.x = Mathf.Round(offset.x / gridSize.x) * gridSize.x;
        if (gridSize.y > 0)
            offset.y = Mathf.Round(offset.y / gridSize.y) * gridSize.y;
        if (gridSize.z > 0)
            offset.z = Mathf.Round(offset.z / gridSize.z) * gridSize.z;

        Vector3 snappedPosition = gridCenter + offset;
        snappedPosition.y = Mathf.Max(minY, snappedPosition.y);

        return snappedPosition;
    }

    /// <summary>
    /// Checks if a quaternion is normalized.
    /// </summary>
    private bool IsNormalized(Quaternion q)
    {
        // Unity quaternions are expected to be normalized; this is a simple check.
        return Mathf.Abs(1.0f - (q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w)) < 0.01f;
    }
}
