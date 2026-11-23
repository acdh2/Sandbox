using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using UnityEngine.InputSystem;  // For Key type
using UnityEngine.Serialization;

/// <summary>
/// Triggers UnityEvents for keys pressed while the player sits on a Seat in the same welded structure.
/// The Seat forwards the keys from its keysToCheck list.
/// (For keys that should work anywhere in the game, use KeyListener instead.)
/// </summary>
public class SeatedKeyListener : MonoBehaviour, ISeatedKeyListener
{
    [System.Serializable]
    public class KeyEventPair
    {
        [Tooltip("Key to listen for")]
        public Key key;

        [Tooltip("Event triggered when the key is pressed")]
        [FormerlySerializedAs("onKeyEvent")]
        public UnityEvent onKeyPress;
    }

    [Tooltip("List of key-event mappings")]
    public List<KeyEventPair> keyEvents = new();

    /// <summary>
    /// Called when a key is pressed.
    /// Invokes all UnityEvents mapped to this key.
    /// </summary>
    /// <param name="keyCode">The key that was pressed</param>
    public void OnKeyPress(Key keyCode)
    {
        // Iterate over all key-event pairs and invoke matching events
        foreach (var keyEvent in keyEvents)
        {
            if (keyEvent.key == keyCode)
            {
                keyEvent.onKeyPress?.Invoke();
            }
        }
    }
}
