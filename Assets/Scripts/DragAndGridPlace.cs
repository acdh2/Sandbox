using UnityEngine;

[RequireComponent(typeof(Transform))]
public class DragAndGridPlace : MonoBehaviour
{
    public LayerMask selectableLayers;

    public Vector3 gridSize = Vector3.one;
    public Vector3 gridCenter = Vector3.zero;

    public bool enableRotation = true;
    public Vector3 rotationSnapDegrees = Vector3.zero;

    private bool smoothMovement = false;
    private float moveResponsiveness = 100f;

    private bool isPressed = false;

    private Camera cam;
    private Rigidbody selectedRb;
    private Vector3 localOffset;
    private Quaternion rotationOffset;
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private Collider selectedCollider;

    private Material originalMaterial;

    void Start()
    {
        cam = Camera.main;
    }

    void SetRendererColor(Collider col, Color color)
    {
        if (col == null) return;
        if (originalMaterial != null) return;

        Renderer renderer = col.gameObject.GetComponent<Renderer>();
        if (renderer == null) return;

        originalMaterial = renderer.sharedMaterial;
        renderer.material.color = color;
    }

    void ResetRendererColor(Collider col)
    {
        if (col == null) return;

        Renderer renderer = col.gameObject.GetComponent<Renderer>();
        if (renderer == null) return;

        if (originalMaterial == null) return;
        renderer.sharedMaterial = originalMaterial;
        originalMaterial = null;
    }    

    void SetSelection(Collider selection) {
        if (selectedCollider != selection) {
            ResetRendererColor(selectedCollider);
            selectedCollider = selection;
            SetRendererColor(selectedCollider, Color.red);
        }
    }

    void LateUpdate()
    {
        if (!Input.GetMouseButton(0)) {
            HandleSelection();
        }

        if (Input.GetMouseButtonDown(0)) {
            HandleDragStart();
            isPressed = true;
        }
        else if (Input.GetMouseButton(0) && selectedRb != null) {
            UpdateTargetTransform();
        }
        else if (Input.GetMouseButtonUp(0)) {
            HandleDragEnd();
            isPressed = false;
        }
    }

    void HandleSelection() 
    {
        //if (Input.GetMouseButton(0) {
            //SetSelection(null);
        //}
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, selectableLayers)) {
            SetSelection(hit.collider);
        } else {
            SetSelection(null);
        }
    }

    void FixedUpdate()
    {
        if (isPressed && selectedRb != null)
            ApplySmoothMovement();
    }


    private void HandleDragStart()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (selectedCollider != null)
        {
            var rb = selectedCollider.attachedRigidbody;
            if (rb != null && rb.isKinematic)
            {
                selectedRb = rb;
                Vector3 worldPos = selectedRb.transform.position;
                localOffset = cam.transform.InverseTransformPoint(worldPos);

                if (enableRotation)
                {
                    rotationOffset = Quaternion.Inverse(cam.transform.rotation) * selectedRb.rotation;
                }
            }
        }
    }

    private void UpdateTargetTransform()
    {
        Vector3 worldTargetPos = cam.transform.TransformPoint(localOffset);
        targetPosition = SnapToGrid(worldTargetPos);
        targetPosition = ApplyBoundaries(targetPosition);

        if (enableRotation)
        {
            Quaternion desiredRot = cam.transform.rotation * rotationOffset;
            Vector3 euler = desiredRot.eulerAngles;

            if (rotationSnapDegrees.x > 0f) euler.x = Mathf.Round(euler.x / rotationSnapDegrees.x) * rotationSnapDegrees.x;
            if (rotationSnapDegrees.y > 0f) euler.y = Mathf.Round(euler.y / rotationSnapDegrees.y) * rotationSnapDegrees.y;
            if (rotationSnapDegrees.z > 0f) euler.z = Mathf.Round(euler.z / rotationSnapDegrees.z) * rotationSnapDegrees.z;

            targetRotation = Quaternion.Euler(euler);
        }
        else
        {
            targetRotation = selectedRb.rotation;
        }
    }

    private bool isNormalized(Quaternion q, float tolerance = 0.0001f)
    {
        float magnitude = Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
        return Mathf.Abs(1f - magnitude) < tolerance;
    }

    private void ApplySmoothMovement()
    {
        if (smoothMovement)
        {
            selectedRb.MovePosition(Vector3.Lerp(selectedRb.position, targetPosition, moveResponsiveness * Time.fixedDeltaTime));
            Quaternion quaternion = Quaternion.Slerp(selectedRb.rotation, targetRotation, moveResponsiveness * Time.fixedDeltaTime);
            if (isNormalized(quaternion)) {
                selectedRb.MoveRotation(quaternion);
            }
        }
        else
        {
            selectedRb.MovePosition(targetPosition);
            if (isNormalized(targetRotation)) {
                selectedRb.MoveRotation(targetRotation);
            }
        }
    }

    private void HandleDragEnd()
    {
        selectedRb = null;
    }

    private Vector3 SnapToGrid(Vector3 position)
    {
        Vector3 localPos = position - gridCenter;

        if (gridSize.x > 0f) localPos.x = Mathf.Round(localPos.x / gridSize.x) * gridSize.x;
        if (gridSize.y > 0f) localPos.y = Mathf.Round(localPos.y / gridSize.y) * gridSize.y;
        if (gridSize.z > 0f) localPos.z = Mathf.Round(localPos.z / gridSize.z) * gridSize.z;

        return localPos + gridCenter;
    }

    private Vector3 ApplyBoundaries(Vector3 position) {
        Vector3 localPos = position;

        if (localPos.y < 0.5f) {
            localPos.y = 0.5f;
        }

        return localPos;
    }
}
