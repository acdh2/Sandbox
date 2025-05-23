using System.Collections.Generic;
using UnityEngine;

public class SelectionHandler : MonoBehaviour
{
    public LayerMask selectableLayers;

    public GameObject CurrentSelection { get; private set; }

    private Camera cam;
    private bool selectionLocked = false;

    // Voor het opslaan van originele materialen per renderer
    private Dictionary<Renderer, Material> originalMaterials = new Dictionary<Renderer, Material>();

    void Start()
    {
        cam = Camera.main;
    }

    public void LockSelection()
    {
        selectionLocked = true;
    }

    public void UnlockSelection()
    {
        selectionLocked = false;
    }

    Transform GetTopMostParent(Transform child)
    {
        if (child.parent == null) return child;
        return child.parent;
    }

    void Update()
    {
        if (selectionLocked) return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, selectableLayers))
        {
            SetSelection(GetTopMostParent(hit.collider.gameObject.transform).gameObject);
        }
        else
        {
            SetSelection(null);
        }
    }

    private void SetSelection(GameObject newSelection)
    {
        if (CurrentSelection == newSelection) return;

        ResetRendererColors(CurrentSelection);
        CurrentSelection = newSelection;

        if (newSelection != null)
        {
            SetRendererColors(newSelection, Color.red);
        }
    }

    private void SetRendererColors(GameObject target, Color color)
    {
        originalMaterials.Clear(); // Leegmaken voor nieuwe selectie

        foreach (var renderer in GetRenderersInObjectAndChildren(target))
        {
            if (renderer != null)
            {
                originalMaterials[renderer] = renderer.sharedMaterial;
                renderer.material.color = color;
            }
        }
    }

    private void ResetRendererColors(GameObject target)
    {
        if (target == null) return;

        foreach (var kvp in originalMaterials)
        {
            if (kvp.Key != null)
                kvp.Key.sharedMaterial = kvp.Value;
        }

        originalMaterials.Clear();
    }

    private IEnumerable<Renderer> GetRenderersInObjectAndChildren(GameObject target)
    {
        yield return target.GetComponent<Renderer>();

        foreach (Transform child in target.transform)
        {
            var childRenderer = child.GetComponent<Renderer>();
            if (childRenderer != null)
                yield return childRenderer;
        }
    }
}
