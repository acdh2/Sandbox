using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

    public class SeatListener : KeyPressListener, ISeatListener
{
    public UnityEvent onSeat;
    public UnityEvent onUnseat;

    public void OnSeat() => onSeat?.Invoke();
    public void OnUnseat() => onUnseat?.Invoke();

}
