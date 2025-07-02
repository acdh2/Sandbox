using UnityEngine;

/// <summary>
/// Represents an object that can be selected, with description and drag state.
/// </summary>
public class Selectable : MonoBehaviour
{
    [Tooltip("Description of the object, displayed in UI or tooltips")]
    [SerializeField]
    private string objectDescription = "";

    [Tooltip("Indicates whether this object can be dragged")]
    [SerializeField]
    private bool isDraggable = false;

    /// <summary>
    /// Gets the description text of this object.
    /// </summary>
    public string ObjectDescription => objectDescription;

    /// <summary>
    /// Indicates if the object is draggable by the player.
    /// </summary>
    public bool IsDraggable => isDraggable;
}
