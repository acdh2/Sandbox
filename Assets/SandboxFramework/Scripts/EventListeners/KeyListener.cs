using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using System.Collections.Generic;
using UnityEngine.InputSystem;  // For Key type

/// <summary>
/// Fires UnityEvents when a key is pressed or released, anywhere in the game
/// (the player does not need to look at or select this object).
/// </summary>
public class KeyListener : MonoBehaviour
{

    [System.Serializable]
    public class KeyEventPair
    {
        [Tooltip("Key to listen for")]
        public Key key;

        [Tooltip("Event triggered when the key is pressed")]
        [FormerlySerializedAs("onKeyPressEvent")]
        public UnityEvent onKeyPress;

        [Tooltip("Event triggered when the key is released")]
        [FormerlySerializedAs("onKeyReleaseEvent")]
        public UnityEvent onKeyRelease;
    }

    [Tooltip("List of key-event mappings")]
    public List<KeyEventPair> keyEvents = new();

    private void Update()
    {
        // No keyboard connected (e.g. on a touch device)
        if (Keyboard.current == null) return;

        foreach (KeyEventPair keyEventPair in keyEvents)
        {
            // Skip entries where no key has been chosen yet
            if (keyEventPair.key == Key.None) continue;

            var keyControl = Keyboard.current[keyEventPair.key];

            if (keyControl.wasPressedThisFrame)
            {
                keyEventPair.onKeyPress?.Invoke();
            }
            else if (keyControl.wasReleasedThisFrame)
            {
                keyEventPair.onKeyRelease?.Invoke();
            }
        }
    }
}
