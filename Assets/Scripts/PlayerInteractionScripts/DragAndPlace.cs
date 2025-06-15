using UnityEngine;

/// <summary>
/// Allows dragging and placing of a selected object on a grid using mouse input.
/// Requires a SelectionHandler component to function.
/// </summary>
[RequireComponent(typeof(SelectionHandler))]
public class DragAndPlace : MonoBehaviour
{
    public enum DragState
    {
        Idle,
        Dragging
    }

    [Header("Grid Settings")]
    public Vector3 gridSize = Vector3.one;
    public Vector3 gridCenter = Vector3.zero;

    [Header("Rotation Settings")]
    public bool enableRotation = true;
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
    private Rigidbody originalRigidbody;
    private bool originalKinematicState;

    private DragState currentState = DragState.Idle;

    void Start()
    {
        cam = Camera.main;
        selectionHandler = GetComponent<SelectionHandler>();
    }

    void Update()
    {
        HandleInput();
    }

    /// <summary>
    /// Handles mouse input depending on the current drag state.
    /// </summary>
    private void HandleInput()
    {
        switch (currentState)
        {
            case DragState.Idle:
                if (Input.GetMouseButtonDown(0))
                    TryStartDragging();
                break;

            case DragState.Dragging:
                if (Input.GetMouseButtonUp(0))
                    StopDragging();
                else if (Input.GetMouseButton(0))
                {
                    UpdateTargetTransform();
                    ApplyTransformToSelection();
                }
                break;
        }
    }

    /// <summary>
    /// Attempts to start dragging the currently selected object.
    /// </summary>
    private void TryStartDragging()
    {
        GameObject selectedObject = selectionHandler.CurrentSelection;
        if (selectedObject == null) return;

        selectedTransform = selectedObject.transform;
        localOffset = cam.transform.InverseTransformPoint(selectedTransform.position);
        selectionHandler.LockSelection();

        if (enableRotation)
            rotationOffset = Quaternion.Inverse(cam.transform.rotation) * selectedTransform.rotation;

        DisableRigidbodyIfPresent(selectedObject);

        InvokeGrabEvent(selectedObject);

        currentState = DragState.Dragging;
    }

    /// <summary>
    /// Invokes the grab event on the object if it has a SandboxBase component.
    /// </summary>
    /// <param name="targetObject">The object being grabbed.</param>
    private void InvokeGrabEvent(GameObject targetObject)
    {
        targetObject.GetComponent<SandboxBase>()?.InvokeGrab();
    }

    /// <summary>
    /// Stops dragging and restores the Rigidbody state.
    /// </summary>
    private void StopDragging()
    {
        if (selectedTransform != null)
                InvokeReleaseEvent(selectedTransform.gameObject);

        RestoreRigidbodyIfPresent();
        selectedTransform = null;
        selectionHandler.UnlockSelection();
        currentState = DragState.Idle;
    }

    /// <summary>
    /// Invokes the release event on the object if it has a SandboxBase.
    /// </summary>
    /// <param name="targetObject">The object being released.</param>
    void InvokeReleaseEvent(GameObject targetObject)
    {
        targetObject.GetComponent<SandboxBase>()?.InvokeRelease();
    }

    /// <summary>
    /// Updates the target position and rotation for the dragged object.
    /// </summary>
    private void UpdateTargetTransform()
    {
        Vector3 worldTargetPosition = cam.transform.TransformPoint(localOffset);
        targetPosition = ApplyPlacementConstraints(SnapToGrid(worldTargetPosition));
        targetRotation = enableRotation ? GetSnappedRotation() : selectedTransform.rotation;
    }

    /// <summary>
    /// Moves the selected object to the target position and rotation.
    /// </summary>
    private void ApplyTransformToSelection()
    {
        if (smoothMovement)
        {
            selectedTransform.position = Vector3.Lerp(
                selectedTransform.position,
                targetPosition,
                moveResponsiveness * Time.deltaTime
            );

            Quaternion interpolated = Quaternion.Slerp(
                selectedTransform.rotation,
                targetRotation,
                moveResponsiveness * Time.deltaTime
            );

            if (IsNormalized(interpolated))
                selectedTransform.rotation = interpolated;
        }
        else
        {
            selectedTransform.position = targetPosition;
            if (IsNormalized(targetRotation))
                selectedTransform.rotation = targetRotation;
        }
    }

    /// <summary>
    /// Snaps the rotation to specified angular increments.
    /// </summary>
    private Quaternion GetSnappedRotation()
    {
        Quaternion rawRotation = cam.transform.rotation * rotationOffset;
        Vector3 euler = rawRotation.eulerAngles;

        if (rotationSnapDegrees.x > 0f) euler.x = Mathf.Round(euler.x / rotationSnapDegrees.x) * rotationSnapDegrees.x;
        if (rotationSnapDegrees.y > 0f) euler.y = Mathf.Round(euler.y / rotationSnapDegrees.y) * rotationSnapDegrees.y;
        if (rotationSnapDegrees.z > 0f) euler.z = Mathf.Round(euler.z / rotationSnapDegrees.z) * rotationSnapDegrees.z;

        return Quaternion.Euler(euler);
    }

    /// <summary>
    /// Disables the Rigidbody on the object if it exists, and stores its original state.
    /// </summary>
    private void DisableRigidbodyIfPresent(GameObject obj)
    {
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            originalRigidbody = rb;
            originalKinematicState = rb.isKinematic;
            rb.isKinematic = true;
        }
    }

    /// <summary>
    /// Restores the original Rigidbody state if one was modified.
    /// </summary>
    private void RestoreRigidbodyIfPresent()
    {
        if (originalRigidbody != null)
        {
            originalRigidbody.isKinematic = originalKinematicState;
            originalRigidbody = null;
        }
    }

    /// <summary>
    /// Snaps a position to the configured grid size and center.
    /// </summary>
    private Vector3 SnapToGrid(Vector3 position)
    {
        Vector3 localPos = position - gridCenter;

        if (gridSize.x > 0f) localPos.x = Mathf.Round(localPos.x / gridSize.x) * gridSize.x;
        if (gridSize.y > 0f) localPos.y = Mathf.Round(localPos.y / gridSize.y) * gridSize.y;
        if (gridSize.z > 0f) localPos.z = Mathf.Round(localPos.z / gridSize.z) * gridSize.z;

        return localPos + gridCenter;
    }

    /// <summary>
    /// Applies any boundary constraints to the placement position.
    /// </summary>
    private Vector3 ApplyPlacementConstraints(Vector3 position)
    {
        position.y = Mathf.Max(0.5f, position.y); // Prevent sinking below floor
        return position;
    }

    /// <summary>
    /// Returns true if the given quaternion is approximately normalized.
    /// </summary>
    private bool IsNormalized(Quaternion q, float tolerance = 0.0001f)
    {
        float magnitude = Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
        return Mathf.Abs(1f - magnitude) < tolerance;
    }
}
