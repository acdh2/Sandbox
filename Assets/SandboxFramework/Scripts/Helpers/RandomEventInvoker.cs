using UnityEngine;
using UnityEngine.Events;

public class RandomEventInvoker : MonoBehaviour
{
    [Tooltip("Lijst van mogelijke UnityEvents waarvan er willekeurig één wordt aangeroepen.")]
    public UnityEvent[] events;

    [Tooltip("Voer automatisch één van de events uit bij Start.")]
    public bool invokeOnStart = false;

    void Start()
    {
        if (invokeOnStart)
        {
            InvokeRandomEvent();
        }
    }

    /// <summary>
    /// Roept een willekeurig UnityEvent aan uit de lijst.
    /// </summary>
    public void InvokeRandomEvent()
    {
        if (!enabled) return;
        
        if (events == null || events.Length == 0)
        {
            Debug.LogWarning($"{name}: Geen events beschikbaar om aan te roepen.");
            return;
        }

        int index = Random.Range(0, events.Length);
        if (events[index] != null)
        {
            events[index].Invoke();
        }
        else
        {
            Debug.LogWarning($"{name}: Geselecteerd event is null.");
        }
    }
}
