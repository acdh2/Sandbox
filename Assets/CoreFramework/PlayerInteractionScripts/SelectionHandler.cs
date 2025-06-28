using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Handles object selection using raycasting against objects that have a Selectable component.
/// Applies a temporary selection layer for highlighting and restores original layers on deselection.
/// </summary>
public class SelectionHandler : MonoBehaviour
{
    [Header("Selection Settings")]
    [Tooltip("Layer used for visual feedback on selected objects.")]
    public string selectionLayerName = "Selection";

    public GameObject CurrentSelection { get; private set; }

    private Camera cam;
    private bool selectionLocked = false;
    private int selectionLayer;

    private readonly Dictionary<GameObject, int> originalLayer = new();

    private void Start()
    {
        cam = Camera.main;
        selectionLayer = LayerMask.NameToLayer(selectionLayerName);
    }

    private void Update()
    {
        if (selectionLocked)
            return;

        GameObject hoveredObject = GetSelectableUnderCursor();

        if (hoveredObject != null)
            SetSelection(hoveredObject);
        else
            ClearSelection();
    }

    public void LockSelection() => selectionLocked = true;
    public void UnlockSelection() => selectionLocked = false;
    public void ClearSelection() => SetSelection(null);

    private void SetSelection(GameObject newSelection)
    {
        // if (CurrentSelection == newSelection) 
        //     return;

        RestoreOriginalLayers();
        CurrentSelection = newSelection;

        if (CurrentSelection != null)
            ApplySelectionLayer(CurrentSelection);
    }

    private GameObject GetSelectableUnderCursor()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 100f, ~0); // All layers

        GameObject closest = null;
        float closestDistance = float.MaxValue;

        foreach (var hit in hits)
        {
            Selectable selectable = hit.collider.GetComponentInParent<Selectable>();
            if (selectable == null)
                continue;

            float distance = hit.distance;
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = selectable.gameObject;
            }
        }

        return closest;
    }

    private void ApplySelectionLayer(GameObject root)
    {
        Stack<GameObject> stack = new();
        stack.Push(root.transform.root.gameObject);

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

    private void RestoreOriginalLayers()
    {
        foreach (var kvp in originalLayer)
        {
            if (kvp.Key != null)
                kvp.Key.layer = kvp.Value;
        }
        originalLayer.Clear();
    }
}
