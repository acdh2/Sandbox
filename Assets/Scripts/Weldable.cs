using System.Collections.Generic;
using UnityEngine;

public enum WeldMode
{
    None,
    AttachableOnly,
    ReceivableOnly,
    Both
}

public enum WeldType
{
    Undefined,
    HierarchyBased,
    PhysicsBased
}

[DisallowMultipleComponent]
[RequireComponent(typeof(Selectable))]
public class Weldable : MonoBehaviour
{
    [SerializeField]
    private WeldMode mode = WeldMode.Both;

    private WeldType currentType = WeldType.Undefined;
    private readonly HashSet<Weldable> connections = new();

    public bool CanAttach => mode == WeldMode.AttachableOnly || mode == WeldMode.Both;
    public bool CanReceive => mode == WeldMode.ReceivableOnly || mode == WeldMode.Both;

    public WeldType CurrentType => currentType;


    public void WeldTo(Weldable target, WeldType weldType)
    {
        if (target == null || target == this) return;

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

        if (IsConnected(target)) return;

        AddConnection(target);
        target.AddConnection(this);

        // Voer de daadwerkelijke weld uit ná de verbinding
        if (weldType == WeldType.HierarchyBased)
        {
            ApplyHierarchyWeld(target);
        }
        else if (weldType == WeldType.PhysicsBased)
        {
            ApplyPhysicsWeld(target);
        }

        // Events NA succesvol maken van groep
        if (wasIsolated)
            NotifyOnWeld();

        if (targetWasIsolated)
            target.NotifyOnWeld();
    }
    public void Unweld()
    {
        bool wasGrouped = connections.Count > 0;
        if (wasGrouped)
            NotifyOnUnweld();

        foreach (var connected in connections)
        {
            if (connected.connections.Remove(this))
            {
                if (connected.connections.Count == 0)
                    connected.NotifyOnUnweld();
            }
        }

        // Ruim fysieke of hiërarchische weld op
        if (currentType == WeldType.HierarchyBased)
            RemoveHierarchyWelds(connections);
        else if (currentType == WeldType.PhysicsBased)
            RemovePhysicsWelds(connections);

        connections.Clear();
        currentType = WeldType.Undefined;
    }


    private static Rigidbody GetOrAddRigidbody(GameObject obj)
    {
        var rb = obj.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = obj.AddComponent<Rigidbody>();
            rb.mass = 1f; // standaardmassa
        }

        return rb;
    }

    private void ApplyHierarchyWeld(Weldable target)
    {
        if (transform.parent == null)
        {
            transform.SetParent(target.transform, true);
        }
        else
        {
            target.transform.root.SetParent(transform, true);
        }
    }

    private void ApplyPhysicsWeld(Weldable target)
    {
        Rigidbody thisRb = GetOrAddRigidbody(gameObject);
        Rigidbody targetRb = GetOrAddRigidbody(target.gameObject);

        FixedJoint joint = gameObject.AddComponent<FixedJoint>();
        joint.connectedBody = targetRb;
    }

    private void RemoveHierarchyWelds(IEnumerable<Weldable> connectedWeldables)
    {
        transform.SetParent(null, true);

        foreach (var other in connectedWeldables)
        {
            if (other.transform.root == transform.root)
            {
                other.transform.SetParent(null, true);
            }
        }
    }

    private void RemovePhysicsWelds(IEnumerable<Weldable> connectedWeldables)
    {
        foreach (var other in connectedWeldables)
        {
            foreach (var joint in other.GetComponents<FixedJoint>())
            {
                if (joint.connectedBody == GetComponent<Rigidbody>())
                {
                    Object.Destroy(joint);
                }
            }
        }

        // Verwijder alle eigen joints
        foreach (var joint in GetComponents<FixedJoint>())
        {
            Object.Destroy(joint);
        }
    }

    /// <summary>
    /// Haal directe verbonden objecten op.
    /// </summary>
    public IReadOnlyCollection<Weldable> GetDirectConnections() => connections;

    /// <summary>
    /// Haal alle verbonden objecten op (ook indirect), exclusief self.
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
        if (currentType == WeldType.Undefined)
        {
            currentType = newType;
            return true;
        }

        return currentType == newType;
    }

    public IEnumerable<IWeldListener> GetDescendantWeldListeners()
    {
        foreach (var listener in GetComponents<IWeldListener>())
        {
            yield return listener;
        }

        var stack = new Stack<Transform>();
        stack.Push(transform);

        while (stack.Count > 0)
        {
            var current = stack.Pop();

            foreach (Transform child in current)
            {
                // Als het kind een Weldable is, niet dieper zoeken
                if (child.GetComponent<Weldable>() != null)
                    continue;

                // Alle IWeldListeners op dit GameObject verzamelen
                foreach (var listener in child.GetComponents<IWeldListener>())
                {
                    yield return listener;
                }

                // Verder zoeken in de kinderen van dit kind
                stack.Push(child);
            }
        }
    }

    public void NotifyOnWeld()
    {
        foreach (var listener in GetDescendantWeldListeners())
        {
            listener.OnWeld();
        }
    }

    public void NotifyOnUnweld()
    {
        foreach (var listener in GetDescendantWeldListeners())
        {
            listener.OnUnweld();
        }
    }
    
}
