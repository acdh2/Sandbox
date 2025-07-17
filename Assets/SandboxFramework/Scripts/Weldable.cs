using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Defines how this object can participate in welding.
/// </summary>
[Serializable]
public enum WeldMode
{
    None,
    AttachableOnly,
    ReceivableOnly,
    Both
}

/// <summary>
/// Defines the mechanism of the welding (hierarchical or physical).
/// </summary>
public enum WeldType
{
    Undefined,
    HierarchyBased,
    PhysicsBased
}

[DisallowMultipleComponent]
public class Weldable : MonoBehaviour
{
    [Tooltip("Defines if the object can be welded or receive welds")]
    [SerializeField]
    private WeldMode weldMode = WeldMode.Both;

    // public bool CanBeAttached
    // {
    //     set
    //     {
    //         if (value && CanReceive) weldMode = WeldMode.Both;
    //         if (value && !CanReceive) weldMode = WeldMode.AttachableOnly;
    //         if (CanReceive) weldMode = WeldMode.ReceivableOnly;
    //         weldMode = WeldMode.None;
    //     }
    // }

    public void SetWeldMode(WeldMode value)
    {
        this.weldMode = value;
    }

    private WeldType currentWeldType = WeldType.Undefined;
    //private readonly HashSet<Weldable> connections = new();
    public List<Weldable> connections = new();

    public bool CanAttach
    {
        get => weldMode == WeldMode.AttachableOnly || weldMode == WeldMode.Both;
        set => weldMode = value
            ? (CanReceive ? WeldMode.Both : WeldMode.AttachableOnly)
            : (CanReceive ? WeldMode.ReceivableOnly : WeldMode.None);
    }

    public bool CanReceive
    {
        get => weldMode == WeldMode.ReceivableOnly || weldMode == WeldMode.Both;
        set => weldMode = value
            ? (CanAttach ? WeldMode.Both : WeldMode.ReceivableOnly)
            : (CanAttach ? WeldMode.AttachableOnly : WeldMode.None);
    }

    public WeldType CurrentWeldType => currentWeldType;

    private void TryAutoHierarchyWeldWithAncestor()
    {
        Transform current = transform.parent;

        while (current != null)
        {
            var parentWeldable = current.GetComponent<Weldable>();
            if (parentWeldable != null)
            {
                // Check of deze weldable nog niet al verbonden is
                if (!IsConnected(parentWeldable))
                {
                    // Alleen weld uitvoeren als modes compatibel zijn
                    if (CanAttach && parentWeldable.CanReceive)
                    {
                        WeldTo(parentWeldable, WeldType.HierarchyBased, true);
                    }
                }

                break; // Alleen eerste voorouder gebruiken
            }

            current = current.parent;
        }
    }

    private System.Collections.IEnumerator Start()
    {
        yield return null;
        TryAutoHierarchyWeldWithAncestor();
    }    

    /// <summary>
    /// Welds this object to a target using the specified weld type.
    /// </summary>
    internal void WeldTo(Weldable target, WeldType weldType, bool isAutoWeld=false)
    {
        if (target == null || target == this)
            return;

        if (!CanAttach || !target.CanReceive)
        {
            Debug.LogWarning($"Weld failed: mode mismatch ({name} ➜ {target.name})");
            return;
        }

        bool wasIsolated = connections.Count == 0;
        bool targetWasIsolated = target.connections.Count == 0;

        if (!TrySetWeldType(weldType) || !target.TrySetWeldType(weldType))
        {
            Debug.LogWarning($"Weld failed: type mismatch ({name} ↔ {target.name})");
            return;
        }

        if (IsConnected(target))
        {
            Debug.LogWarning("Already connected");
            return;
        }

        if (target.IsConnected(this))
            {
                Debug.LogError("One-sided connection detected");
            }

        AddConnection(target);
        target.AddConnection(this);

        // Perform weld action after establishing the connection
        if (weldType == WeldType.HierarchyBased && !isAutoWeld)
        {
            ApplyHierarchyWeld(target);
        }
        else if (weldType == WeldType.PhysicsBased)
        {
            ApplyPhysicsWeld(target);
        }

        // Notify only when groups are formed
        NotifyOnWeld(wasIsolated);
        target.NotifyOnWeld(targetWasIsolated);
    }

