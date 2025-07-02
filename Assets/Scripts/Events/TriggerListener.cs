using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// TriggerListener invokes UnityEvents based on entering or exiting triggers with specific object names.
/// </summary>
public class TriggerListener : MonoBehaviour
{
    [Serializable]
    public class TriggerEntry
    {
        /// <summary>
        /// Name of the GameObject to match (case-insensitive).
        /// </summary>
        public string objectName;

        /// <summary>
        /// Event invoked when an object with matching name enters the trigger.
        /// </summary>
        public UnityEvent OnTriggerEnter;

        /// <summary>
        /// Event invoked when an object with matching name exits the trigger.
        /// </summary>
        public UnityEvent OnTriggerExit;
    }

    /// <summary>
    /// List of trigger entries, each containing a target object name and events.
    /// </summary>
    public List<TriggerEntry> triggers = new List<TriggerEntry>();

    /// <summary>
    /// Called by Unity when another collider enters the trigger.
    /// Invokes OnTriggerEnter events for all matching entries.
    /// </summary>
    /// <param name="other">Collider entering the trigger.</param>
    private void OnTriggerEnter(Collider other)
    {
        string otherName = other.gameObject.name;

        foreach (var entry in triggers)
        {
            if (string.Equals(entry.objectName, otherName, StringComparison.OrdinalIgnoreCase))
            {
                entry.OnTriggerEnter?.Invoke();
            }
        }
    }

    /// <summary>
    /// Called by Unity when another collider exits the trigger.
    /// Invokes OnTriggerExit events for all matching entries.
    /// </summary>
    /// <param name="other">Collider exiting the trigger.</param>
    private void OnTriggerExit(Collider other)
    {
        string otherName = other.gameObject.name;

        foreach (var entry in triggers)
        {
            if (string.Equals(entry.objectName, otherName, StringComparison.OrdinalIgnoreCase))
            {
                entry.OnTriggerExit?.Invoke();
            }
        }
    }
}
