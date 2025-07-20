using System;
using UnityEngine;
using UnityEngine.UIElements;

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
[DisallowMultipleComponent]
public class DragHandler : MonoBehaviour
{
    /// <summary>
    /// Flags to control which axes allow rotation.
    /// </summary>
    [Serializable]
    public enum RotationAxisFlags
    {
        None,
        YawOnly,
        All
    }

    [Header("Grid Settings")]
    public Vector3 gridSize = Vector3.one;
    public Vector3 gridCenter = Vector3.zero;

    //[Header("Placement Constraints")]
    //public float minY = 0f;

    [Header("Rotation Settings")]
    public RotationAxisFlags allowRotation = RotationAxisFlags.All;
    public Vector3 rotationSnapDegrees = Vector3.zero;

    [Header("Rigidbody")]
    public RigidbodyStateChange rigidbodyStateChangeOnDrag = RigidbodyStateChange.SetKinematic;
    public RigidbodyStateChange rigidbodyStateChangeOnRelease = RigidbodyStateChange.SetNonKinematic;

    private Camera cam;
    //private Transform selectedTransform;
    private Draggable selectedDraggable;
    private Vector3 localPositionOffset;
    private Quaternion localRotationOffset;

    private Vector3 targetPosition;
    private Quaternion targetRotation;

    private SelectionHandler selectionHandler;

    private DragState currentState = DragState.Idle;
    public DragState CurrentState => currentState;

    public bool useRotationKey1 = true;
    public bool useRotationKey2 = true;

    private void Start()
    {
        cam = Camera.main;
        selectionHandler = GetComponent<SelectionHandler>();
    }

    private void Update()
    {
        HandleInput();
    }

