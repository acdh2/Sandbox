using UnityEngine;

using System.Collections.Generic;

public static class WeldingUtils
{
    public static bool IsWelded(GameObject obj)
    {
        Transform current = obj.transform;
        while (current != null)
        {
            if (current.GetComponent<Rigidbody>() != null)
                return true;
            current = current.parent;
        }
        return false;
    }

    public static bool IsWeldable(GameObject obj)
    {
        return obj.CompareTag(Tags.Draggable) || obj.GetComponent<IWeldable>() != null;
    }

    public static GameObject GetWeldableRoot(GameObject obj)
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

    public static Collider[] FindOverlappingCollidersBySingleCollider(Collider collider, float maxAllowedPenetration)
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

    public static bool IsPenetrating(Collider[] selfColliders, Collider other, float maxAllowedPenetration)
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

    public static HashSet<GameObject> FindConnectedWeldables(GameObject root)
    {
        var weldables = new HashSet<GameObject>();
        var stack = new Stack<Transform>();
        stack.Push(root.transform);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            GameObject obj = current.gameObject;

            if (IsWeldable(obj))
                weldables.Add(obj);

            foreach (Transform child in current)
            {
                stack.Push(child);
            }
        }

        return weldables;
    }

    public static HashSet<GameObject> FindAllConnectedWeldables(GameObject startRoot, float maxAllowedPenetration)
    {
        var visited = new HashSet<GameObject>();
        var toVisit = new Queue<GameObject>();

        toVisit.Enqueue(startRoot);
        visited.Add(startRoot);

        while (toVisit.Count > 0)
        {
            GameObject current = toVisit.Dequeue();

            Collider[] currentColliders = current.GetComponentsInChildren<Collider>();
            if (currentColliders.Length == 0) continue;

            foreach (var col in currentColliders)
            {
                Collider[] overlappingColliders = FindOverlappingCollidersBySingleCollider(col, maxAllowedPenetration);

                foreach (Collider overlapCol in overlappingColliders)
                {
                    GameObject otherRoot = GetWeldableRoot(overlapCol.gameObject);

                    if (otherRoot != null && !visited.Contains(otherRoot))
                    {
                        visited.Add(otherRoot);
                        toVisit.Enqueue(otherRoot);
                    }
                }
            }
        }

        return visited;
    }

    /// <summary>
    /// Removes the Rigidbody component if present.
    /// </summary>
    public static void RemoveRigidbody(GameObject obj)
    {
        var rb = obj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Rigidbody.Destroy(rb);
        }
    }

}
