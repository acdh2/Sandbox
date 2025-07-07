using UnityEngine;
using System.Collections;
using System.Collections.Generic;

enum WeldingType
{
    HierarchyBased,
    PhysicsBased
}

[RequireComponent(typeof(SelectionHandler))]
public class Welder : MonoBehaviour
{
    private const int MaxWeldsAtTheSameTime = 64;
    private const float MaxPenetrationThreshold = 0.01f;
    private const float WeldProximityThreshold = 0.01f;


    public WeldType weldingType = WeldType.HierarchyBased;

    private SelectionHandler selectionHandler;

    private void Start()
    {
        selectionHandler = GetComponent<SelectionHandler>();
    }

    private void Update()
    {
        // Trigger weld on current selection when weld button is pressed
        if (InputSystem.GetButtonDown(InputButton.Weld))
        {
            GameObject selected = selectionHandler.CurrentSelection;
            selectionHandler.ClearSelection();
            Weld(selected);
        }

        // Trigger unweld on current selection when unweld button is pressed
        if (InputSystem.GetButtonDown(InputButton.Unweld))
        {
            GameObject selected = selectionHandler.CurrentSelection;
            selectionHandler.ClearSelection();
            Unweld(selected);
        }
    }

    /// <summary>
    /// Checks if the object is weldable in a mode that allows it to attach to others.
    /// </summary>
    private bool IsWeldable(GameObject obj)
    {
        var weldable = obj.GetComponent<Weldable>();
        return weldable != null &&
               (weldable.mode == WeldMode.AttachableOnly || weldable.mode == WeldMode.Both);
    }

    /// <summary>
    /// Checks if the object can act as a weld base (receiver).
    /// </summary>
    private bool IsWeldBase(GameObject obj)
    {
        var weldable = obj.GetComponent<Weldable>();
        return weldable != null &&
               (weldable.mode == WeldMode.ReceivableOnly || weldable.mode == WeldMode.Both);
    }

    /// <summary>
    /// Returns true if the object has weldable children or is parented under a weldable ancestor.
    /// </summary>
    private bool IsWelded(GameObject obj)
    {
        // Check if any child is weldable
        foreach (Transform child in obj.transform)
            if (IsWeldable(child.gameObject))
                return true;

        // Check if any ancestor is weldable
        if (obj.transform.parent != null)
            return GetWeldableAncestor(obj.transform.parent.gameObject) != null;

        return false;
    }

    /// <summary>
    /// Checks if the object can currently attach to others (active weldable).
    /// </summary>
    private bool CanBeWelded(GameObject obj)
    {
        var weldable = obj.GetComponent<Weldable>();
        return weldable != null && weldable.CanAttach;
    }

    /// <summary>
    /// Traverses upward in the hierarchy to find the closest weldable ancestor.
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
    /// Determines if two GameObjects share the same root ancestor (are in the same hierarchy).
    /// </summary>
    private bool IsInSameHierarchy(GameObject a, GameObject b)
    {
        return a.transform.root == b.transform.root;
    }

    /// <summary>
    /// Checks if two colliders are penetrating each other beyond a configured threshold.
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
    /// Finds colliders overlapping the given collider that are penetrating it.
    /// </summary>
    private Collider[] FindPenetratingColliders(Collider collider)
    {
        Bounds bounds = collider.bounds;
        Vector3 margin = new Vector3(WeldProximityThreshold, WeldProximityThreshold, WeldProximityThreshold);

        Collider[] candidates = Physics.OverlapBox(bounds.center, bounds.extents + margin, collider.transform.rotation);
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
    /// Finds a weldable object overlapping the target that can receive a weld connection.
    /// </summary>
    private Weldable FindNewOverlappingWeldable(GameObject target)
    {
        Collider collider = target.GetComponent<Collider>();
        if (collider == null) return null;

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

    /// <summary>
    /// Attempts to weld the selected object to overlapping weldable objects.
    /// Performs up to MaxWeldsAtTheSameTime weld operations per call.
    /// </summary>
    public void Weld(GameObject selected)
    {
        if (selected == null) return;

        Weldable selectedWeldable = selected.GetComponent<Weldable>();
        if (selectedWeldable == null || !selectedWeldable.CanAttach) return;

        for (int i = 0; i < MaxWeldsAtTheSameTime; i++)
        {
            Weldable newParent = FindNewOverlappingWeldable(selected);
            if (newParent == null) break;

            if (selectedWeldable.transform.parent == null)
            {
                selectedWeldable.WeldTo(newParent, weldingType);
            }
            else
            {
                newParent.WeldTo(selectedWeldable, weldingType);
            }
        }
    }

    /// <summary>
    /// Detaches the selected weldable object from its parent weld base.
    /// </summary>
    private void Unweld(GameObject target)
    {
        target = GetWeldableAncestor(target);
        if (target == null) return;

        Weldable weldable = target.GetComponent<Weldable>();
        weldable?.Unweld();
    }
}
