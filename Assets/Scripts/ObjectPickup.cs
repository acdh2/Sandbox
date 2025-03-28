using NUnit.Framework.Interfaces;
using UnityEngine;

public class ObjectPickup : MonoBehaviour
{
	public LayerMask pickupLayers;

    public float pickupRange = 5f;
    public float moveSpeed = 10f;

    public float MaxAllowedPenetration = 0.025f;

    private Camera playerCamera;
    private Rigidbody heldObject;
    private Vector3 objectOffset;
    private Quaternion objectRotationOffset;

    private Vector3 previousPosition;
    private Vector3 velocity;
    
    void Start()
    {
        playerCamera = Camera.main;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            TryPickupObject();
        }
        else if (Input.GetMouseButtonUp(0))
        {
            DropObject();
        }
    }

    void FixedUpdate()
    {
        if (heldObject)
        {
            MoveHeldObject();
        }
    }


// Method to check for overlaps and resolve them by adjusting the Rigidbody's position
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

     // Method to be called with a Rigidbody, checking for overlaps and penetration
    private bool CheckPenetrationsForRigidbody(Rigidbody rigidbody)
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

        // Iterate over each overlapping collider and compute the penetration distance
        foreach (var collider in overlappingColliders)
        {
            // Ignore the object's own colliders themselves
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

            if (overlapped)
            {
                if (distance > MaxAllowedPenetration) return true;
            }
        }

        return false;
    }

    void TryPickupObject()
    {
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, pickupRange, pickupLayers))
        {
            Rigidbody rb = hit.rigidbody;
            if (rb != null)
            {
                heldObject = rb;
                heldObject.useGravity = false;
                //heldObject.isKinematic = true;
                objectOffset = playerCamera.transform.InverseTransformPoint(hit.transform.position);
                objectRotationOffset = Quaternion.Inverse(playerCamera.transform.rotation) * hit.transform.rotation;
                previousPosition = heldObject.position;
                velocity = Vector3.zero;
            }
        }
    }

    void MoveHeldObject()
    {
        Vector3 targetPosition = playerCamera.transform.TransformPoint(objectOffset);
        Quaternion targetRotation = playerCamera.transform.rotation * objectRotationOffset;
        heldObject.MovePosition(Vector3.Lerp(heldObject.position, targetPosition, moveSpeed * Time.fixedDeltaTime));
        heldObject.MoveRotation(Quaternion.Slerp(heldObject.rotation, targetRotation, moveSpeed * Time.fixedDeltaTime));

        Vector3 positionDifference = targetPosition - previousPosition;
        velocity += positionDifference;
        velocity *= 0.9f;

        previousPosition = heldObject.position;
        
        if (ResolvePenetrationsForRigidbody(heldObject)) {
            DropObject();
        }
        // if (CheckPenetrationsForRigidbody(heldObject)) {
        //     DropObject();
        // }
    }

    void DropObject()
    {
        if (heldObject)
        {
            heldObject.linearVelocity = velocity;
            heldObject.useGravity = true;
            //heldObject.isKinematic = false;
            heldObject = null;
        }
    }
}
