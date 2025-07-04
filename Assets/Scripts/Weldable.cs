using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using UnityEngine;

/// <summary>
/// Defines the welding modes an object can have.
/// </summary>
public enum WeldMode
{
    None,
    AttachableOnly,
    ReceivableOnly,
    Both
}

/// <summary>
/// Component that allows objects to "weld" to each other,
/// managing parent-child relations and notifying listeners.
/// </summary>
[RequireComponent(typeof(Selectable))]
public class Weldable : MonoBehaviour
{
    // Public weld mode setting
    public WeldMode mode = WeldMode.Both;

    // Connections to other Weldables
    private readonly HashSet<Weldable> connectedObjects = new();

    /// <summary>
    /// Whether this object can attach to others.
    /// </summary>
    public bool CanAttach => mode == WeldMode.AttachableOnly || mode == WeldMode.Both;

    /// <summary>
    /// Whether this object can receive attachments.
    /// </summary>
    public bool CanReceive => mode == WeldMode.ReceivableOnly || mode == WeldMode.Both;

    void Start()
    {
        // On start, check if this object is a child of another Weldable.
        // If yes, add this object as connection to that Weldable.
        Transform parent = transform.parent;
        while (parent != null)
        {
            Weldable parentWeld = parent.GetComponent<Weldable>();
            if (parentWeld != null)
            {
                parentWeld.AddConnection(this);
                break;
            }
            parent = parent.parent;
        }
    }

    /// <summary>
    /// Add a connection to another Weldable.
    /// </summary>
    /// <param name="newConnection">The other Weldable to connect to.</param>
    /// <param name="reciprocal">Whether to add this connection reciprocally.</param>
    public void AddConnection(Weldable newConnection, bool reciprocal = true)
    {
        if (newConnection == null || newConnection == this) return;

        if (connectedObjects.Add(newConnection)) // Add returns false if already present
        {
            if (connectedObjects.Count == 1) // First connection
            {
                OnWeld();
            }

            if (reciprocal)
            {
                newConnection.AddConnection(this, false);
            }
        }
    }

    /// <summary>
    /// Remove a connection to another Weldable.
    /// </summary>
    /// <param name="connection">The Weldable to disconnect from.</param>
    /// <param name="reciprocal">Whether to remove this connection reciprocally.</param>
    public void RemoveConnection(Weldable connection, bool reciprocal = true)
    {
        if (connection == null) return;

        if (connectedObjects.Remove(connection))
        {
            if (reciprocal)
            {
                connection.RemoveConnection(this, false);
            }

            if (connectedObjects.Count == 0) // No more connections
            {
                OnUnweld();
            }
        }
    }

    /// <summary>
    /// Collect all IWeldListener components from this object, its parent, and children.
    /// Skips child subtrees that contain another Weldable.
    /// </summary>
    /// <returns>List of IWeldListener components connected to this weldable.</returns>
    private List<IWeldListener> CollectConnectedIWeldListeners()
    {
        var listeners = new List<IWeldListener>();

        // Add listeners from this object
        listeners.AddRange(GetComponents<IWeldListener>());

        // Add listener from parent if exists
        if (transform.parent != null)
        {
            var parentListener = transform.parent.GetComponent<IWeldListener>();
            if (parentListener != null)
            {
                listeners.Add(parentListener);
            }
        }

        // Traverse children, skip subtrees that have a Weldable component
        var stack = new Stack<Transform>();
        foreach (Transform child in transform)
        {
            stack.Push(child);
        }

        while (stack.Count > 0)
        {
            Transform current = stack.Pop();

            if (current.GetComponent<Weldable>() != null)
                continue; // Skip subtree

            listeners.AddRange(current.GetComponents<IWeldListener>());

            foreach (Transform child in current)
            {
                stack.Push(child);
            }
        }

        return listeners;
    }

    /// <summary>
    /// Detaches any child Weldables by removing their parent and cleaning up connections.
    /// </summary>
    private void DetachChildren()
    {
        var foundWeldables = new List<Weldable>();
        var stack = new Stack<Transform>();

        foreach (Transform child in transform)
        {
            stack.Push(child);
        }

        while (stack.Count > 0)
        {
            Transform current = stack.Pop();
            Weldable weldable = current.GetComponent<Weldable>();

            if (weldable != null)
            {
                foundWeldables.Add(weldable);
                // Skip children of this weldable
                continue;
            }

            foreach (Transform child in current)
            {
                stack.Push(child);
            }
        }

        foreach (Weldable weldable in foundWeldables)
        {
            weldable.transform.SetParent(null, true);
            weldable.RemoveConnection(this);
            RemoveConnection(weldable);
        }
    }

    // /// <summary>
    // /// Finds the first Welder in the project
    // /// And uses it to weld this object
    // /// </summary>
    // public void Weld()
    // {
    //     Welder welder = FindAnyObjectByType<Welder>();
    //     if (welder)
    //     {
    //         welder.Weld(gameObject);
    //     }
    // }

