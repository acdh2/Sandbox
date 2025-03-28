using UnityEngine;
using UnityEngine.InputSystem;
public class ObjectDragger : MonoBehaviour
{
    public LayerMask draggableLayer;
    public float maxGrabDistance = 5f;
    public float MaxAllowedPenetration = 0.025f;

    private Camera playerCamera;
    private Rigidbody grabbedObject;
    private ConfigurableJoint grabJoint; // Change from FixedJoint to ConfigurableJoint
    private GameObject anchorObject;
    private Rigidbody anchorRigidbody;
    private Vector3 dragTargetPosition;
    private Quaternion dragTargetRotation;
    private RigidbodyInterpolation stackedInterpolationMode = RigidbodyInterpolation.None;
    
    void Start()
    {
        playerCamera = Camera.main;
        
        // Create an invisible kinematic object to act as the anchor
        anchorObject = new GameObject("DragAnchor");
        anchorRigidbody = anchorObject.AddComponent<Rigidbody>();
        anchorRigidbody.isKinematic = true;
        anchorRigidbody.useGravity = false;
        
        grabJoint = anchorObject.AddComponent<ConfigurableJoint>(); // Using ConfigurableJoint
        grabJoint.connectedBody = null;
        grabJoint.anchor = Vector3.zero;
        grabJoint.connectedAnchor = Vector3.zero;

        // Set up ConfigurableJoint
        grabJoint.xMotion = ConfigurableJointMotion.Limited;
        grabJoint.yMotion = ConfigurableJointMotion.Limited;
        grabJoint.zMotion = ConfigurableJointMotion.Limited;
        grabJoint.angularXMotion = ConfigurableJointMotion.Limited;
        grabJoint.angularYMotion = ConfigurableJointMotion.Limited;
        grabJoint.angularZMotion = ConfigurableJointMotion.Limited;
    }

  public bool ResolvePenetrationsForRigidbody(Rigidbody rigidbody)
    {
        // Find all colliders attached to the Rigidbody and its children
        Collider[] colliders = rigidbody.GetComponentsInChildren<Collider>();

        if (colliders.Length == 0)
        {
            Debug.LogWarning("The object does not have any colliders.");
            return false;
        }

        // Query for all colliders within the bounding box of the object
        Bounds bounds = new Bounds(rigidbody.transform.position, Vector3.zero);

        // Combine bounds of all colliders to get the full volume of the object
        foreach (var col in colliders)
        {
            bounds.Encapsulate(col.bounds);
        }

        // Query for all colliders within the bounding box of the object
        Collider[] overlappingColliders = Physics.OverlapBox(bounds.center, bounds.extents);

        bool result = false;

        // Iterate over each overlapping collider and compute the penetration distance
        foreach (var collider in overlappingColliders)
        {
            // Ignore the object's own colliders
            bool isObjectCollider = false;
            foreach (var objectCollider in colliders)
            {
                if (collider == objectCollider)
                {
                    isObjectCollider = true;
                    break;
                }
            }

            if (isObjectCollider) continue;

            // Compute the penetration
            bool overlapped = Physics.ComputePenetration(
                colliders[0], rigidbody.transform.position, rigidbody.transform.rotation,
                collider, collider.transform.position, collider.transform.rotation,
                out Vector3 direction, out float distance
            );

            if (overlapped && (distance > MaxAllowedPenetration))
            {
                // Print out the distance (penetration depth)
                Debug.Log($"Overlap detected with {collider.gameObject.name}. Penetration distance: {distance}");

                // Resolve the penetration by moving the object out of the collider
                Vector3 resolveDirection = direction.normalized;
                Vector3 newPosition = rigidbody.transform.position + resolveDirection * distance;

                // Apply the position correction (you can also use Rigidbody.MovePosition or manually move it here)
                rigidbody.MovePosition(newPosition);

                Debug.Log($"Resolved penetration with {collider.gameObject.name}. Moved to {newPosition}");
                result = true;
            }
        }

        return result;
    }

    void Update()
    {
        if ((grabbedObject == null) && (Input.GetMouseButtonDown(0)))
        {
            TryGrabObject();
        }
        else if ((grabbedObject != null) && (Input.GetMouseButtonUp(0) || !Screen.safeArea.Contains(Input.mousePosition)))
        {
            ReleaseObject();
        }
    }


    void LateUpdate()
    {
        if (grabbedObject)
        {
            // Move the anchor object to match the camera position
            Vector3 worldTarget = playerCamera.transform.TransformPoint(dragTargetPosition);        
            anchorRigidbody.MovePosition(worldTarget);

            Quaternion worldTargetRotation = playerCamera.transform.rotation * dragTargetRotation;
            anchorRigidbody.MoveRotation(worldTargetRotation);

            if (ResolvePenetrationsForRigidbody(grabbedObject)) {
                ReleaseObject();
            }

        }

    }

    void TryGrabObject()
    {
        if (grabbedObject != null) return;

        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, maxGrabDistance, draggableLayer))
        {
            Rigidbody rb = hit.collider.attachedRigidbody;
            if (rb != null && grabbedObject == null)
            {
                grabbedObject = rb;
                stackedInterpolationMode = grabbedObject.interpolation;
                grabbedObject.interpolation = RigidbodyInterpolation.Interpolate;
                
                // Move the anchor object to the hit point
                anchorObject.transform.position = hit.point;
                dragTargetPosition = playerCamera.transform.InverseTransformPoint(hit.point);
                dragTargetRotation = Quaternion.Inverse(playerCamera.transform.rotation) * anchorRigidbody.transform.rotation;
                
                Vector3 localHitPoint = rb.transform.InverseTransformPoint(hit.point);

                // Set the connected body and anchors
                grabJoint.connectedBody = grabbedObject;
                grabJoint.connectedAnchor = localHitPoint;

                grabbedObject.WakeUp();
            }
        }
    }

    void ReleaseObject()
    {
        if (grabbedObject == null) return;

        grabbedObject.interpolation = stackedInterpolationMode;
        grabJoint.connectedBody = null;
        grabbedObject = null;
    }
}
