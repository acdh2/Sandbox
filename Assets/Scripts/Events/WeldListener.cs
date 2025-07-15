using UnityEngine;
using UnityEngine.Events;

public class WeldListener : MonoBehaviour, IWeldListener
{
    public UnityEvent onWeld;
    public UnityEvent onUnweld;

    public UnityEvent onAdded;

    public UnityEvent onRemoved;

    public void OnWeld() => onWeld?.Invoke();
    public void OnUnweld() => onUnweld?.Invoke();
    
    public void OnAdded() => onAdded?.Invoke();
    public void OnRemoved() => onRemoved?.Invoke();
}
