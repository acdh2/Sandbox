using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(SelectionHandler))]
public class Welder : MonoBehaviour
{
    const float maxAllowedPenetration = 0.01f;

    public string weldableTag;

    private SelectionHandler selectionHandler;

    private void Start()
    {
        selectionHandler = GetComponent<SelectionHandler>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            Weld(selectionHandler.CurrentSelection);
        }
        if (Input.GetKeyDown(KeyCode.U))
        {
            Unweld(selectionHandler.CurrentSelection);
        }
    }

    public bool IsWelded(GameObject obj)
    {
        return obj.transform.parent != null;
    }

    public bool CanBeWelded(GameObject obj)
    {
        GameObject weldable = GetWeldableAncestor(obj);
        return (weldable != null);
    }

    public bool IsWeldable(GameObject obj)
    {
        return obj.CompareTag(weldableTag);
    }

    public GameObject GetWeldableAncestor(GameObject obj)
    {
        Transform current = obj.transform;
        while (current != null)
        {
            if (IsWeldable(current.gameObject))
                return current.gameObject;
            current = current.parent;
        }
        return null;
    }

    public GameObject GetTopmostWeldableAncestor(GameObject obj)
    {
        GameObject topmostWeldable = null;

        Transform current = obj.transform;
        while (current != null)
        {
            if (IsWeldable(current.gameObject))
                topmostWeldable = current.gameObject;
            current = current.parent;
        }
        return topmostWeldable;
    }

    public Collider[] FindOverlappingCollidersBySingleCollider(Collider collider, float maxAllowedPenetration)
    {
        Bounds bounds = collider.bounds;
        Collider[] candidates = Physics.OverlapBox(bounds.center, bounds.extents, collider.transform.rotation);

        var result = new List<Collider>();

        foreach (var candidate in candidates)
        {
            if (candidate == collider) continue;

            if (IsPenetrating(new Collider[] { collider }, candidate, maxAllowedPenetration))
            {
                result.Add(candidate);
            }
        }

        return result.ToArray();
    }

    public bool IsPenetrating(Collider[] selfColliders, Collider other, float maxAllowedPenetration)
    {
        foreach (var own in selfColliders)
        {
            if (Physics.ComputePenetration(
                own, own.transform.position, own.transform.rotation,
                other, other.transform.position, other.transform.rotation,
                out _, out float distance))
            {
                if (distance > maxAllowedPenetration)
                    return true;
            }
        }
        return false;
    }


    private bool IsInSameHierarchy(GameObject a, GameObject b)
    {
        return a.transform.root == b.transform.root;
    }

    GameObject FindNewOverlappingWeldable(GameObject target)
    {
        Collider collider = target.GetComponent<Collider>();
        if (collider)
        {
            foreach (Collider overlap in FindOverlappingCollidersBySingleCollider(collider, maxAllowedPenetration))
            {
                GameObject other = overlap.gameObject;
                if (!IsInSameHierarchy(target, other) && CanBeWelded(other))
                {
                    return other;
                }
            }
        }

        return null;
    }

    void UnweldImmediateChildren(GameObject target)
    {
        Transform root = target.transform.root;
        if (root == null) return;

        target = GetWeldableAncestor(target);
        if (target == null) return;

        foreach (Transform child in target.transform)
        {
            if (IsWeldable(child.gameObject))
            {
                child.SetParent(null, true);
            }
        }

        target.transform.SetParent(null, true);
    }

    void RecursivelyUnweldHierarchy(GameObject target)
    {
        Transform root = target.transform.root;
        if (root == null) return;

        foreach (Transform t in root.GetComponentsInChildren<Transform>())
        {
            if (IsWeldable(t.gameObject))
            {
                t.SetParent(null, true);
            }
        }
    }

    void RecursivelyWeldOverlaps(GameObject target)
    {
        if (target == null) return;

        List<GameObject> visited = new List<GameObject>();
        Queue<GameObject> toVisit = new Queue<GameObject>();

        toVisit.Enqueue(target);
        while (toVisit.Count > 0)
        {
            GameObject candidate = toVisit.Dequeue();

            if (visited.Contains(candidate)) continue;
            visited.Add(candidate);

            if (!IsWeldable(candidate)) continue;

            foreach (Collider collider in candidate.GetComponentsInChildren<Collider>())
            {
                foreach (Collider overlap in FindOverlappingCollidersBySingleCollider(collider, maxAllowedPenetration))
                {
                    if (IsInSameHierarchy(overlap.gameObject, target)) continue;

                    Transform overlappingTransform = overlap.gameObject.transform;
                    if (CanBeWelded(overlappingTransform.gameObject))
                    {
                        if (overlappingTransform.parent == null)
                        {
                            overlappingTransform.SetParent(candidate.transform);
                            toVisit.Enqueue(overlap.gameObject);
                        }
                    }
                }
            }
        }

    }

    void Unweld(GameObject selected)
    {
        if (selected == null) return;

        GameObject selectedItem = GetWeldableAncestor(selected);
        UnweldImmediateChildren(selectedItem);
    }

    void Weld(GameObject selected)
    {
        if (selected == null) return;

        GameObject selectedRoot = GetTopmostWeldableAncestor(selected);
        if (selectedRoot == null) return;

        GameObject newParent = FindNewOverlappingWeldable(selected);
        if (newParent == null) return;

        RecursivelyUnweldHierarchy(selectedRoot);

        selected.transform.SetParent(newParent.transform, true);

        RecursivelyWeldOverlaps(selected);
    }


}
