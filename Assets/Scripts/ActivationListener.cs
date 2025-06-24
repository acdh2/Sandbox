using UnityEngine;
using UnityEngine.Events;

public class ActivationListener : MonoBehaviour, IActivatable
{
    [Header("Activation Events")]
    public Color activationGroup = Color.white;
    public UnityEvent onActivate;
    public UnityEvent onDeactivate;

    private bool isActive = false;

    public bool MatchActivationGroup(Color group) => activationGroup == group;

    /// <summary>
    /// Activates the object and fires the onActivate event.
    /// </summary>
    public void OnActivate()
    {
        if (!enabled || isActive) return;

        isActive = true;
        onActivate?.Invoke();
    }

    /// <summary>
    /// Deactivates the object and fires the onDeactivate event.
    /// </summary>
    public void OnDeactivate()
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
