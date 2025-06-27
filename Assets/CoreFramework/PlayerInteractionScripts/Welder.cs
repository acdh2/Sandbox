using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(SelectionHandler))]
public class Welder : MonoBehaviour
{
    private const float MaxPenetrationThreshold = 0.01f;
    private const float WeldProximityThreshold = 0.01f;

    public string weldableTag;
    public string weldBaseTag;

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

    public bool IsWeldBase(GameObject obj) => obj.CompareTag(weldBaseTag);

    /// <summary>
    /// Finds the nearest weldable ancestor in the hierarchy.
    /// </summary>
    public GameObject GetWeldableAncestor(GameObject obj)
    {
        if (obj == null) return null;
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
        Vector3 fudge = new Vector3(WeldProximityThreshold, WeldProximityThreshold, WeldProximityThreshold);
        Collider[] candidates = Physics.OverlapBox(bounds.center, bounds.extents + fudge, collider.transform.rotation);

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
        if (Physics.ComputePenetration(
            a, a.transform.position, a.transform.rotation,
            b, b.transform.position, b.transform.rotation,
            out _, out float distance))
        {
            float effectiveThreshold = MaxPenetrationThreshold - WeldProximityThreshold;
            return distance >= effectiveThreshold;
        }
        else
        {
            return false;
        }
    }

    public bool IsPenetratingOld(Collider a, Collider b)
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
    private void UnweldImmediateConnections2(GameObject target)
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
    private void UnweldHierarchy2(GameObject target)
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
    private void WeldOverlappingObjects2(GameObject origin)
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
                    }
                }
            }
        }
    }

    private void UnweldFromParent(GameObject target)
    {
        target = GetWeldableAncestor(target);
        if (target == null) return;

        foreach (IWeldable weldable in target.transform.root.GetComponentsInChildren<IWeldable>())
        {
            weldable.OnUnweld();
        }

        if (target.transform.parent)
        {
            target.transform.SetParent(null, true);
        }

        foreach (IWeldable weldable in target.transform.root.GetComponentsInChildren<IWeldable>())
        {
            weldable.OnUnweld();
        }

    }

    private void Unweld(GameObject selected)
    {
        UnweldFromParent(selected);
    }



    /// <summary>
    /// Maakt het geselecteerde weldable object de nieuwe root door alle weldable ancestors als children te herstructureren.
    /// Niet-weldable ancestors worden genegeerd en blijven erbuiten.
    /// </summary>
    private void ReparentWeldableAncestors(GameObject selected)
    {
        selected = GetWeldableAncestor(selected);
        if (selected == null) return;

        List<Transform> weldableAncestors = new();
        Transform current = selected.transform;

        while (current != null)
        {
            if (IsWeldable(current.gameObject))
            {
                weldableAncestors.Add(current);
            }
            current = current.parent;
        }

        foreach (Transform transform in weldableAncestors)
        {
            transform.SetParent(null, true);
        }

        for (int i = weldableAncestors.Count - 1; i > 0; i--)
        {
            weldableAncestors[i].SetParent(weldableAncestors[i - 1], true);
        }

    }

    private void Weld(GameObject selected)
    {
        if (!CanBeWelded(selected)) return;

        GameObject newParent = FindNewOverlappingWeldable(selected);
        if (newParent == null) return;

        ReparentWeldableAncestors(selected);

        selected.transform.SetParent(newParent.transform, true);

        foreach (IWeldable weldable in selected.transform.root.GetComponentsInChildren<IWeldable>())
        {
            weldable.OnWeld();
        }
    }

    // private void WeldOld2(GameObject selected)
    // {
    //     if (!selected) return;

    //     GameObject root = GetTopmostWeldableAncestor(selected);
    //     if (!root) return;

    //     GameObject newParent = FindNewOverlappingWeldable(selected);
    //     if (newParent == null) return;

    //     // Zorg dat de weldable-hiërarchie correct is voordat we gaan hechten
    //     ReparentWeldableAncestors(selected);

    //     // Nieuwe weld
    //     selected.transform.SetParent(newParent.transform, true);
        
    //     foreach (IWeldable weldable in selected.transform.root.GetComponentsInChildren<IWeldable>())
    //     {
    //         weldable.OnWeld();
    //     }
    // }


    // private void WeldOld(GameObject selected)
    // {
    //     if (!selected) return;

    //     GameObject root = GetTopmostWeldableAncestor(selected);
    //     if (!root) return;

    //     GameObject newParent = FindNewOverlappingWeldable(selected);
    //     if (newParent == null) return;

    //     //alternative 1:
    //     //UnweldHierarchy(root);

    //     //alternative 2:
    //     Transform oldParent = selected.transform.parent;

    //     //both
    //     selected.transform.SetParent(newParent.transform, true);

    //     //alternative 2:
    //     if (oldParent != null)
    //     {
    //         oldParent.SetParent(selected.transform, true);
    //     }

    //     //alternative 1:
    //     //WeldOverlappingObjects(selected);

    //     foreach (IWeldable weldable in selected.transform.root.GetComponentsInChildren<IWeldable>())
    //     {
    //         weldable.OnWeld();
    //     }
    // }
}