    Quaternion RotationFromAxes(Vector3 right, Vector3 up, Vector3 forward)
    {
        // Zorg dat het een orthonormale matrix is
        right = right.normalized;
        forward = forward.normalized;
        up = Vector3.Cross(forward, right).normalized; // Recalculate up to ensure orthogonality
        right = Vector3.Cross(up, forward).normalized; // Recalculate right again
        Matrix4x4 m = new Matrix4x4();
        m.SetColumn(0, new Vector4(right.x, right.y, right.z, 0));
        m.SetColumn(1, new Vector4(up.x, up.y, up.z, 0));
        m.SetColumn(2, new Vector4(forward.x, forward.y, forward.z, 0));
        m.SetColumn(3, new Vector4(0, 0, 0, 1));
        return m.rotation;
    }
    // private void MatchWeldingPoints(Transform other)
    // {
    //     WeldingPoint[] myPoints = GetComponentsInChildren<WeldingPoint>(true);
    //     WeldingPoint[] otherPoints = other.GetComponentsInChildren<WeldingPoint>(true);

    //     float bestDistance = float.MaxValue;
    //     WeldingPoint bestMine = null;
    //     WeldingPoint bestOther = null;

    //     foreach (var myPoint in myPoints)
    //     {
    //         foreach (var otherPoint in otherPoints)
    //         {
    //             float dist = Vector3.Distance(myPoint.transform.position, otherPoint.transform.position);
    //             float combinedRadius = myPoint.radius + otherPoint.radius;

    //             if (dist <= combinedRadius && dist < bestDistance)
    //             {
    //                 bestDistance = dist;
    //                 bestMine = myPoint;
    //                 bestOther = otherPoint;
    //             }
    //         }
    //     }

    //     if (bestMine != null && bestOther != null)
    //     {
    //         // 1. Verplaats positie
    //         Vector3 offset = bestMine.transform.position - bestOther.transform.position;
    //         Transform otherRoot = other.transform.root;
    //         otherRoot.position += offset;

    //         // 2. Verzamel gewenste lokale richtingen (in wereldruimte)
    //         Vector3 targetRight = bestMine.alignX ? bestMine.transform.right : -bestOther.transform.right;
    //         Vector3 targetUp = bestMine.alignY ? bestMine.transform.up : -bestOther.transform.up;
    //         Vector3 targetForward = bestMine.alignZ ? bestMine.transform.forward : -bestOther.transform.forward;

    //         // 3. Zet deze wereldrichtingen om naar local space van bestOther
    //         Vector3 localRight = bestOther.transform.InverseTransformDirection(targetRight);
    //         Vector3 localUp = bestOther.transform.InverseTransformDirection(targetUp);
    //         Vector3 localForward = bestOther.transform.InverseTransformDirection(targetForward);

    //         // 4. Bouw gewenste lokale rotatie op
    //         Quaternion desiredLocalRotation = Quaternion.LookRotation(localForward, localUp);

    //         // 5. Zet gewenste lokale rotatie om naar wereldruimte
    //         Quaternion desiredWorldRotation = bestOther.transform.parent != null
    //             ? bestOther.transform.parent.rotation * desiredLocalRotation
    //             : desiredLocalRotation;

    //         // 6. Bereken delta en pas toe op root
    //         Quaternion delta = desiredWorldRotation * Quaternion.Inverse(bestOther.transform.rotation);
    //         otherRoot.rotation = delta * otherRoot.rotation;
    //     }
    // }


    /// <summary>
    /// Weld this object to another Weldable.
    /// Sets the other as parent and adds connection.
    /// </summary>
    /// <param name="target">Weldable to weld to.</param>
    public void WeldTo(Weldable target)
    {
        if (target == null) return;

        //MatchWeldingPoints(target.transform);
        transform.SetParent(target.transform);
        AddConnection(target);

        //FindAnyObjectByType<DragHandler>().StopDragging(); //FIX DEBUG!!!
    }

    /// <summary>
    /// Unweld this object from its parent and detach children weldables.
    /// </summary>
    public void Unweld()
    {
        if (transform.parent != null)
        {
            Weldable weldableParent = transform.parent.GetComponentInParent<Weldable>(true);
            if (weldableParent != null)
            {
                transform.SetParent(null, true);
                RemoveConnection(weldableParent);
            }
        }

        DetachChildren();
    }

    /// <summary>
    /// Called when the first connection is made.
    /// Notifies all connected IWeldListeners of weld event.
    /// </summary>
    private void OnWeld()
    {
        foreach (IWeldListener listener in CollectConnectedIWeldListeners())
        {
            listener.OnWeld();
        }
    }

    /// <summary>
    /// Called when last connection is removed.
    /// Notifies all connected IWeldListeners of unweld event.
    /// </summary>
    private void OnUnweld()
    {
        foreach (IWeldListener listener in CollectConnectedIWeldListeners())
        {
            listener.OnUnweld();
        }
    }

}