    void FixedUpdate()
    {
        if (selectionHandler.currentSelection == null)
        {
            StopDragging();
            return;
        }
        if (currentState == DragState.Dragging)
        {
            UpdateTargetTransform();
            ApplyTransformToSelection();
        }
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
                if (selectedDraggable == null || InputSystem.GetPointerUp())
                {
                    StopDragging();
                    return;
                }
                HandleRotation();
                break;
        }
    }

    /// <summary>
    /// Attempts to begin dragging the currently selected object.
    /// </summary>
    private void TryStartDragging()
    {
        GameObject selectedObject = selectionHandler.currentSelection;
        if (selectedObject == null) return;

        var draggable = selectedObject.GetComponent<Draggable>();
        if (draggable == null || !draggable.enabled) return;

        selectionHandler.LockSelection();
        InitializeSelection(selectedObject.transform);
        //OnGrabEvent(selectedObject);

        UpdateTargetTransform();
        selectedDraggable = draggable;
        selectedDraggable.StartDrag(rigidbodyStateChangeOnDrag);

        currentState = DragState.Dragging;
    }

    /// <summary>
    /// Initializes offsets and rotation data for dragging the object.
    /// </summary>
    /// <param name="selectedObject">The object to initialize.</param>
    private void InitializeSelection(Transform selectedTransform)
    {
        localPositionOffset = cam.transform.InverseTransformPoint(selectedTransform.position);
        localRotationOffset = Quaternion.Inverse(cam.transform.rotation) * selectedTransform.rotation;
    }

    /// <summary>
    /// Stops dragging and unlocks the selection.
    /// </summary>
    private void StopDragging()
    {
        if (currentState != DragState.Dragging) return;

        // if (selectedTransform != null) //hiero
        // {
        //     OnReleaseEvent(selectedTransform.gameObject);
        // }
        if (selectedDraggable != null)
        {
            selectedDraggable.EndDrag(rigidbodyStateChangeOnRelease);
            selectedDraggable = null;
        }

        // selectedTransform = null;
        selectionHandler.UnlockSelection();
        currentState = DragState.Idle;
    }

    /// <summary>
    /// Handles rotation input commands.
    /// </summary>
    private void HandleRotation()
    {
        if (useRotationKey1 && InputSystem.GetButtonDown(InputButton.Rotate1))
        {
            RotateSelected(0f, 90f, 0f);
        }
        if (useRotationKey2 && InputSystem.GetButtonDown(InputButton.Rotate2))
        {
            RotateSelectedTowardsCamera(-90f);
        }
    }

    /// <summary>
    /// Rotates the selected object by specified Euler angles around its pivot point.
    /// </summary>
    private void RotateSelected(float x, float y, float z)
    {
        GameObject selectedObject = selectionHandler.currentSelection;
        if (selectedObject == null) return;
        
        Transform selectedRoot = selectedObject.transform.root;
        if (selectedRoot == null) return;

        Vector3 pivot = selectedObject.transform.position;
        Quaternion deltaRotation = Quaternion.Euler(x, y, z);

        // Rotate position around pivot
        Vector3 direction = selectedRoot.position - pivot;
        selectedRoot.position = pivot + deltaRotation * direction;
        
        // Apply rotation
        selectedRoot.rotation = GetSnappedRotation(deltaRotation * selectedRoot.rotation);

        // Re-initialize offsets after rotation//hiero
        InitializeSelection(selectedObject.transform);
    }

    private void RotateSelectedTowardsCamera(float angle)
    {
        GameObject selectedObject = selectionHandler.currentSelection;
        if (selectedObject == null) return;

        Transform selectedTransform = selectedObject.transform;
        Transform selectedRoot = selectedObject.transform.root;
        if (selectedRoot == null) return;

        Vector3 cameraOffset = Vector3.zero;

        Vector3 toCamera = Camera.main.transform.position - selectedTransform.position;
        if (Mathf.Abs(toCamera.x) > Mathf.Abs(toCamera.z * 1.5f))
        {
            if (toCamera.x > 0f) cameraOffset = Vector3.right;
            else cameraOffset = -Vector3.right;
        }
        else
        {
            if (toCamera.z > 0f) cameraOffset = Vector3.forward;
            else cameraOffset = -Vector3.forward;
        }
        Vector3 rightVector = Vector3.Cross(cameraOffset, Vector3.up);

        Quaternion deltaRotation = Quaternion.AngleAxis(angle, rightVector);

        Vector3 pivot = selectedObject.transform.position;

        // Rotate position around pivot
        Vector3 direction = selectedRoot.position - pivot;
        selectedRoot.position = pivot + deltaRotation * direction;
        
        // Apply rotation
        selectedRoot.rotation = GetSnappedRotation(deltaRotation * selectedRoot.rotation);

        Rigidbody rb = selectedRoot.GetComponent<Rigidbody>();
        if (rb)
        {
            rb.MovePosition(selectedRoot.position);
            rb.MoveRotation(selectedRoot.rotation);
        }
        InitializeSelection(selectedObject.transform);
    }

    /// <summary>
    /// Updates target position and rotation of the selected object based on input and constraints.
    /// </summary>
    private void UpdateTargetTransform()
    {
        if (selectedDraggable == null) return;

        Vector3 worldTargetPosition = cam.transform.TransformPoint(localPositionOffset);
        targetPosition = SnapToGrid(worldTargetPosition);

        Vector3 targetRotationAngles = selectedDraggable.transform.rotation.eulerAngles;
        if (allowRotation != RotationAxisFlags.None)
        {
            Vector3 worldTargetRotation = (cam.transform.rotation * localRotationOffset).eulerAngles;
            if (allowRotation == RotationAxisFlags.All) targetRotationAngles = worldTargetRotation;
            if (allowRotation == RotationAxisFlags.YawOnly) targetRotationAngles.y = worldTargetRotation.y;
        }

        targetRotation = Quaternion.Euler(GetSnappedRotation(targetRotationAngles)); 
    }

    /// <summary>
    /// Applies calculated position and rotation to the selected object
    /// </summary>
    private void ApplyTransformToSelection()
    {
        if (selectedDraggable)
        {
            selectedDraggable.UpdateDrag(targetPosition, targetRotation);
        }
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

    private Quaternion GetSnappedRotation(Quaternion quaternion)
    {
        return Quaternion.Euler(GetSnappedRotation(quaternion.eulerAngles));
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
        //snappedPosition.y = Mathf.Max(minY, snappedPosition.y);

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
