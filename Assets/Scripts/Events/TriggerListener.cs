using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TriggerListener : MonoBehaviour
{
    [Serializable]
    public class TriggerEntry
    {
        public string objectName;
        public UnityEvent OnTriggerEnter;
        public UnityEvent OnTriggerExit;
    }

    public List<TriggerEntry> triggers = new List<TriggerEntry>();

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
