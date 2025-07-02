using UnityEngine;
using UnityEngine.Events;

public class DragListener : MonoBehaviour, IGrabbable
{
    public UnityEvent onGrab;
    public UnityEvent onRelease;

    public void OnGrab() => onGrab?.Invoke();

    public void OnRelease() => onRelease?.Invoke();
}
