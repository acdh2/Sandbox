using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeldingService
{
    private float maxAllowedPenetration = -0.01f;

    public WeldingService()
    {
    }

    public IEnumerator Weld(GameObject target)
    {
        if (WeldingUtils.IsWelded(target))
        {
            Debug.LogError("Object is already welded");
            yield break;
        }

        GameObject root = WeldingUtils.GetWeldableRoot(target);
        if (root == null) yield break;

        HashSet<GameObject> weldGroup = FindAllConnectedWeldables(root);

        foreach (GameObject weldable in weldGroup)
        {
            WeldingUtils.RemoveRigidbody(weldable);
            weldable.transform.SetParent(root.transform, true);
        }

        WeldingUtils.RemoveRigidbody(root);
        yield return null;

        if (!root.TryGetComponent<Rigidbody>(out _))
        {
            root.AddComponent<Rigidbody>();
        }

        foreach (GameObject weldable in weldGroup)
        {
            OnWeldEvent(weldable);
        }
        OnWeldEvent(root);
    }

    public void Unweld(GameObject target)
    {
        if (!WeldingUtils.IsWelded(target))
        {
            Debug.LogError("Object is already unwelded");
            return;
        }

        GameObject root = WeldingUtils.GetWeldableRoot(target);
        if (root == null) return;

        HashSet<GameObject> connected = WeldingUtils.FindConnectedWeldables(root);
        connected.Add(root);

        foreach (GameObject obj in connected)
        {
            obj.transform.SetParent(null, true);
            WeldingUtils.RemoveRigidbody(obj);
        }

        foreach (GameObject obj in connected)
        {
            OnUnweldEvent(obj);
        }
    }

    private HashSet<GameObject> FindAllConnectedWeldables(GameObject startRoot)
    {
        var visited = new HashSet<GameObject>();
        var toVisit = new Queue<GameObject>();

        toVisit.Enqueue(startRoot);
        visited.Add(startRoot);

        while (toVisit.Count > 0)
        {
            GameObject current = toVisit.Dequeue();
            Collider[] currentColliders = current.GetComponentsInChildren<Collider>();

            foreach (var col in currentColliders)
            {
                Collider[] overlaps = WeldingUtils.FindOverlappingCollidersBySingleCollider(col, maxAllowedPenetration);

                foreach (var otherCol in overlaps)
                {
                    GameObject otherRoot = WeldingUtils.GetWeldableRoot(otherCol.gameObject);
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
    
    private void OnWeldEvent(GameObject obj)
    {        
        obj.GetComponent<IWeldable>()?.OnWeld();
    }

    private void OnUnweldEvent(GameObject obj)
    {
        obj.GetComponent<IWeldable>()?.OnUnweld();
    }

}
