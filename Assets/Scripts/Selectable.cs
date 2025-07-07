using UnityEngine;

/// <summary>
/// Represents an object that can be selected, with description and drag state.
/// </summary>
[DisallowMultipleComponent]
public class Selectable : MonoBehaviour, IDragListener
{
    [Tooltip("Description of the object, displayed in UI or tooltips")]
    [SerializeField]
    private string objectDescription = "";

    [Tooltip("Indicates whether this object can be dragged")]
    [SerializeField]
    private bool isDraggable = true;

    /// <summary>
    /// Gets the description text of this object.
    /// </summary>
    public string ObjectDescription => objectDescription;

    /// <summary>
    /// Indicates if the object is draggable by the player.
    /// </summary>
    public bool IsDraggable => isDraggable;

    public bool MakeRigidbodyKinematicWhenDragged = false;

    private bool previousKinematicSetting = false;

    private bool isBeingDragged = false;

    public void OnGrab()
    {
        if (isBeingDragged) return;
        isBeingDragged = true;

        if (MakeRigidbodyKinematicWhenDragged)
        {
            Rigidbody rigidbody = GetComponent<Rigidbody>();
            if (rigidbody)
            {
                previousKinematicSetting = rigidbody.isKinematic;
                rigidbody.isKinematic = true;
            }
        }
    }

    public void OnRelease()
    {
        if (!isBeingDragged) return;
        isBeingDragged = false;

        if (MakeRigidbodyKinematicWhenDragged)
        {
            Rigidbody rigidbody = GetComponent<Rigidbody>();
            if (rigidbody)
            {
                rigidbody.isKinematic = previousKinematicSetting;
            }
        }
    }
}
