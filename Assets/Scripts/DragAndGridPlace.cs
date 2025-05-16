using UnityEngine;

[RequireComponent(typeof(Transform))]
public class DragAndGridPlace : MonoBehaviour
{
    public LayerMask selectableLayers;
    public Vector3 gridSize = Vector3.one;
    public Vector3 gridCenter = Vector3.zero;

    public bool enableRotation = true;
    public Vector3 rotationSnapDegrees = Vector3.zero;

    private Camera cam;
    private Rigidbody selectedRb;
    private Vector3 localOffset;
    private Quaternion rotationOffset;

    void Start()
    {
        cam = Camera.main;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, selectableLayers))
            {
                var rb = hit.collider.attachedRigidbody;
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

        if (Input.GetMouseButton(0) && selectedRb != null)
        {
            // Verplaatsing
            Vector3 worldTargetPos = cam.transform.TransformPoint(localOffset);
            selectedRb.MovePosition(SnapToGrid(worldTargetPos));

            // Rotatie (optioneel)
            if (enableRotation)
            {
                Quaternion targetRot = cam.transform.rotation * rotationOffset;

                if (rotationSnapDegrees != Vector3.zero)
                {
                    Vector3 euler = targetRot.eulerAngles;
                    euler.x = Mathf.Round(euler.x / rotationSnapDegrees.x) * rotationSnapDegrees.x;
                    euler.y = Mathf.Round(euler.y / rotationSnapDegrees.y) * rotationSnapDegrees.y;
                    euler.z = Mathf.Round(euler.z / rotationSnapDegrees.z) * rotationSnapDegrees.z;
                    targetRot = Quaternion.Euler(euler);
                }

                //Quaternion newRot = Quaternion.Slerp(selectedRb.rotation, targetRot, Time.deltaTime * rotationLerpSpeed);
                selectedRb.MoveRotation(targetRot);
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            selectedRb = null;
        }
    }

    Vector3 SnapToGrid(Vector3 position)
    {
        Vector3 localPos = position - gridCenter;
        if (gridSize.x > 0f) localPos.x = Mathf.Round(localPos.x / gridSize.x) * gridSize.x;
        if (gridSize.y > 0f) localPos.y = Mathf.Round(localPos.y / gridSize.y) * gridSize.y;
        if (gridSize.z > 0f) localPos.z = Mathf.Round(localPos.z / gridSize.z) * gridSize.z;
        return gridCenter + localPos;
    }
}
