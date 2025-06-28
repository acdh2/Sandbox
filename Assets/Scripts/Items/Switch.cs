using System.Collections.Generic;
using UnityEngine;

public class Switch : MonoBehaviour
{
    [Tooltip("Only targets with a matching activationGroup color will respond.")]
    public Color activationGroup = Color.white;

    /// <summary>
    /// Activates all matching and inactive IActivatable components under the welded structure.
    /// </summary>
    public void TurnOn()
    {
        foreach (var target in FindAllActivatables())
        {
            if (!target.IsActive() && target.MatchActivationGroup(activationGroup))
            {
                target.OnActivate();
            }
        }
    }

    /// <summary>
    /// Deactivates all active IActivatable components under the welded structure.
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
    /// Toggles activation state of all IActivatable components under the welded structure.
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

    public void Reactivate()
    {
        TurnOff();
        TurnOn();
    }

    /// <summary>
    /// Finds all IActivatable components starting from the topmost parent of this object.
    /// </summary>
    private List<IActivatable> FindAllActivatables()
    {
        Transform root = transform.root;
        return new List<IActivatable>(root.GetComponentsInChildren<IActivatable>(true));
    }

}
