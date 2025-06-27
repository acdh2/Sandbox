using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class KeyPressListener : MonoBehaviour, IKeypressListener
{
    [System.Serializable]
    public class KeyEventPair
    {
        public KeyCode key;
        public UnityEvent onKeyEvent;
    }

    public List<KeyEventPair> keyEvents = new List<KeyEventPair>();

    public void OnKeyPress(KeyCode keyCode)
    {
        // Loop door alle koppelingen en invoke de events van alle matching keys
        foreach (var keyEvent in keyEvents)
        {
            if (keyEvent.key == keyCode)
            {
                keyEvent.onKeyEvent?.Invoke();
            }
        }
    }
    
}
