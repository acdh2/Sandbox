using System;
using UnityEngine;

/// <summary>
/// Represents an object that can be selected, with description and drag state.
/// </summary>
[DisallowMultipleComponent]
public class Selectable : MonoBehaviour
{
    [Tooltip("Description of the object, displayed in UI or tooltips")]
    [SerializeField]
    private string objectDescription = "";

    /// <summary>
    /// Description of the object, displayed in UI or tooltips
    /// </summary>
    public string ObjectDescription
    {
        get => objectDescription;
        set => objectDescription = value;
    }

}
