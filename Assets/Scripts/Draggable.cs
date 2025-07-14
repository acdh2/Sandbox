using UnityEngine;

public class Draggable : MonoBehaviour
{
    private bool isBeingDragged = false;
    private Rigidbody rigidBody;

    private Matrix4x4 offsetMatrix;

    void Awake()
    {
        rigidBody = GetComponent<Rigidbody>();
    }

    public void StartDrag(Vector3 position, Quaternion rotation)
    {
        isBeingDragged = true;
    }

    public void UpdateDrag(Vector3 position, Quaternion rotation)
    {
        if (isBeingDragged)
        {
            if (!enabled)
            {
                EndDrag();
                return;
            }
        }
        else
        {
            return;
        }
        
        if (rigidBody)
        {
            MoveRigidbody(position, rotation);
        }
        else
        {
            MoveTransform(position, rotation);
        }
    }

    private void MoveTransform(Vector3 position, Quaternion rotation)
    {
        if (transform.parent == null)
        {
            transform.position = position;
            transform.rotation = rotation;
        }
        else
        {
            // Bereken de offset van dit object t.o.v. de root
            Transform root = transform.root;

            Matrix4x4 currentLocalMatrix = root.worldToLocalMatrix * transform.localToWorldMatrix;
            Matrix4x4 desiredWorldMatrix = Matrix4x4.TRS(position, rotation, transform.lossyScale);
            Matrix4x4 newRootWorldMatrix = desiredWorldMatrix * currentLocalMatrix.inverse;

            root.position = newRootWorldMatrix.GetColumn(3);
            root.rotation = Quaternion.LookRotation(
                newRootWorldMatrix.GetColumn(2),
                newRootWorldMatrix.GetColumn(1)
            );
        }
    }

    private void MoveRigidbody(Vector3 position, Quaternion rotation)
    {
        rigidBody.MoveRotation(rotation);
        rigidBody.MovePosition(position);
        rigidBody.isKinematic = true;
    }

    public void EndDrag()
    {
        if (rigidBody)
        {
            rigidBody.isKinematic = false;
        }
        isBeingDragged = false;
    }
}
