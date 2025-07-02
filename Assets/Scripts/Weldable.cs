using System.Collections.Generic;
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

    /// <summary>
    /// Weld this object to another Weldable.
    /// Sets the other as parent and adds connection.
    /// </summary>
    /// <param name="weldTo">Weldable to weld to.</param>
    public void Weld(Weldable weldTo)
    {
        if (weldTo == null) return;

        transform.SetParent(weldTo.transform);
        AddConnection(weldTo);
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
