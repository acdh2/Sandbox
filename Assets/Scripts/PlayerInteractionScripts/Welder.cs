using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(SelectionHandler))]
public class Welder : MonoBehaviour
{
    private const float MaxPenetrationThreshold = 0.01f;

    public string weldableTag;

    private SelectionHandler selectionHandler;

    private void Start()
    {
        selectionHandler = GetComponent<SelectionHandler>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
            Weld(selectionHandler.CurrentSelection);

        if (Input.GetKeyDown(KeyCode.U))
            Unweld(selectionHandler.CurrentSelection);
    }

    /// <summary>
    /// Checks if the object is already welded to another weldable.
    /// </summary>
    public bool IsWelded(GameObject obj)
    {
        foreach (Transform child in obj.transform)
            if (IsWeldable(child.gameObject)) return true;

        if (obj.transform.parent != null)
            return GetWeldableAncestor(obj.transform.parent.gameObject) != null;

        return false;
    }

    /// <summary>
    /// Determines if the object can be welded (i.e., has a weldable ancestor).
    /// </summary>
    public bool CanBeWelded(GameObject obj) => GetWeldableAncestor(obj) != null;

    /// <summary>
    /// Checks if the object is weldable by tag.
    /// </summary>
    public bool IsWeldable(GameObject obj) => obj.CompareTag(weldableTag);

    /// <summary>
    /// Finds the nearest weldable ancestor in the hierarchy.
    /// </summary>
    public GameObject GetWeldableAncestor(GameObject obj)
    {
        Transform current = obj.transform;
        while (current != null)
        {
            if (IsWeldable(current.gameObject)) return current.gameObject;
            current = current.parent;
        }
        return null;
    }

    /// <summary>
    /// Finds the top-most weldable ancestor.
    /// </summary>
    public GameObject GetTopmostWeldableAncestor(GameObject obj)
    {
        GameObject topmost = null;
        Transform current = obj.transform;
        while (current != null)
        {
            if (IsWeldable(current.gameObject)) topmost = current.gameObject;
            current = current.parent;
        }
        return topmost;
    }

    /// <summary>
    /// Checks penetration between one collider and nearby colliders.
    /// </summary>
    public Collider[] FindPenetratingColliders(Collider collider)
    {
        Bounds bounds = collider.bounds;
        Collider[] candidates = Physics.OverlapBox(bounds.center, bounds.extents, collider.transform.rotation);

        List<Collider> result = new();
        foreach (var candidate in candidates)
        {
            if (candidate == collider) continue;
            if (IsPenetrating(collider, candidate))
                result.Add(candidate);
        }
        return result.ToArray();
    }

    /// <summary>
    /// Determines whether two colliders are penetrating beyond the threshold.
    /// </summary>
    public bool IsPenetrating(Collider a, Collider b)
    {
        return Physics.ComputePenetration(
            a, a.transform.position, a.transform.rotation,
            b, b.transform.position, b.transform.rotation,
            out _, out float distance) && distance > MaxPenetrationThreshold;
    }

    private bool IsInSameHierarchy(GameObject a, GameObject b)
    {
        return a.transform.root == b.transform.root;
    }

    /// <summary>
    /// Finds a suitable new parent for welding.
    /// </summary>
    private GameObject FindNewOverlappingWeldable(GameObject target)
    {
        Collider collider = target.GetComponent<Collider>();
        if (!collider) return null;

        foreach (Collider overlap in FindPenetratingColliders(collider))
        {
            GameObject other = overlap.gameObject;
            if (!IsInSameHierarchy(target, other) && CanBeWelded(other))
                return other;
        }
        return null;
    }

    /// <summary>
    /// Unwelds direct children and parent if applicable.
    /// </summary>
    private void UnweldImmediateConnections(GameObject target)
    {
        target = GetWeldableAncestor(target);
        if (target == null) return;

        List<GameObject> affected = new();

        foreach (Transform child in target.transform)
        {
            if (IsWeldable(child.gameObject))
            {
                affected.Add(child.gameObject);
                child.SetParent(null, true);
            }
        }

        if (target.transform.parent)
        {
            affected.Add(target.transform.parent.gameObject);
            target.transform.SetParent(null, true);
        }

        affected.Add(target);

        foreach (GameObject obj in affected)
            obj.GetComponent<IWeldable>()?.OnUnweld();
    }

    /// <summary>
    /// Fully unwelds all weldable objects in the hierarchy.
    /// </summary>
    private void UnweldHierarchy(GameObject target)
    {
        Transform root = target.transform.root;
        if (!root) return;

        foreach (Transform t in root.GetComponentsInChildren<Transform>())
        {
            if (IsWeldable(t.gameObject))
                t.SetParent(null, true);
        }
    }

    /// <summary>
    /// Recursively welds overlapping weldable objects.
    /// </summary>
    private void WeldOverlappingObjects(GameObject origin, HashSet<GameObject> welded)
    {
        Queue<GameObject> queue = new();
        HashSet<GameObject> visited = new();

        queue.Enqueue(origin);
        while (queue.Count > 0)
        {
            GameObject current = queue.Dequeue();
            if (!visited.Add(current)) continue;
            if (!IsWeldable(current)) continue;

            foreach (Collider col in current.GetComponentsInChildren<Collider>())
            {
                foreach (Collider overlap in FindPenetratingColliders(col))
                {
                    GameObject other = overlap.gameObject;
                    if (IsInSameHierarchy(origin, other) || !CanBeWelded(other)) continue;
                    if (other.transform.parent == null)
                    {
                        other.transform.SetParent(current.transform, true);
                        queue.Enqueue(other);
                        welded.Add(other);
                    }
                }
            }
        }
        welded.Add(origin);
    }

    private void Unweld(GameObject selected)
    {
        if (selected)
            UnweldImmediateConnections(selected);
    }

    private void Weld(GameObject selected)
    {
        if (!selected) return;

        GameObject root = GetTopmostWeldableAncestor(selected);
        if (!root) return;

        GameObject newParent = FindNewOverlappingWeldable(selected);
        if (!newParent) return;

        UnweldHierarchy(root);
        selected.transform.SetParent(newParent.transform, true);

        HashSet<GameObject> welded = new() { selected };
        if (!IsWelded(newParent)) welded.Add(newParent);

        WeldOverlappingObjects(selected, welded);

        foreach (var obj in welded)
            obj.GetComponent<IWeldable>()?.OnWeld();
    }
}