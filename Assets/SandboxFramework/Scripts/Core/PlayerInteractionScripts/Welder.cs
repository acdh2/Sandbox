using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Lets the player weld (glue) the selected object to everything it overlaps with,
/// or unweld it again, using the Weld and Unweld buttons.
/// </summary>
[RequireComponent(typeof(SelectionHandler))]
[DisallowMultipleComponent]
public class Welder : MonoBehaviour
{
    private const int MaxWeldsAtTheSameTime = 640;
    private const float MaxPenetrationThreshold = 0.01f;

    [Tooltip("Extra margin around the collider used to check for overlaps, so neighbouring blocks can still connect.")]
    [FormerlySerializedAs("WeldProximityThreshold")]
    public float weldProximityThreshold = 0.01f;

    public WeldType weldingType = WeldType.HierarchyBased;

    private SelectionHandler selectionHandler;

    private void Start()
    {
        selectionHandler = GetComponent<SelectionHandler>();
    }

    private void Update()
    {
        // When the weld button is pressed, weld the currently selected object.
        if (SandboxInput.GetButtonDown(InputButton.Weld))
        {
            GameObject selected = selectionHandler.CurrentSelection;
            selectionHandler.ClearSelection();
            Weld(selected);
        }

        // When the unweld button is pressed, unweld the currently selected object.
        if (SandboxInput.GetButtonDown(InputButton.Unweld))
        {
            GameObject selected = selectionHandler.CurrentSelection;
            selectionHandler.ClearSelection();
            Unweld(selected);
        }
    }

    /// <summary>
    /// Checks if the given object has an enabled Weldable component.
    /// </summary>
    private bool IsWeldable(GameObject obj)
    {
        var weldable = obj.GetComponent<Weldable>();
        return weldable != null && weldable.enabled;
    }

    /// <summary>
    /// Finds the closest ancestor of the object that has an enabled Weldable component.
    /// Returns null if none found.
    /// </summary>
    private GameObject GetWeldableAncestor(GameObject obj)
    {
        if (obj == null) return null;
        Transform current = obj.transform;
        while (current != null)
        {
            if (IsWeldable(current.gameObject))
                return current.gameObject;
            current = current.parent;
        }
        return null;
    }

    /// <summary>
    /// Checks whether two objects belong to the same root hierarchy.
    /// </summary>
    private bool IsInSameHierarchy(GameObject a, GameObject b)
    {
        return a.transform.root == b.transform.root;
    }

    /// <summary>
    /// Checks if two colliders overlap deeply enough to be welded together.
    /// Physics.ComputePenetration only returns true when the colliders overlap;
    /// 'distance' is then how far they overlap (always positive).
    /// </summary>
    private bool IsPenetrating(Collider a, Collider b)
    {
        if (Physics.ComputePenetration(
            a, a.transform.position, a.transform.rotation,
            b, b.transform.position, b.transform.rotation,
            out _, out float distance))
        {
            float effectiveThreshold = MaxPenetrationThreshold - weldProximityThreshold;
            return distance >= effectiveThreshold;
        }
        return false;
    }

    /// <summary>
    /// Finds colliders overlapping the given collider and returns those penetrating it.
    /// </summary>
    private Collider[] FindPenetratingColliders(Collider collider)
    {
        Bounds bounds = collider.bounds;
        Vector3 margin = Vector3.one * weldProximityThreshold;

        // Get all colliders overlapping the collider's bounds plus margin
        Collider[] candidates = Physics.OverlapBox(bounds.center, bounds.extents + margin, Quaternion.identity);
        var penetrating = new List<Collider>();

        foreach (var candidate in candidates)
        {
            if (candidate == collider) continue;
            if (IsPenetrating(collider, candidate))
                penetrating.Add(candidate);
        }

        return penetrating.ToArray();
    }

    /// <summary>
    /// Searches for a weldable object overlapping the target that can be welded to it.
    /// Outputs the overlapping transform found.
    /// </summary>
    private Weldable FindNewOverlappingWeldable(GameObject target, out Transform overlappingTransform)
    {
        overlappingTransform = null;

        Collider collider = target.GetComponent<Collider>();
        if (collider == null) return null;

        Weldable targetWeldable = target.GetComponent<Weldable>();
        if (targetWeldable == null || !targetWeldable.enabled) return null;

        foreach (Collider overlap in FindPenetratingColliders(collider))
        {
            GameObject other = overlap.gameObject;
            if (!IsInSameHierarchy(target, other))
            {
                Weldable weldableOther = other.GetComponentInParent<Weldable>();
                if (weldableOther != null && weldableOther.enabled)
                {
                    if (!weldableOther.IsConnected(targetWeldable))
                    {
                        overlappingTransform = other.transform;
                        return weldableOther;
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Performs welding of the selected object to overlapping weldables.
    /// Processes up to MaxWeldsAtTheSameTime welds per call.
    /// </summary>
    public void Weld(GameObject selected)
    {
        if (selected == null) return;

        Weldable rootWeldable = selected.GetComponent<Weldable>();
        if (rootWeldable == null || !rootWeldable.enabled) return;

        var weldQueue = new Queue<Weldable>();
        var visited = new HashSet<Weldable>();

        weldQueue.Enqueue(rootWeldable);
        visited.Add(rootWeldable);

        int weldCount = 0;

        while (weldQueue.Count > 0 && weldCount < MaxWeldsAtTheSameTime)
        {
            Weldable current = weldQueue.Dequeue();

            // Find a new weldable overlapping object
            Weldable overlappingWeldable = FindNewOverlappingWeldable(current.gameObject, out Transform overlapTransform);
            if (overlappingWeldable != null && !visited.Contains(overlappingWeldable))
            {
                weldQueue.Enqueue(current);
                current.WeldTo(overlappingWeldable, weldingType, false, overlapTransform);
                weldQueue.Enqueue(overlappingWeldable);
                visited.Add(overlappingWeldable);
                weldCount++;
            }
        }

        if (weldingType == WeldType.HierarchyBased)
        {
            selectionHandler.HighlightObject(rootWeldable.transform.root.gameObject, true);
        }
    }

    /// <summary>
    /// Detaches the weldable object from its parent weld base.
    /// </summary>
    private void Unweld(GameObject target)
    {
        target = GetWeldableAncestor(target);
        if (target == null) return;

        Weldable weldable = target.GetComponent<Weldable>();
        if (weldable != null && weldable.enabled)
        {
            weldable.Unweld();
        }
    }
}
