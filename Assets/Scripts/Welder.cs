using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(SelectionHandler))]
public class Welder : MonoBehaviour
{
    [Header("Instellingen")]
    public LayerMask weldableLayers;
    private float maxAllowedPenetration = -0.01f;

    private SelectionHandler selectionHandler;

    private void Start()
    {
        selectionHandler = GetComponent<SelectionHandler>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
            WeldCurrentSelection();

        if (Input.GetKeyDown(KeyCode.Z))
            UnweldCurrentSelection();
    }

    private void WeldCurrentSelection()
    {
        GameObject selected = selectionHandler.CurrentSelection;
        if (selected == null)
        {
            Debug.LogWarning("Geen object geselecteerd.");
            return;
        }

        var connectedObjects = FindConnectedObjects(selected);
        Debug.Log($"Overlappende objecten gevonden: {connectedObjects.Count}");

        RemoveRigidbodies(selected, connectedObjects);
        AttachAsChildren(selected, connectedObjects);

        StartCoroutine(AddRigidbodyNextFrame(selected));
    }

    private void UnweldCurrentSelection()
    {
        GameObject selected = selectionHandler.CurrentSelection;
        if (selected == null)
        {
            Debug.LogWarning("Geen object geselecteerd.");
            return;
        }

        RemoveRigidbody(selected);
    }

    private void RemoveRigidbodies(GameObject root, HashSet<GameObject> objects)
    {
        RemoveRigidbody(root);
        foreach (var obj in objects)
            RemoveRigidbody(obj);
    }

    private void RemoveRigidbody(GameObject obj)
    {
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Destroy(rb);
            Debug.Log($"Rigidbody verwijderd van {obj.name}");
        }
    }

    private void AttachAsChildren(GameObject root, HashSet<GameObject> objects)
    {
        foreach (var obj in objects)
        {
            if ((weldableLayers.value & (1 << obj.layer)) == 0)
                continue;

            // if (obj.transform.parent == root.transform)
            //     continue;

            obj.transform.SetParent(root.transform, worldPositionStays: true);
            Debug.Log($"→ {obj.name} als child van {root.name}");
        }
    }

    private IEnumerator AddRigidbodyNextFrame(GameObject obj)
    {
        yield return null;
        AddRigidbodyIfMissing(obj);
    }

    private void AddRigidbodyIfMissing(GameObject obj)
    {
        if (obj.GetComponent<Rigidbody>() == null)
        {
            obj.AddComponent<Rigidbody>();
            Debug.Log($"Rigidbody toegevoegd aan {obj.name}");
        }
    }

    private HashSet<GameObject> FindConnectedObjects(GameObject start)
    {
        var visited = new HashSet<GameObject>();
        var toVisit = new Queue<GameObject>();
        toVisit.Enqueue(start);

        while (toVisit.Count > 0)
        {
            GameObject current = toVisit.Dequeue();
            if (!visited.Add(current))
                continue;

            foreach (var col in FindOverlappingColliders(current))
            {
                GameObject obj = col.gameObject;
                if (!visited.Contains(obj))
                    toVisit.Enqueue(obj);
            }
        }

        visited.Remove(start); // root niet meenemen als child
        return visited;
    }

    private Collider[] FindOverlappingColliders(GameObject obj)
    {
        var colliders = obj.GetComponentsInChildren<Collider>();
        if (colliders.Length == 0)
            return new Collider[0];

        Bounds bounds = GetCombinedBounds(colliders);
        var candidates = Physics.OverlapBox(bounds.center, bounds.extents, obj.transform.rotation);

        var overlapping = new List<Collider>();
        foreach (var candidate in candidates)
        {
            if (IsPartOfObject(candidate, colliders))
                continue;

            if (!IsInWeldableLayer(candidate.gameObject))
                continue;

            if (CheckPenetration(colliders, candidate))
                overlapping.Add(candidate);
        }

        return overlapping.ToArray();
    }

    private Bounds GetCombinedBounds(Collider[] colliders)
    {
        Bounds bounds = new Bounds(colliders[0].bounds.center, Vector3.zero);
        foreach (var col in colliders)
            bounds.Encapsulate(col.bounds);
        return bounds;
    }

    private bool IsPartOfObject(Collider candidate, Collider[] ownColliders)
    {
        foreach (var ownCol in ownColliders)
            if (candidate == ownCol)
                return true;
        return false;
    }

    private bool IsInWeldableLayer(GameObject obj)
    {
        return (weldableLayers.value & (1 << obj.layer)) != 0;
    }

    private bool CheckPenetration(Collider[] colliders, Collider candidate)
    {
        foreach (var ownCol in colliders)
        {
            if (Physics.ComputePenetration(
                ownCol, ownCol.transform.position, ownCol.transform.rotation,
                candidate, candidate.transform.position, candidate.transform.rotation,
                out _, out float distance))
            {
                if (distance > maxAllowedPenetration)
                    return true;
            }
        }
        return false;
    }
}
