using UnityEngine;

[RequireComponent(typeof(SelectionHandler))]
public class OverlapChecker : MonoBehaviour
{
    private SelectionHandler selectionHandler;

    private float maxAllowedPenetration = 0.01f;

    void Start()
    {
        selectionHandler = GetComponent<SelectionHandler>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Z))
        {
            var selectedObj = selectionHandler.CurrentSelection;
            if (selectedObj != null)
            {
                Rigidbody rb = selectedObj.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    var overlapping = GetOverlappingColliders(rb);
                    Debug.Log($"Found {overlapping.Length} overlapping colliders:");

                    foreach (var col in overlapping)
                    {
                        Debug.Log($" - {col.gameObject.name}");
                    }
                }
                else
                {
                    Debug.LogWarning("Geselecteerd object heeft geen Rigidbody.");
                }
            }
            else
            {
                Debug.LogWarning("Er is geen object geselecteerd.");
            }
        }
    }

    Collider[] GetOverlappingColliders(Rigidbody rigidbody)
    {
        Collider[] colliders = rigidbody.GetComponentsInChildren<Collider>();

        if (colliders.Length == 0)
            return new Collider[0];

        Bounds bounds = new Bounds(rigidbody.transform.position, Vector3.zero);
        foreach (var col in colliders)
            bounds.Encapsulate(col.bounds);

        Collider[] candidates = Physics.OverlapBox(bounds.center, bounds.extents, rigidbody.transform.rotation);

        System.Collections.Generic.List<Collider> result = new System.Collections.Generic.List<Collider>();

        foreach (var candidate in candidates)
        {
            // Eigen colliders overslaan
            bool isOwnCollider = false;
            foreach (var ownCol in colliders)
            {
                if (candidate == ownCol)
                {
                    isOwnCollider = true;
                    break;
                }
            }
            if (isOwnCollider)
                continue;

            // Penetration check (optioneel: hier kun je filteren op afstand)
            bool overlapped = false;
            foreach (var ownCol in colliders)
            {
                if (Physics.ComputePenetration(
                    ownCol, ownCol.transform.position, ownCol.transform.rotation,
                    candidate, candidate.transform.position, candidate.transform.rotation,
                    out Vector3 direction, out float distance))
                {
                    if (distance > maxAllowedPenetration)
                    {
                        overlapped = true;
                        break;
                    }
                }
            }

            if (overlapped)
                result.Add(candidate);
        }

        return result.ToArray();
    }
}
