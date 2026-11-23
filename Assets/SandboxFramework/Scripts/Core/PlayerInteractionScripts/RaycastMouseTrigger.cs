using UnityEngine;

/// <summary>
/// Casts a ray from the mouse pointer and triggers the MouseListener
/// of the object that was clicked (same aiming as the SelectionHandler).
/// </summary>
[DisallowMultipleComponent]
public class RaycastMouseTrigger : MonoBehaviour
{
    [Tooltip("Maximum raycast distance.")]
    [SerializeField] private float rayDistance = 5f;

    /// <summary>
    private Camera cam;

    private void Start()
    {
        cam = Camera.main;
    }

    /// <summary>
    /// Performs a raycast from the mouse pointer and returns the MouseListener component if found.
    /// </summary>
    private MouseListener GetMouseEventTarget()
    {
        Ray ray = cam.ScreenPointToRay(SandboxInput.GetPointerPosition());
        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance))
        {
            return hit.collider.GetComponent<MouseListener>();
        }
        return null;
    }

    private void Update()
    {
        // Check for left mouse button press
        if (SandboxInput.GetPointerDown())
        {
            GetMouseEventTarget()?.OnMouseDown();
        }

        // Check for left mouse button release
        if (SandboxInput.GetPointerUp())
        {
            GetMouseEventTarget()?.OnMouseUp();
        }
    }
}
