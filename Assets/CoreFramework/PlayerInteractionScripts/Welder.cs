using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(SelectionHandler))]
public class Welder : MonoBehaviour
{
    const int MaxWeldsAtTheSameTime = 64;

    private const float MaxPenetrationThreshold = 0.01f;
    private const float WeldProximityThreshold = 0.01f;

    private SelectionHandler selectionHandler;

    private void Start()
    {
        selectionHandler = GetComponent<SelectionHandler>();
    }

    private void Update()
    {
        if (InputSystem.GetButtonDown(InputButton.Weld))
            Weld(selectionHandler.CurrentSelection);

        if (InputSystem.GetButtonDown(InputButton.Unweld))
            Unweld(selectionHandler.CurrentSelection);
    }

    /// <summary>
    /// Checks whether the object is marked as weldable via its tag.
    /// </summary>
    private bool IsWeldable(GameObject obj)
    {
        var weldable = obj.GetComponent<Weldable>();
        return weldable != null && (
            (weldable.mode == WeldMode.AttachableOnly) ||
            (weldable.mode == WeldMode.Both)
        );
    }

    /// <summary>
    /// Checks whether the object is marked as a weld base via its tag.
    /// </summary>
    private bool IsWeldBase(GameObject obj)
    {
        var weldable = obj.GetComponent<Weldable>();
        return weldable != null && (
            (weldable.mode == WeldMode.ReceivableOnly) ||
            (weldable.mode == WeldMode.Both)
        );
    }

    /// <summary>
    /// Determines whether the given object is already welded to something else.
    /// </summary>
    private bool IsWelded(GameObject obj)
    {
        foreach (Transform child in obj.transform)
            if (IsWeldable(child.gameObject)) return true;

        if (obj.transform.parent != null)
            return GetWeldableAncestor(obj.transform.parent.gameObject) != null;

        return false;
    }

    /// <summary>
    /// Determines whether the given object can be welded.
    /// </summary>
    private bool CanBeWelded(GameObject obj)
    {
        Weldable weldable = obj.GetComponent<Weldable>();
        if (weldable == null) return false;

        // Het object moet iets kunnen attachen (actief)
        return weldable.CanAttach;
    }

    /// <summary>
    /// Finds the first weldable ancestor in the hierarchy.
    /// </summary>
    private GameObject GetWeldableAncestor(GameObject obj)
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
    /// Returns whether two objects belong to the same hierarchy root.
    /// </summary>
    private bool IsInSameHierarchy(GameObject a, GameObject b)
    {
        return a.transform.root == b.transform.root;
    }

    /// <summary>
    /// Checks whether two colliders are penetrating beyond a given threshold.
    /// </summary>
    private bool IsPenetrating(Collider a, Collider b)
    {
        if (Physics.ComputePenetration(
            a, a.transform.position, a.transform.rotation,
            b, b.transform.position, b.transform.rotation,
            out _, out float distance))
        {
            float effectiveThreshold = MaxPenetrationThreshold - WeldProximityThreshold;
            return distance >= effectiveThreshold;
        }
        return false;
    }

    /// <summary>
    /// Finds nearby colliders that are penetrating the target collider.
    /// </summary>
    private Collider[] FindPenetratingColliders(Collider collider)
    {
        Bounds bounds = collider.bounds;
        Vector3 margin = new Vector3(WeldProximityThreshold, WeldProximityThreshold, WeldProximityThreshold);
        Collider[] candidates = Physics.OverlapBox(bounds.center, bounds.extents + margin, collider.transform.rotation);

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
    /// Finds another weldable object that overlaps with the target, suitable for parenting.
    /// </summary>
    private Weldable FindNewOverlappingWeldable(GameObject target)
    {
        Collider collider = target.GetComponent<Collider>();
        if (!collider) return null;

        foreach (Collider overlap in FindPenetratingColliders(collider))
        {
            GameObject other = overlap.gameObject;
            if (!IsInSameHierarchy(target, other))
            {
                Weldable weldableOther = other.GetComponent<Weldable>();
                if (weldableOther != null && weldableOther.CanReceive)
                    return weldableOther;
            }
        }

        return null;
    }

    // /// <summary>
    // /// Reparents all weldable ancestors under each other, ending with the selected object.
    // /// </summary>
    // private void ReparentWeldableAncestors(GameObject selected)
    // {
    //     selected = GetWeldableAncestor(selected);
    //     if (selected == null) return;

    //     List<Transform> weldableAncestors = new();
    //     Transform current = selected.transform;

    //     while (current != null)
    //     {
    //         if (IsWeldable(current.gameObject) && !IsWeldBase(current.gameObject))
    //             weldableAncestors.Add(current);

    //         current = current.parent;
    //     }

    //     foreach (Transform transform in weldableAncestors)
    //         transform.SetParent(null, true);

    //     for (int i = weldableAncestors.Count - 1; i > 0; i--)
    //         weldableAncestors[i].SetParent(weldableAncestors[i - 1], true);
    // }

    /// <summary>
    /// Performs welding logic for the selected object.
    /// </summary>
    private void Weld(GameObject selected)
    {
        if (selected == null) return;

        Weldable selectedWeldable = selected.GetComponent<Weldable>();
        if (selectedWeldable == null || !selectedWeldable.CanAttach) return;

        for (int index = 0; index < MaxWeldsAtTheSameTime; index++)
        {
            Weldable newParent = FindNewOverlappingWeldable(selected);
            if (newParent == null) break;
            if (selectedWeldable.transform.parent == null)
            {
                selectedWeldable.Weld(newParent);
            }
            else
            {
                newParent.Weld(selectedWeldable);
            }
        }

        //selectedWeldable.OnWeld();
            // Eventueel OnWeld aanroepen
            // foreach (Weldable weldable in selected.transform.root.GetComponentsInChildren<Weldable>())
            //     weldable.OnWeld();
        }

    /// <summary>
    /// Detaches the selected object from its parent and calls OnUnweld for affected weldables.
    /// </summary>
    private void Unweld(GameObject target)
    {
        target = GetWeldableAncestor(target);
        if (target == null) return;

        Weldable weldable = target.GetComponent<Weldable>();
        weldable?.Unweld();

    }

}
