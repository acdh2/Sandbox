using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Handles object selection using raycasting against objects on specified layers (draggableLayers).
/// Applies a temporary selection layer for highlighting and restores original layers on deselection.
/// </summary>
public class SelectionHandler : MonoBehaviour
{
    [Header("Selection Settings")]
    [Tooltip("Which layers are considered draggable/selectable.")]
    public LayerMask draggableLayers;

    public GameObject CurrentSelection { get; private set; }
    public event Action<GameObject> OnSelectionChanged;

    private Camera cam;
    private bool selectionLocked = false;
    private int selectionLayer;

    private readonly Dictionary<GameObject, int> originalLayer = new();

    private void Start()
    {
        cam = Camera.main;
        selectionLayer = LayerMask.NameToLayer("Selection");
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

    public void LockSelection() => selectionLocked = true;
    public void UnlockSelection() => selectionLocked = false;
    public void ClearSelection() => SetSelection(null);

    private void SetSelection(GameObject newSelection)
    {
        
        //if (CurrentSelection == newSelection)
          //  return;

        RestoreOriginalLayers();

        CurrentSelection = newSelection;

        if (CurrentSelection != null)
            ApplySelectionLayer(CurrentSelection);

        OnSelectionChanged?.Invoke(CurrentSelection);
    }

    private GameObject GetDraggableRoot(GameObject item)
    {
        Transform currentTransform = item.transform;

        while (currentTransform != null)
        {
            GameObject candidate = currentTransform.gameObject;

            bool isDraggable = ((draggableLayers.value & (1 << candidate.layer)) != 0);
            bool isCurrentSelection = (candidate == CurrentSelection && candidate.layer == selectionLayer);

            if (isDraggable || isCurrentSelection)
            {
                return candidate;
            }

            currentTransform = currentTransform.parent;
        }

        return null;
    }

    private GameObject GetClosestInteractableUnderCursor()
    {
        int combinedLayerMask = draggableLayers | (1 << selectionLayer);

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 100f, combinedLayerMask);

        GameObject closest = null;
        float closestDistance = float.MaxValue;

        foreach (var hit in hits)
        {
            GameObject candidate = GetDraggableRoot(hit.collider.gameObject);
            if (candidate == null) continue;

            float distance = hit.distance;
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = candidate;
            }
        }

        return closest;
    }


    private void ApplySelectionLayer(GameObject root)
    {
        // root = GetDraggableRoot(root.transform.root.gameObject);
        // if (root)
        // {
        //     if (!originalLayer.ContainsKey(root))
        //         originalLayer[root] = root.layer;
        //     root.layer = selectionLayer;
        // }

        Stack<GameObject> stack = new();
        stack.Push(root);//.transform.root.gameObject);

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
            GameObject obj = kvp.Key;
            int original = kvp.Value;

            if (obj != null)
                obj.layer = original;
        }
        originalLayer.Clear();
    }
}
