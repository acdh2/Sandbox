using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Handles welding and unwelding of interactable objects based on collider overlap.
/// Press 'F' to weld/unweld the currently selected object.
/// </summary>
[RequireComponent(typeof(SelectionHandler))]
public class Welder : MonoBehaviour
{
    [Header("Weld Settings")]
    [SerializeField] private float maxAllowedPenetration = -0.01f;

    private SelectionHandler selectionHandler;

    private void Start()
    {
        // Cache reference to selection handler
        selectionHandler = GetComponent<SelectionHandler>();
    }

    private void Update()
    {
        // Press 'F' to toggle weld on the selected object
        if (Input.GetKeyDown(KeyCode.F))
        {
            TryToggleWeld();
        }
    }

    /// <summary>
    /// Attempts to weld or unweld the currently selected object.
    /// </summary>
    private void TryToggleWeld()
    {
        GameObject selected = selectionHandler.CurrentSelection;
        if (selected == null) return;

        // If it has a Rigidbody, it's already welded → unweld it
        if (selected.TryGetComponent(out Rigidbody _))
            Unweld(selected);
        else
            Weld(selected);
    }

    /// <summary>
    /// Welds the root and all overlapping weldable objects.
    /// </summary>
    private void Weld(GameObject root)
    {
        selectionHandler.ClearSelection();

        var affectedObjects = new HashSet<GameObject>();
        var overlappingObjects = FindConnectedObjects(root);

        foreach (var obj in overlappingObjects)
        {
            if (!IsWeldable(obj)) continue;

            RemoveRigidbody(obj);
            obj.transform.SetParent(root.transform, true);
            affectedObjects.Add(obj);
        }

        RemoveRigidbody(root);
        StartCoroutine(AddRigidbodyNextFrame(root));
        affectedObjects.Add(root);

        // Notify all welded objects
        foreach (var obj in affectedObjects)
        {
            InvokeWeldEvent(obj);
        }
    }

    /// <summary>
    /// Unwelds the given root and all its welded children recursively.
    /// </summary>
    private void Unweld(GameObject root)
    {
        selectionHandler.ClearSelection();

        var affectedObjects = new HashSet<GameObject>();
        DetachWeldedChildrenRecursive(root, affectedObjects);

        RemoveRigidbody(root);
        affectedObjects.Add(root);

        // Notify all unwelded objects
        foreach (var obj in affectedObjects)
        {
            InvokeUnweldEvent(obj);
        }
    }

    /// <summary>
    /// Recursively detaches all welded children from the given parent.
    /// </summary>
    private void DetachWeldedChildrenRecursive(GameObject parent, HashSet<GameObject> affected)
    {
        List<Transform> children = new();
        foreach (Transform child in parent.transform)
            children.Add(child); // Store list to avoid modifying while iterating

        foreach (Transform child in children)
        {
            GameObject childObj = child.gameObject;

            if (IsWeldable(childObj))
            {
                child.SetParent(null, true);
                RemoveRigidbody(childObj);
                affected.Add(childObj);
            }

            DetachWeldedChildrenRecursive(childObj, affected);
        }
    }

    /// <summary>
    /// Removes the Rigidbody component if present.
    /// </summary>
    private void RemoveRigidbody(GameObject obj)
    {
        var rb = obj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Destroy(rb);
        }
    }

    /// <summary>
    /// Adds a Rigidbody one frame later. Required because welding may change hierarchy first.
    /// </summary>
    private IEnumerator AddRigidbodyNextFrame(GameObject obj)
    {
        yield return null;
        if (!obj.TryGetComponent(out Rigidbody _))
        {
            obj.AddComponent<Rigidbody>();
        }
    }

    /// <summary>
    /// Performs BFS to find all overlapping weldable objects starting from root.
    /// </summary>
    private HashSet<GameObject> FindConnectedObjects(GameObject root)
    {
        var visited = new HashSet<GameObject>();
        var queue = new Queue<GameObject>();
        queue.Enqueue(root);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!visited.Add(current)) continue;

            foreach (var col in FindOverlappingColliders(current))
            {
                var obj = col.gameObject;
                if (!visited.Contains(obj))
                    queue.Enqueue(obj);
            }
        }

        visited.Remove(root); // Exclude self
        return visited;
    }

    /// <summary>
    /// Finds all colliders penetrating the colliders of the given object.
    /// </summary>
    private Collider[] FindOverlappingColliders(GameObject obj)
    {
        var ownColliders = obj.GetComponentsInChildren<Collider>();
        if (ownColliders.Length == 0) return new Collider[0];

        Bounds bounds = GetCombinedBounds(ownColliders);
        var candidates = Physics.OverlapBox(bounds.center, bounds.extents, obj.transform.rotation);

        var result = new List<Collider>();
        foreach (var candidate in candidates)
        {
            if (IsOwnCollider(candidate, ownColliders)) continue;
            if (!IsWeldable(candidate.gameObject)) continue;

            if (IsPenetrating(ownColliders, candidate))
                result.Add(candidate);
        }

        return result.ToArray();
    }

    /// <summary>
    /// Calculates the combined bounds of multiple colliders.
    /// </summary>
    private Bounds GetCombinedBounds(Collider[] colliders)
    {
        Bounds bounds = new(colliders[0].bounds.center, Vector3.zero);
        foreach (var col in colliders)
            bounds.Encapsulate(col.bounds);
        return bounds;
    }

    /// <summary>
    /// Returns true if the collider is one of the given colliders.
    /// </summary>
    private bool IsOwnCollider(Collider candidate, Collider[] self)
    {
        foreach (var col in self)
            if (col == candidate) return true;
        return false;
    }

    /// <summary>
    /// Returns true if the object is weldable (has proper tag or interface).
    /// </summary>
    private bool IsWeldable(GameObject obj)
    {
        return obj.CompareTag(Tags.Draggable) || obj.GetComponent<IWeldable>() != null;
    }

    /// <summary>
    /// Checks if the given colliders are physically penetrating the candidate collider.
    /// </summary>
    private bool IsPenetrating(Collider[] selfColliders, Collider other)
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

    /// <summary>
    /// Calls the weld event on the object if it implements IWeldable.
    /// </summary>
    private void InvokeWeldEvent(GameObject obj)
    {
        obj.GetComponent<IWeldable>()?.InvokeWeld();
    }

    /// <summary>
    /// Calls the unweld event on the object if it implements IWeldable.
    /// </summary>
    private void InvokeUnweldEvent(GameObject obj)
    {
        obj.GetComponent<IWeldable>()?.InvokeUnweld();
    }
}
