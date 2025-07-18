using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages object selection via raycasting on objects with a Selectable component.
/// Temporarily changes selected objects' layers for highlighting,
/// and restores original layers upon deselection.
/// </summary>
[DisallowMultipleComponent]
public class SelectionHandler : MonoBehaviour
{
    [Header("Selection Settings")]
    [Tooltip("Layer used for visual feedback on selected objects.")]
    public string selectionLayerName = "Selection";

    public float raycastDistance = 8f;

    public GameObject currentSelection { get; private set; }

    private Camera cam;
    private bool selectionLocked = false;
    private int selectionLayer;
    private readonly Dictionary<GameObject, int> originalLayers = new();

    private float selectionLockedTimer = 0f;

    private void Start()
    {
        cam = Camera.main;
        selectionLayer = LayerMask.NameToLayer(selectionLayerName);
    }

    private void Update()
    {
        if (selectionLocked)
            return;

        if (selectionLockedTimer > 0f)
        {
            selectionLockedTimer -= Time.deltaTime;
            return;
        }

        GameObject hoveredObject = GetSelectableUnderCursor();

        if (hoveredObject != null)
            SetSelection(hoveredObject);
        else
            ClearSelection();
    }

    /// <summary>
    /// Locks selection changes.
    /// </summary>
    public void LockSelection() => selectionLocked = true;

    /// <summary>
    /// Unlocks selection changes.
    /// </summary>
    public void UnlockSelection() => selectionLocked = false;

    /// <summary>
    /// Clears the current selection.
    /// </summary>
    public void ClearSelection() => SetSelection(null);

    /// <summary>
    /// Sets the current selection, restores layers of previously selected objects,
    /// and applies selection layer to the new selection.
    /// </summary>
    private void SetSelection(GameObject newSelection)
    {
        currentSelection = newSelection;
        bool showHierarchy = InputSystem.GetButton(InputButton.ShowHierarchy);

        GameObject itemToHightlight = newSelection;
        if (showHierarchy)
            if (newSelection != null)
                itemToHightlight = newSelection.transform.root.gameObject;
                
        ApplySelectionLayer(itemToHightlight, showHierarchy);
    }

    /// <summary>
    /// Returns the closest selectable object under the cursor using raycasting.
    /// </summary>
    private GameObject GetSelectableUnderCursor()
    {
        Ray ray = cam.ScreenPointToRay(InputSystem.GetPointerPosition());
        RaycastHit[] hits = Physics.RaycastAll(ray, raycastDistance);

        GameObject closest = null;
        float closestDistance = float.MaxValue;

        foreach (var hit in hits)
        {
            var selectable = hit.collider.GetComponentInParent<Selectable>();
            if (selectable == null || !selectable.enabled)
                continue;

            if (hit.distance < closestDistance)
            {
                closestDistance = hit.distance;
                closest = selectable.gameObject;
            }
        }

        return closest;
    }

    /// <summary>
    /// Applies the selection layer to the root object and all its descendants,
    /// storing their original layers for restoration.
    /// </summary>
    private void ApplySelectionLayer(GameObject target, bool recursive)
    {
        RestoreOriginalLayers();

        if (target == null) return;

        var stack = new Stack<GameObject>();
        stack.Push(target);

        while (stack.Count > 0)
        {
            var obj = stack.Pop();
            if (!recursive && obj != target && obj.GetComponent<Selectable>()) continue;

            if (!originalLayers.ContainsKey(obj))
                originalLayers[obj] = obj.layer;

            obj.layer = selectionLayer;

            foreach (Transform child in obj.transform)
                stack.Push(child.gameObject);
        }
    }

    public void HighlightObject(GameObject target, bool recursive = false, float timeToHightlight = 1f)
    {
        ApplySelectionLayer(target, recursive);
        selectionLockedTimer = timeToHightlight;
    }

    /// <summary>
    /// Restores the original layers of all previously selected objects.
    /// </summary>
    private void RestoreOriginalLayers()
    {
        foreach (var kvp in originalLayers)
        {
            if (kvp.Key != null)
                kvp.Key.layer = kvp.Value;
        }
        originalLayers.Clear();
    }
}
