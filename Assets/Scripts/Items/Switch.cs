using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents a switch that can activate, deactivate or toggle IActivatable components
/// within the welded structure based on an activation group color.
/// </summary>
public class Switch : MonoBehaviour
{
    [Tooltip("Only targets with a matching activationGroup color will respond.")]
    public Color activationGroup = Color.white;

    /// <summary>
    /// Activates all matching and currently inactive IActivatable components within the welded structure.
    /// </summary>
    public void TurnOn()
    {
        foreach (var target in FindAllActivatables())
        {
            // Activate only if inactive and matching the activation group
            if (!target.IsActive() && target.MatchActivationGroup(activationGroup))
            {
                target.OnActivate();
            }
        }
    }

    /// <summary>
    /// Deactivates all currently active IActivatable components within the welded structure.
    /// </summary>
    public void TurnOff()
    {
        foreach (var target in FindAllActivatables())
        {
            if (target.IsActive())
            {
                target.OnDeactivate();
            }
        }
    }

    /// <summary>
    /// Toggles the activation state of all IActivatable components within the welded structure.
    /// Only activates those matching the activation group when toggling on.
    /// </summary>
    public void Toggle()
    {
        foreach (var target in FindAllActivatables())
        {
            if (target.IsActive())
            {
                target.OnDeactivate();
            }
            else if (target.MatchActivationGroup(activationGroup))
            {
                target.OnActivate();
            }
        }
    }

    /// <summary>
    /// Forces a reactivation cycle: first turns off all active components,
    /// then turns on all matching inactive components.
    /// </summary>
    public void Reactivate()
    {
        TurnOff();
        TurnOn();
    }

    /// <summary>
    /// Retrieves all IActivatable components in this object's root welded hierarchy,
    /// including inactive ones.
    /// </summary>
    /// <returns>List of all IActivatable components found</returns>
    private List<IActivatable> FindAllActivatables()
    {
        Transform root = transform.root;
        List<IActivatable> result = new List<IActivatable>(root.GetComponentsInChildren<IActivatable>(true));

        Weldable weldable = GetComponent<Weldable>();
        if (weldable)
        {
            foreach (Weldable other in weldable.GetAllConnectedRecursive())
            {
                Transform otherRoot = other.transform.root;
                if (otherRoot != root)
                {
                    foreach (IActivatable activatable in otherRoot.GetComponentsInChildren<IActivatable>(true))
                    {
                        result.Add(activatable);
                    }
                }
            }
        }

        return result;
    }
}
