using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

/// <summary>
/// Invokes UnityEvents when the player clicks on this GameObject.
/// Requires a Collider component, because Unity uses it to detect the click.
/// </summary>
[RequireComponent(typeof(Collider))]
public class MouseListener : MonoBehaviour
{
    [Header("Mouse Events")]
    [Tooltip("Invoked when the mouse button is pressed down on this GameObject.")]
    [FormerlySerializedAs("onMouseDownEvent")]
    public UnityEvent onMouseDown;

    [Tooltip("Invoked when the mouse button is released over this GameObject.")]
    [FormerlySerializedAs("onMouseUpEvent")]
    public UnityEvent onMouseUp;

    /// <summary>
    /// Called automatically by Unity when the mouse button is pressed
    /// while the cursor is over this GameObject's collider.
    /// </summary>
    public void OnMouseDown()
    {
        if (!enabled) return;
        onMouseDown?.Invoke();
    }

    /// <summary>
    /// Called automatically by Unity when the mouse button is released
    /// while the cursor is over this GameObject's collider.
    /// </summary>
    public void OnMouseUp()
    {
        if (!enabled) return;
        onMouseUp?.Invoke();
    }
}
