using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Base component for all sandbox elements that can be grabbed, welded and activated.
/// Handles group-based activation and event invocation for interaction logic.
/// </summary>
public class SandboxBase : MonoBehaviour, IWeldable, IGrabbable
{
    [Header("Weld & Grab Events")]
    public UnityEvent onWeld;
    public UnityEvent onUnweld;
    public UnityEvent onGrab;
    public UnityEvent onRelease;

    // --- IWeldable interface implementation ---

    /// <summary>
    /// Called when the object is welded into place.
    /// </summary>
    public void OnWeld() => onWeld?.Invoke();

    /// <summary>
    /// Called when the object is unwelded.
    /// </summary>
    public void OnUnweld() => onUnweld?.Invoke();

    /// <summary>
    /// Called when the object is grabbed.
    /// </summary>
    public void OnGrab() => onGrab?.Invoke();

    /// <summary>
    /// Called when the object is released.
    /// </summary>
    public void OnRelease() => onRelease?.Invoke();
}
