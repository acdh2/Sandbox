//EDITED

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

        if (WeldingUtils.IsWelded(selected))
            Unweld(selected);
        else
            Weld(selected);
    }

    /// <summary>
    /// Welds the root and all overlapping weldable objects connected recursively.
    /// </summary>
    public void Weld(GameObject target)
    {
        if (WeldingUtils.IsWelded(target))
        {
            Debug.LogError("Object is already welded");
            return;
        }
        selectionHandler.ClearSelection();

        // Zoek de weldable root van het geselecteerde object
        GameObject root = WeldingUtils.GetWeldableRoot(target);
        if (root == null) return;

        // Vind alle verbonden weldable roots via overlap
        HashSet<GameObject> weldGroup = FindAllConnectedWeldables(root);

        // Pas weld aan: alle weldables in de groep aan dezelfde parent zetten en Rigidbody verwijderen
        foreach (GameObject weldable in weldGroup)
        {
            WeldingUtils.RemoveRigidbody(weldable);
            weldable.transform.SetParent(root.transform, true);
        }

        WeldingUtils.RemoveRigidbody(root);
        StartCoroutine(AddRigidbodyNextFrame(root));

        // Notify welded objects
        foreach (GameObject weldable in weldGroup)
        {
            OnWeldEvent(weldable);
        }
        OnWeldEvent(root);
    }
        
    /// <summary>
    /// Unwelds the given root and all welded children recursively.
    /// </summary>
    public void Unweld(GameObject target)
    {
        if (!WeldingUtils.IsWelded(target))
        {
            Debug.LogError("Object is already welded");
            return;
        }

        selectionHandler.ClearSelection();

        GameObject root = WeldingUtils.GetWeldableRoot(target);
        if (root == null) return;

        // Verzamel alle verbonden weldables
        HashSet<GameObject> connected = WeldingUtils.FindConnectedWeldables(root);
        connected.Add(root); // root zelf ook losmaken

        // Ontkoppel alle weldables
        foreach (GameObject obj in connected)
        {
            if (obj.transform.parent != null)
                obj.transform.SetParent(null, true);

            WeldingUtils.RemoveRigidbody(obj);
        }

        // Roep OnUnweldEvent aan voor elk losgemaakt object
        foreach (GameObject obj in connected)
        {
            OnUnweldEvent(obj);
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
    /// Recursively finds all connected weldable root objects by collider overlap.
    /// </summary>
    private HashSet<GameObject> FindAllConnectedWeldables(GameObject startRoot)
    {
        var visited = new HashSet<GameObject>();
        var toVisit = new Queue<GameObject>();

        toVisit.Enqueue(startRoot);
        visited.Add(startRoot);

        while (toVisit.Count > 0)
        {
            GameObject current = toVisit.Dequeue();

            // Alle colliders van de weldable root
            Collider[] currentColliders = current.GetComponentsInChildren<Collider>();
            if (currentColliders.Length == 0) continue;

            // Voor iedere collider: zoek overlappende colliders
            foreach (var col in currentColliders)
            {
                // Zoek overlappende colliders die penetreren
                Collider[] overlappingColliders = WeldingUtils.FindOverlappingCollidersBySingleCollider(col, maxAllowedPenetration);

                foreach (Collider overlapCol in overlappingColliders)
                {
                    // Zoek weldable root van collider object
                    GameObject otherRoot = WeldingUtils.GetWeldableRoot(overlapCol.gameObject);

                    // Als weldable root gevonden en nog niet bezocht, toevoegen
                    if (otherRoot != null && !visited.Contains(otherRoot))
                    {
                        visited.Add(otherRoot);
                        toVisit.Enqueue(otherRoot);
                    }
                }
            }
        }

        return visited;
    }

    /// <summary>
    /// Calls the weld event on the object if it implements IWeldable.
    /// </summary>
    private void OnWeldEvent(GameObject obj)
    {
        obj.GetComponent<IWeldable>()?.OnWeld(this);
    }

    /// <summary>
    /// Calls the unweld event on the object if it implements IWeldable.
    /// </summary>
    private void OnUnweldEvent(GameObject obj)
    {
        obj.GetComponent<IWeldable>()?.OnUnweld(this);
    }
}
