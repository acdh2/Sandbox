using System.Collections.Generic;
using UnityEngine;

public enum WeldMode
{
    None,
    AttachableOnly,
    ReceivableOnly,
    Both
}

// Nieuwe enum voor weld type
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
    public WeldMode mode = WeldMode.Both;

    private readonly HashSet<Weldable> connectedObjects = new();

    // Nieuwe property: huidige weld type, start op Undefined
    private WeldType currentType = WeldType.Undefined;

    // Properties die je al had, blijven hetzelfde
    public bool CanAttach => mode == WeldMode.AttachableOnly || mode == WeldMode.Both;
    public bool CanReceive => mode == WeldMode.ReceivableOnly || mode == WeldMode.Both;

    private readonly Dictionary<Weldable, FixedJoint> physicsJoints = new();

    void Start()
    {
        // Zoals eerder: check of we genest zijn onder andere Weldable
        Transform parent = transform.parent;
        while (parent != null)
        {
            Weldable parentWeld = parent.GetComponent<Weldable>();
            if (parentWeld != null)
            {
                // Nieuwe regel: als currentType nog undefined, zetten op HierarchyBased
                if (currentType == WeldType.Undefined)
                {
                    currentType = WeldType.HierarchyBased;
                }
                parentWeld.AddConnection(this);
                break;
            }
            parent = parent.parent;
        }
    }

    private bool HasConnection(Weldable target) {
        return connectedObjects.Contains(target);
    }

    public void AddConnection(Weldable newConnection, bool reciprocal = true)
    {
        if (HasConnection(newConnection))
        {
            if (reciprocal) newConnection.AddConnection(this, false);
            return;
        }

        if (newConnection == null || newConnection == this) return;

        if (connectedObjects.Add(newConnection))
        {
            if (connectedObjects.Count == 1)
            {
                OnWeld();
            }

            if (reciprocal)
            {
                newConnection.AddConnection(this, false);
            }
        }
    }

    public void RemoveConnection(Weldable connection, bool reciprocal = true)
    {
        if (connection == null) return;

        if (connectedObjects.Remove(connection))
        {
            if (reciprocal)
            {
                connection.RemoveConnection(this, false);
            }

            if (connectedObjects.Count == 0)
            {
                OnUnweld();

                // Reset currentType als geen verbindingen meer
                currentType = WeldType.Undefined;
            }
        }
    }

    private List<IWeldListener> CollectConnectedIWeldListeners()
    {
        var listeners = new List<IWeldListener>();

        listeners.AddRange(GetComponents<IWeldListener>());

        if (transform.parent != null)
        {
            var parentListener = transform.parent.GetComponent<IWeldListener>();
            if (parentListener != null)
            {
                listeners.Add(parentListener);
            }
        }

        var stack = new Stack<Transform>();
        foreach (Transform child in transform)
        {
            stack.Push(child);
        }

        while (stack.Count > 0)
        {
            Transform current = stack.Pop();

            if (current.GetComponent<Weldable>() != null)
                continue;

            listeners.AddRange(current.GetComponents<IWeldListener>());

            foreach (Transform child in current)
            {
                stack.Push(child);
            }
        }

        return listeners;
    }

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

    public bool TrySetWeldType(WeldType newType)
    {
        if (currentType == WeldType.Undefined)
        {
            currentType = newType;
            return true;
        }
        else if (currentType != newType)
        {
            Debug.LogWarning($"WeldType mismatch: current is {currentType}, tried to set {newType}");
            return false;
        }
        return true;
    }

    private Rigidbody AddRigidbody(GameObject target)
    {
        Rigidbody rb = target.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = target.AddComponent<Rigidbody>();
        }
        return rb;
    }

    private FixedJoint GetFixedJoint(GameObject target)
    {
        FixedJoint joint = target.GetComponent<FixedJoint>();
        if (joint == null)
        {
            joint = target.AddComponent<FixedJoint>();
        }
        return joint;
    }

    /// <summary>
    /// Weld this object to another Weldable using specified WeldType.
    /// </summary>
    /// <param name="target">Weldable to weld to.</param>
    /// <param name="weldType">Type of weld: HierarchyBased or PhysicsBased.</param>
    public void WeldTo(Weldable target, WeldType weldType)
    {
        if (target == null) return;

        if (HasConnection(target)) return;

        if (!TrySetWeldType(weldType) || !target.TrySetWeldType(weldType))
        {
            Debug.LogWarning($"Weld between '{name}' and '{target.name}' failed due to WeldType mismatch.");
            return;
        }

        switch (weldType)
        {
            case WeldType.HierarchyBased:
                // Gebruik oude parent logica
                transform.SetParent(target.transform);
                AddConnection(target);
                break;

            case WeldType.PhysicsBased:
                // Zorg dat beide rigidbodies bestaan
                Rigidbody rbThis = AddRigidbody(gameObject);
                Rigidbody rbTarget = AddRigidbody(target.gameObject);

                // Maak op elk object een FixedJoint naar de ander
                FixedJoint jointToTarget = gameObject.AddComponent<FixedJoint>();
                jointToTarget.connectedBody = rbTarget;
                physicsJoints[target] = jointToTarget;

                // FixedJoint jointToThis = target.gameObject.AddComponent<FixedJoint>();
                // jointToThis.connectedBody = rbThis;
                // target.physicsJoints[this] = jointToThis;

                AddConnection(target);
                break;

            default:
                Debug.LogWarning($"Unsupported WeldType {weldType}");
                break;
        }
    }

    /// <summary>
    /// Unweld this object from its parent or joint, and detach children.
    /// </summary>
    public void Unweld()
    {
        switch (currentType)
        {
            case WeldType.HierarchyBased:
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
                break;

            case WeldType.PhysicsBased:
                // Verwijder alle joints op dit object
                foreach (var joint in physicsJoints.Values)
                {
                    if (joint != null)
                    {
                        Destroy(joint);
                    }
                }

                // Verwijder alle joints op andere objecten die met dit object verbonden zijn
                foreach (var connected in connectedObjects)
                {
                    if (connected.physicsJoints.TryGetValue(this, out var jointToThis))
                    {
                        if (jointToThis != null)
                        {
                            Destroy(jointToThis);
                        }
                        connected.physicsJoints.Remove(this);
                    }
                    connected.RemoveConnection(this, reciprocal: false);
                }

                physicsJoints.Clear();
                connectedObjects.Clear();

                break;

            case WeldType.Undefined:
                // Niks te doen
                break;
        }

        // Reset currentType als geen connecties meer
        if (connectedObjects.Count == 0)
        {
            currentType = WeldType.Undefined;
        }
    }

    private void OnWeld()
    {
        foreach (IWeldListener listener in CollectConnectedIWeldListeners())
        {
            listener.OnWeld();
        }
    }

    private void OnUnweld()
    {
        foreach (IWeldListener listener in CollectConnectedIWeldListeners())
        {
            listener.OnUnweld();
        }
    }

}
