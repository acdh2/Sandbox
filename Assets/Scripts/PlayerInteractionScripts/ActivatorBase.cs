using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base class for components that trigger activation logic (e.g., buttons, pressure plates).
/// Searches the welded object hierarchy and activates or deactivates matching components.
/// </summary>
public class ActivatorBase : MonoBehaviour
{
    [Tooltip("Only targets with a matching activationGroup color will respond.")]
    public Color activationGroup = Color.white;

    /// <summary>
    /// Activates all matching and inactive IActivatable components under the welded structure.
    /// </summary>
    public void Activate()
    {
        foreach (var target in FindAllActivatables())
        {
            if (!target.IsActive() && target.MatchActivationGroup(activationGroup))
            {
                target.Activate();
            }
        }
    }

    /// <summary>
    /// Deactivates all active IActivatable components under the welded structure.
    /// </summary>
    public void Deactivate()
    {
        foreach (var target in FindAllActivatables())
        {
            if (target.IsActive())
            {
                target.Deactivate();
            }
        }
    }

    /// <summary>
    /// Toggles activation state of all IActivatable components under the welded structure.
    /// </summary>
    public void Toggle()
    {
        foreach (var target in FindAllActivatables())
        {
            if (target.IsActive())
            {
                target.Deactivate();
            }
            else if (target.MatchActivationGroup(activationGroup))
            {
                target.Activate();
            }
        }
    }

    /// <summary>
    /// Finds all IActivatable components starting from the topmost parent of this object.
    /// </summary>
    private List<IActivatable> FindAllActivatables()
    {
        Transform root = GetTopmostParent(transform);
        return new List<IActivatable>(root.GetComponentsInChildren<IActivatable>(true));
    }

    /// <summary>
    /// Traverses up the transform hierarchy to find the topmost parent.
    /// </summary>
    private Transform GetTopmostParent(Transform current)
    {
        while (current.parent != null)
        {
            current = current.parent;
        }
        return current;
    }
}
