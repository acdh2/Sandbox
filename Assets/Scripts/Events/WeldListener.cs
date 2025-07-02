using UnityEngine;
using UnityEngine.Events;

public class WeldListener : MonoBehaviour, IWeldListener
{
    public UnityEvent onWeld;
    public UnityEvent onUnweld;

    public void OnWeld() => onWeld?.Invoke();

    public void OnUnweld() => onUnweld?.Invoke();
}
