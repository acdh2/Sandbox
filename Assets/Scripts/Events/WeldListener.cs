using UnityEngine;
using UnityEngine.Events;

public class WeldListener : MonoBehaviour, IWeldListener
{
    [Header("Weld & Grab Events")]
    public UnityEvent onWeld;
    public UnityEvent onUnweld;

    public void OnWeld() => onWeld?.Invoke();

    public void OnUnweld() => onUnweld?.Invoke();
}
