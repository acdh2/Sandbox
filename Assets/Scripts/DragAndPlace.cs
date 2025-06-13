using UnityEngine;

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

    private void HandleInput()
    {
        switch (currentState)
        {
            case DragState.Idle:
                if (Input.GetMouseButtonDown(0))
                {
                    TryStartDragging();
                }
                break;

            case DragState.Dragging:
                if (Input.GetMouseButtonUp(0))
                {
                    StopDragging();
                }
                else if (Input.GetMouseButton(0))
                {
                    UpdateTargetTransform();
                    MoveSelectedObject();
                }
                break;
        }
    }

    private void TryStartDragging()
    {
        GameObject selectedObject = selectionHandler.CurrentSelection;
        if (selectedObject == null) return;

        selectedTransform = selectedObject.transform;
        localOffset = cam.transform.InverseTransformPoint(selectedTransform.position);
        selectionHandler.LockSelection();

        if (enableRotation)
            rotationOffset = Quaternion.Inverse(cam.transform.rotation) * selectedTransform.rotation;

        DisableRigidbody(selectedObject);

        currentState = DragState.Dragging;
    }

    private void StopDragging()
    {
        EnableRigidbody();

        selectedTransform = null;
        selectionHandler.UnlockSelection();

        currentState = DragState.Idle;
    }

    private void UpdateTargetTransform()
    {
        Vector3 worldTargetPosition = cam.transform.TransformPoint(localOffset);
        targetPosition = ApplyBoundaries(SnapToGrid(worldTargetPosition));

        targetRotation = enableRotation ? GetSnappedRotation() : selectedTransform.rotation;
    }

    private void MoveSelectedObject()
    {
        if (smoothMovement)
        {
            selectedTransform.position = Vector3.Lerp(selectedTransform.position, targetPosition, moveResponsiveness * Time.deltaTime);
            Quaternion interpolated = Quaternion.Slerp(selectedTransform.rotation, targetRotation, moveResponsiveness * Time.deltaTime);
            if (IsNormalized(interpolated)) selectedTransform.rotation = interpolated;
        }
        else
        {
            selectedTransform.position = targetPosition;
            if (IsNormalized(targetRotation)) selectedTransform.rotation = targetRotation;
        }
    }

    private Quaternion GetSnappedRotation()
    {
        Quaternion rawRotation = cam.transform.rotation * rotationOffset;
        Vector3 euler = rawRotation.eulerAngles;

        if (rotationSnapDegrees.x > 0f) euler.x = Mathf.Round(euler.x / rotationSnapDegrees.x) * rotationSnapDegrees.x;
        if (rotationSnapDegrees.y > 0f) euler.y = Mathf.Round(euler.y / rotationSnapDegrees.y) * rotationSnapDegrees.y;
        if (rotationSnapDegrees.z > 0f) euler.z = Mathf.Round(euler.z / rotationSnapDegrees.z) * rotationSnapDegrees.z;

        return Quaternion.Euler(euler);
    }

    private void DisableRigidbody(GameObject obj)
    {
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            originalRigidbody = rb;
            originalKinematicState = rb.isKinematic;
            rb.isKinematic = true;
        }
    }

    private void EnableRigidbody()
    {
        if (originalRigidbody != null)
        {
            originalRigidbody.isKinematic = originalKinematicState;
            originalRigidbody = null;
        }
    }

    private Vector3 SnapToGrid(Vector3 position)
    {
        Vector3 localPos = position - gridCenter;

        if (gridSize.x > 0f) localPos.x = Mathf.Round(localPos.x / gridSize.x) * gridSize.x;
        if (gridSize.y > 0f) localPos.y = Mathf.Round(localPos.y / gridSize.y) * gridSize.y;
        if (gridSize.z > 0f) localPos.z = Mathf.Round(localPos.z / gridSize.z) * gridSize.z;

        return localPos + gridCenter;
    }

    private Vector3 ApplyBoundaries(Vector3 position)
    {
        position.y = Mathf.Max(0.5f, position.y);
        return position;
    }

    private bool IsNormalized(Quaternion q, float tolerance = 0.0001f)
    {
        float magnitude = Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
        return Mathf.Abs(1f - magnitude) < tolerance;
    }
}
