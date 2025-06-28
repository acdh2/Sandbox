using UnityEngine;
using UnityEngine.Events;

public class SwitchListener : MonoBehaviour, IActivatable
{
    [Header("Switch Events")]
    public Color switchGroup = Color.white;
    public UnityEvent onTurnOn;
    public UnityEvent onTurnOff;

    private bool isActive = false;

    public bool MatchActivationGroup(Color group) => switchGroup == group;

    /// <summary>
    /// Activates the object and fires the onActivate event.
    /// </summary>
    public void OnActivate()
    {
        if (!enabled || isActive) return;

        isActive = true;
        onTurnOn?.Invoke();
    }

    /// <summary>
    /// Deactivates the object and fires the onDeactivate event.
    /// </summary>
    public void OnDeactivate()
    {
        if (!enabled || !isActive) return;

        isActive = false;
        onTurnOff?.Invoke();
    }

    /// <summary>
    /// Returns whether the object is currently active.
    /// </summary>
    public bool IsActive() => isActive;
}
