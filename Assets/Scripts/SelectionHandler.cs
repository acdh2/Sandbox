using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class SelectionHandler : MonoBehaviour
{
    public LayerMask selectableLayers;
    public GameObject CurrentSelection { get; private set; }

    private Camera cam;
    private bool selectionLocked = false;

    private int selectionLayer;
    private int interactableLayer;

    // C# event, niet zichtbaar in Inspector
    public event Action<GameObject> OnSelectionChanged;

    void Start()
    {
        cam = Camera.main;
        selectionLayer = LayerMask.NameToLayer("Selection");
        interactableLayer = LayerMask.NameToLayer("Interactables");
    }

    public void ClearSelection()
    {
        SetSelection(null);
    }

    public void LockSelection() => selectionLocked = true;
    public void UnlockSelection() => selectionLocked = false;

    void Update()
    {
        if (selectionLocked) return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, selectableLayers))
        {
            GameObject top = GetTopmostRoot(hit.collider.transform).gameObject;
            SetSelection(top);
        }
        else
        {
            ClearSelection();
        }
    }

    private void SetSelection(GameObject newSelection)
    {
        if (CurrentSelection == newSelection) return;

        if (CurrentSelection != null)
            SetLayerIteratively(CurrentSelection, interactableLayer);

        CurrentSelection = newSelection;

        if (newSelection != null)
            SetLayerIteratively(newSelection, selectionLayer);

        // Event aanroepen als de selectie wijzigt
        OnSelectionChanged?.Invoke(CurrentSelection);
    }

    private Transform GetTopmostRoot(Transform transform)
    {
        while (transform.parent != null)
            transform = transform.parent;
        return transform;
    }

    private void SetLayerIteratively(GameObject root, int layer)
    {
        Stack<GameObject> stack = new Stack<GameObject>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            GameObject obj = stack.Pop();
            obj.layer = layer;

            foreach (Transform child in obj.transform)
                stack.Push(child.gameObject);
        }
    }
}
