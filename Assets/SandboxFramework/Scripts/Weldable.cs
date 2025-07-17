using System;
using System.Collections;
using System.Collections.Generic;
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

    /// <summary>
    /// Defines if the object can be welded or receive welds
    /// </summary>
    public WeldMode WeldMode
    {
        get => weldMode;
        set => weldMode = value;
    }    

    private WeldType currentWeldType = WeldType.Undefined;
    private readonly HashSet<Weldable> connections = new();

    public bool CanAttach => weldMode == WeldMode.AttachableOnly || weldMode == WeldMode.Both;
    public bool CanReceive => weldMode == WeldMode.ReceivableOnly || weldMode == WeldMode.Both;

    public WeldType CurrentWeldType => currentWeldType;

    public void TryAutoHierarchyWeldWithAncestor()
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
                        WeldTo(parentWeldable, WeldType.HierarchyBased);
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
    public void WeldTo(Weldable target, WeldType weldType)
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
            return;

        if (target.IsConnected(this))
        {
            Debug.LogError("One-sided connection detected");
        }

        AddConnection(target);
        target.AddConnection(this);

        // Perform weld action after establishing the connection
        if (weldType == WeldType.HierarchyBased)
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
    public void Unweld()
    {
        bool wasGrouped = connections.Count > 0;
        NotifyOnUnweld(wasGrouped);

        foreach (var connected in connections)
        {
            bool connectionIsIsolatedAfterUnweld = connected.connections.Count == 1;
            if (connected.connections.Remove(this))
            {
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

    private void ApplyHierarchyWeld(Weldable target)
    {
        if (transform.parent != null)
        {
            //make this top of hierarchy
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
        transform.SetParent(null, true);

        foreach (var other in connected)
        {
            if (other.transform.root == transform.root)
            {
                other.transform.SetParent(null, true);
            }
        }
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
    public IReadOnlyCollection<Weldable> GetDirectConnections() => connections;

    /// <summary>
    /// Gets all connected weldables recursively (excluding self).
    /// </summary>
    public HashSet<Weldable> GetAllConnectedRecursive()
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

    public bool IsConnected(Weldable other)
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
    public IEnumerable<IWeldListener> GetDescendantWeldListeners()
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

    public void NotifyOnWeld(bool joinedWeldGroup)
    {
        foreach (var listener in GetDescendantWeldListeners())
        {
            if (joinedWeldGroup) listener.OnWeld();
            listener.OnAdded();
        }
    }

    public void NotifyOnUnweld(bool leavedWeldGroup)
    {
        foreach (var listener in GetDescendantWeldListeners())
        {
            listener.OnRemoved();
            if (leavedWeldGroup) listener.OnUnweld();
        }
    }
}