    /// <summary>
    /// Unwelds this object from all connections.
    /// </summary>
    internal void Unweld()
    {
        bool wasGrouped = connections.Count > 0;
        NotifyOnUnweld(wasGrouped);

        foreach (var connected in connections)
        {
            if (connected.connections.Remove(this))
            {
                bool connectionIsIsolatedAfterUnweld = connected.connections.Count == 0;
                connected.NotifyOnUnweld(connectionIsIsolatedAfterUnweld);
            }
        }

        if (currentWeldType == WeldType.HierarchyBased)
            RemoveHierarchyWelds(connections);
        else if (currentWeldType == WeldType.PhysicsBased)
            RemovePhysicsWelds(connections);

        connections.Clear();
        currentWeldType = WeldType.Undefined;
    }

    private static Rigidbody GetOrAddRigidbody(GameObject obj)
    {
        var rb = obj.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = obj.AddComponent<Rigidbody>();
            rb.mass = 1f; // Default mass
        }

        return rb;
    }


    private Weldable GetRootWeldable()
    {
        Transform current = transform;
        Weldable lastFound = null;

        while (current != null)
        {
            var weldable = current.GetComponent<Weldable>();
            if (weldable != null && weldable.CanAttach)
                lastFound = weldable;

            current = current.parent;
        }

        return lastFound;
    }

    private List<Weldable> GetChildWeldables()
    {
        var result = new List<Weldable>();

        var stack = new Stack<Transform>();
        foreach (Transform child in transform)
        {
            stack.Push(child);
        }

        while (stack.Count > 0)
        {
            Transform current = stack.Pop();
            Weldable weldable = current.GetComponent<Weldable>();
            if (weldable)
            {
                result.Add(weldable);
            }
            else
            {
                foreach (Transform child in current)
                {
                    stack.Push(child);
                }
            }
        }
        return result;
    }

    public void RefreshConnectionsFromHierarchyRoot()
    {
        Weldable root = GetRootWeldable();

        var stack = new Stack<Weldable>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            var currentWeldable = stack.Pop();
            currentWeldable.connections.Clear();

            foreach (Weldable weldable in currentWeldable.GetChildWeldables())
            {
                currentWeldable.connections.Add(weldable);
                stack.Push(weldable);
            }
        }
    }

    private void SwapParent()
    {
        if (transform.parent == null) return;

        Weldable weldableParent = transform.parent.GetComponentInParent<Weldable>();
        if (weldableParent)
        {
            weldableParent.SwapParent();
            weldableParent.transform.SetParent(transform, true);
        }
    }

    /// <summary>
    /// Reparents all weldable ancestors under each other, ending with the selected object.
    /// </summary>
    public void ReparentWeldableAncestors()
    {
        Weldable root = this;
        if (root == null) return;

        List<Transform> weldableAncestors = new();
        Transform current = root.transform;

        while (current != null)
        {
            Weldable weldable = current.GetComponent<Weldable>();
            if (weldable && weldable.CanAttach && weldable.CanReceive)
            {
                weldableAncestors.Add(current);
            }
            current = current.parent;
        }

        foreach (Transform transform in weldableAncestors)
            transform.SetParent(null, true);

        for (int i = weldableAncestors.Count - 1; i > 0; i--)
            weldableAncestors[i].SetParent(weldableAncestors[i - 1], true);
    }    

    private void ApplyHierarchyWeld(Weldable target)
    {
        if (transform.parent != null)
        {
            ReparentWeldableAncestors();
        }
        transform.SetParent(target.transform, true);
    }

    private void ApplyHierarchyWeld2(Weldable target)
    {
        if (transform.parent != null)
        {
            Transform currentRoot = transform.root;
            List<Transform> children = new List<Transform>();
            foreach (Weldable weldable in GetComponentsInChildren<Weldable>())
            {
                children.Add(weldable.transform);
            }
            foreach (Transform child in children)
            {
                child.SetParent(transform.parent, true);
            }
            transform.SetParent(null, true);
            currentRoot.SetParent(transform, true);
        }

        transform.SetParent(target.transform, true);
    }

    private void ApplyPhysicsWeld(Weldable target)
    {
        Rigidbody thisRb = GetOrAddRigidbody(gameObject);
        Rigidbody targetRb = GetOrAddRigidbody(target.gameObject);

        FixedJoint joint = gameObject.AddComponent<FixedJoint>();
        joint.connectedBody = targetRb;
    }

    private void RemoveHierarchyWelds(IEnumerable<Weldable> connected)
    {
        List<Weldable> children = GetChildWeldables();
        transform.SetParent(null, true);
        foreach (Weldable weldable in children)
        {
            weldable.transform.SetParent(null, true);
        }
        //RefreshConnectionsFromHierarchyRoot();
    }

    private void RemovePhysicsWelds(IEnumerable<Weldable> connected)
    {
        foreach (var other in connected)
        {
            foreach (var joint in other.GetComponents<FixedJoint>())
            {
                if (joint.connectedBody == GetComponent<Rigidbody>())
                    Destroy(joint);
            }
        }

        foreach (var joint in GetComponents<FixedJoint>())
        {
            Destroy(joint);
        }
    }

    /// <summary>
    /// Gets all directly connected weldables.
    /// </summary>
    internal IReadOnlyCollection<Weldable> GetDirectConnections() => connections;

    /// <summary>
    /// Gets all connected weldables recursively (excluding self).
    /// </summary>
    internal HashSet<Weldable> GetAllConnectedRecursive()
    {
        var result = new HashSet<Weldable>();
        var stack = new Stack<Weldable>();
        stack.Push(this);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            foreach (var conn in current.connections)
            {
                if (conn != this && result.Add(conn))
                {
                    stack.Push(conn);
                }
            }
        }

        return result;
    }

    internal bool IsConnected(Weldable other)
    {
        return connections.Contains(other);
    }

    private void AddConnection(Weldable other)
    {
        if (other != null && other != this)
        {
            connections.Add(other);
        }
    }

    private bool TrySetWeldType(WeldType newType)
    {
        if (currentWeldType == WeldType.Undefined)
        {
            currentWeldType = newType;
            return true;
        }

        return currentWeldType == newType;
    }

    /// <summary>
    /// Retrieves all IWeldListener components in this object and its descendants,
    /// skipping children with their own Weldable.
    /// </summary>
    private IEnumerable<IWeldListener> GetDescendantWeldListeners()
    {
        foreach (var listener in GetComponents<IWeldListener>())
            yield return listener;

        var stack = new Stack<Transform>();
        stack.Push(transform);

        while (stack.Count > 0)
        {
            var current = stack.Pop();

            foreach (Transform child in current)
            {
                if (child.GetComponent<Weldable>() != null)
                    continue;

                foreach (var listener in child.GetComponents<IWeldListener>())
                    yield return listener;

                stack.Push(child);
            }
        }
    }

    private void NotifyOnWeld(bool joinedWeldGroup)
    {
        foreach (var listener in GetDescendantWeldListeners())
        {
            if (joinedWeldGroup) listener.OnWeld();
            listener.OnAdded();
        }
    }

    private void NotifyOnUnweld(bool leavedWeldGroup)
    {
        foreach (var listener in GetDescendantWeldListeners())
        {
            listener.OnRemoved();
            if (leavedWeldGroup) listener.OnUnweld();
        }
    }
}
