using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Handles object selection using raycasting against tagged objects (e.g., "Draggable").
/// Applies a temporary selection layer for highlighting and restores original layers on deselection.
/// </summary>
public class SelectionHandler : MonoBehaviour
{
    public GameObject CurrentSelection { get; private set; }

    public event Action<GameObject> OnSelectionChanged;

    private Camera cam;
    private bool selectionLocked = false;

    private int selectionLayer;
    private int defaultLayer;

    // Stores the original layers so we can restore them on deselection
    private readonly Dictionary<GameObject, int> originalLayer = new();

    private void Start()
    {
        cam = Camera.main;
        selectionLayer = LayerMask.NameToLayer("Selection");
        defaultLayer = LayerMask.NameToLayer("Default");
    }

    private void Update()
    {
        if (selectionLocked)
            return;

        GameObject hoveredObject = GetClosestInteractableUnderCursor();

        if (hoveredObject != null)
            SetSelection(hoveredObject);
        else
            ClearSelection();
    }

    /// <summary>
    /// Locks the current selection so it doesn't change automatically.
    /// </summary>
    public void LockSelection() => selectionLocked = true;

    /// <summary>
    /// Unlocks the current selection to allow updates via raycasting.
    /// </summary>
    public void UnlockSelection() => selectionLocked = false;

    /// <summary>
    /// Clears the current selection (if any).
    /// </summary>
    public void ClearSelection() => SetSelection(null);

    /// <summary>
    /// Sets a new selection. Applies or restores layers as needed.
    /// </summary>
    private void SetSelection(GameObject newSelection)
    {
        if (CurrentSelection == newSelection)
            return;

        // Restore layers of previously selected object
        if (CurrentSelection != null)
            RestoreOriginalLayers(CurrentSelection);

        CurrentSelection = newSelection;

        // Apply selection layer to new object
        if (CurrentSelection != null)
            ApplySelectionLayer(CurrentSelection);

        OnSelectionChanged?.Invoke(CurrentSelection);
    }

    /// <summary>
    /// Returns the topmost parent of a transform hierarchy.
    /// </summary>
    private Transform GetTopmostRoot(Transform t)
    {
        while (t.parent != null)
            t = t.parent;
        return t;
    }

    /// <summary>
    /// Performs a raycast and returns the topmost interactable object under the cursor.
    /// </summary>
    private GameObject GetClosestInteractableUnderCursor()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 100f);

        GameObject closest = null;
        float closestDistance = float.MaxValue;

        foreach (var hit in hits)
        {
            if (!hit.collider.CompareTag(Tags.Draggable))
                continue;

            float distance = hit.distance;
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = GetTopmostRoot(hit.collider.transform).gameObject;
            }
        }

        return closest;
    }

    /// <summary>
    /// Recursively applies the selection layer to the given GameObject and all its children.
    /// Stores original layers for later restoration.
    /// </summary>
    private void ApplySelectionLayer(GameObject root)
    {
        Stack<GameObject> stack = new();
        stack.Push(root);

        while (stack.Count > 0)
        {
            GameObject obj = stack.Pop();

            if (!originalLayer.ContainsKey(obj))
                originalLayer[obj] = obj.layer;

            obj.layer = selectionLayer;

            foreach (Transform child in obj.transform)
                stack.Push(child.gameObject);
        }
    }

    /// <summary>
    /// Recursively restores original layers to the given GameObject and all its children.
    /// </summary>
    private void RestoreOriginalLayers(GameObject root)
    {
        Stack<GameObject> stack = new();
        stack.Push(root);

        while (stack.Count > 0)
        {
            GameObject obj = stack.Pop();

            if (originalLayer.TryGetValue(obj, out int layer))
            {
                obj.layer = layer;
                originalLayer.Remove(obj);
            }

            foreach (Transform child in obj.transform)
                stack.Push(child.gameObject);
        }
    }
}
