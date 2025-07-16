using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public enum RigidbodyStateChange
{
    Unchanged,
    SetKinematic,
    SetNonKinematic
}

[RequireComponent(typeof(Selectable))]
[DisallowMultipleComponent]
public class Draggable : MonoBehaviour
{
    public bool shouldPropagateDragEvents = true;

    private bool isBeingDragged = false;
    private Rigidbody rigidBody;

    private Matrix4x4 offsetMatrix;

    public void StartDrag(RigidbodyStateChange stateChange)
    {
        rigidBody = GetComponent<Rigidbody>();
        OnGrab();
        isBeingDragged = true;

        ApplyRigidbodyStateChange(stateChange);
    }

    private void ApplyRigidbodyStateChange(RigidbodyStateChange stateChange)
    {
        if (rigidBody == null) return;
        if (stateChange == RigidbodyStateChange.Unchanged) return;
        rigidBody.isKinematic = stateChange == RigidbodyStateChange.SetKinematic;
    }

    public void UpdateDrag(Vector3 position, Quaternion rotation)
    {
        if (!enabled || !isBeingDragged) return;
        ApplyTransformation(position, rotation);
    }

    private void ApplyTransformation(Vector3 position, Quaternion rotation)
    {

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
    }

    public void EndDrag(RigidbodyStateChange stateChange)
    {
        OnRelease();
        ApplyRigidbodyStateChange(stateChange);        
        isBeingDragged = false;
    }

    private IDragListener[] GetConnectedDragListeners()
    {
        if (shouldPropagateDragEvents)
        {
            Weldable weldable = GetComponentInParent<Weldable>();
            if (weldable)
            {
                return Utils.FindAllInHierarchyAndConnections<IDragListener>(weldable).ToArray();
            }
            return transform.root.GetComponentsInChildren<IDragListener>();
        }
        else
        {
            var result = new List<IDragListener>();
            var stack = new Stack<Transform>();
            stack.Push(transform);

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                if (current != transform && current.GetComponent<Weldable>() != null)
                    continue;
                foreach (var listener in current.GetComponents<IDragListener>())
                {
                    if (listener != null)
                        result.Add(listener);
                }
                foreach (Transform child in current)
                {
                    stack.Push(child);
                }
            }
            return result.ToArray();
        }
    }

    private void OnGrab()
    {
        foreach (DragListener dragListener in GetConnectedDragListeners())
        {
            dragListener.OnGrab();
        }
    }

    private void OnRelease()
    {
        foreach (DragListener dragListener in GetConnectedDragListeners())
        {
            dragListener.OnRelease();
        }
    }

}
