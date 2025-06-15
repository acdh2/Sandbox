using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Base component for all sandbox elements that can be grabbed, welded and activated.
/// Handles group-based activation and event invocation for interaction logic.
/// </summary>
public class SandboxBase : MonoBehaviour, IActivatable, IWeldable
{
    [Header("Weld & Grab Events")]
    public UnityEvent onWeld;
    public UnityEvent onUnweld;
    public UnityEvent onGrab;
    public UnityEvent onRelease;

    [Header("Activation Events")]
    public UnityEvent onActivate;
    public UnityEvent onDeactivate;

    [Header("Activation Settings")]
    public Color activationGroup = Color.white;

    private bool isActive = false;

    // --- IWeldable interface implementation ---

    /// <summary>
    /// Called when the object is welded into place.
    /// </summary>
    public void InvokeWeld() => onWeld?.Invoke();

    /// <summary>
    /// Called when the object is unwelded.
    /// </summary>
    public void InvokeUnweld() => onUnweld?.Invoke();

    /// <summary>
    /// Called when the object is grabbed.
    /// </summary>
    public void InvokeGrab() => onGrab?.Invoke();

    /// <summary>
    /// Called when the object is released.
    /// </summary>
    public void InvokeRelease() => onRelease?.Invoke();

    // --- IActivatable interface implementation ---

    /// <summary>
    /// Returns whether the given group matches this object's activation group.
    /// </summary>
    public bool MatchActivationGroup(Color group) => activationGroup == group;

    /// <summary>
    /// Activates the object and fires the onActivate event.
    /// </summary>
    public void Activate()
    {
        if (!enabled || isActive) return;

        isActive = true;
        onActivate?.Invoke();
    }

    /// <summary>
    /// Deactivates the object and fires the onDeactivate event.
    /// </summary>
    public void Deactivate()
    {
        if (!enabled || !isActive) return;

        isActive = false;
        onDeactivate?.Invoke();
    }

    /// <summary>
    /// Returns whether the object is currently active.
    /// </summary>
    public bool IsActive() => isActive;
}
