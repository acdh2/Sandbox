using UnityEngine;

/// <summary>
/// A button that activates or deactivates connected components when pressed.
/// Beheert debounce-tijd en verandert gedrag op basis van wel of niet gelast zijn.
/// </summary>
public class ButtonScript : ActivatorBase, IWeldable
{
    private const float DebounceInterval = 2f;

    private bool canBePressed = false;
    private bool isDebouncing = false;

    /// <summary>
    /// Zet de zichtbaarheid van de knop aan of uit.
    /// </summary>
    private void SetVisible(bool visible)
    {
        GetComponent<Renderer>().enabled = visible;
    }

    /// <summary>
    /// Wordt aangeroepen als de knop wordt ingedrukt.
    /// </summary>
    public void OnButtonPress()
    {
        DoButtonAction(true);
    }

    /// <summary>
    /// Wordt aangeroepen als de knop wordt losgelaten.
    /// </summary>
    public void OnButtonRelease()
    {
        DoButtonAction(false);
    }

    /// <summary>
    /// Activeert of deactiveert het gekoppelde systeem, als de knop indrukbaar is.
    /// Voert ook debounce-logica uit om snel herhaald indrukken te voorkomen.
    /// </summary>
    private void DoButtonAction(bool shouldActivate)
    {
        if (!enabled || !canBePressed || isDebouncing) return;

        if (shouldActivate)
            Activate();
        else
            Deactivate();

        // Start debounce-timer: tijdelijk niet meer indrukbaar
        isDebouncing = true;
        SetVisible(false);

        InvokeHelper.InvokeAfter(DebounceInterval, () =>
        {
            isDebouncing = false;
            SetVisible(true);
        });
    }

    /// <summary>
    /// Wordt aangeroepen wanneer het object wordt gelast.
    /// Zet de tag naar Untagged en maakt indrukken mogelijk.
    /// </summary>
    public void OnWeld()
    {
        gameObject.tag = Tags.Untagged;
        canBePressed = true;
    }

    /// <summary>
    /// Wordt aangeroepen wanneer het object wordt losgelast.
    /// Zet de tag naar Draggable en voorkomt indrukken.
    /// </summary>
    public void OnUnweld()
    {
        gameObject.tag = Tags.Draggable;
        canBePressed = false;
    }
}
