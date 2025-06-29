using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using NUnit.Framework.Interfaces;
using UnityEngine;

public enum WeldMode
{
    None,
    AttachableOnly,
    ReceivableOnly,
    Both
}

public class Weldable : MonoBehaviour
{
    public bool CanAttach => mode == WeldMode.AttachableOnly || mode == WeldMode.Both;
    public bool CanReceive => mode == WeldMode.ReceivableOnly || mode == WeldMode.Both;
    public WeldMode mode = WeldMode.Both;

    private HashSet<Weldable> connectedObjects = new();

    void Start()
    {
        // Kijk of deze al als child onder een andere Weldable zit
        Transform parent = transform.parent;
        while (parent != null)
        {
            Weldable parentWeld = parent.GetComponent<Weldable>();
            if (parentWeld != null)
            {
                parentWeld.AddConnection(this);
                break;
            }
            parent = parent.parent;
        }
    }

    public void AddConnection(Weldable newConnection, bool reciprocal = true)
    {
        if (!connectedObjects.Contains(newConnection))
        {
            if (connectedObjects.Count == 0)
            {
                OnWeld();
            }
            connectedObjects.Add(newConnection);
            newConnection.AddConnection(this, false);
        }
    }

    public void RemoveConnection(Weldable connection, bool reciprocal = true)
    {
        if (connectedObjects.Contains(connection))
        {
            connectedObjects.Remove(connection);
            connection.RemoveConnection(this, false);            
            if (connectedObjects.Count == 0)
            {
                OnUnweld();
            }
        }
    }

    List<IWeldListener> CollectConnectedIWeldListeners()
    {
        List<IWeldListener> listeners = new List<IWeldListener>();

        // Voeg IWeldListeners van dit object toe
        listeners.AddRange(GetComponents<IWeldListener>());

        // Voeg IWeldListener van de parent toe (als die er is)
        if (transform.parent)
        {
            IWeldListener parentListener = transform.parent.GetComponent<IWeldListener>();
            if (parentListener != null)
                listeners.Add(parentListener);
        }

        // Iteratieve boomdoorzoeking voor children, skip subtree bij Weldable
        Stack<Transform> stack = new Stack<Transform>();

        // Start met directe children
        foreach (Transform child in transform)
        {
            stack.Push(child);
        }

        while (stack.Count > 0)
        {
            Transform current = stack.Pop();

            // Als Weldable aanwezig is, skip subtree
            if (current.GetComponent<Weldable>() != null)
                continue;

            // Voeg IWeldListeners toe op dit object
            listeners.AddRange(current.GetComponents<IWeldListener>());

            // Voeg kinderen toe aan stack
            foreach (Transform child in current)
            {
                stack.Push(child);
            }
        }

        return listeners;
    }

    private void DetachChildren()
    {
        List<Weldable> found = new();
        Stack<Transform> stack = new();

        foreach (Transform child in transform)
        {
            stack.Push(child);
        }

        while (stack.Count > 0)
        {
            Transform current = stack.Pop();
            Weldable weldable = current.GetComponent<Weldable>();

            // Check for Weldable
            if (weldable != null)
            {
                found.Add(weldable);
                // Skip its children
                continue;
            }

            // No Weldable? Keep exploring
            foreach (Transform child in current)
            {
                stack.Push(child);
            }
        }

        foreach (Weldable weldable in found)
        {
            weldable.transform.SetParent(null, true);
            weldable.RemoveConnection(this);
            RemoveConnection(weldable);
        }
    }

    public void Weld(Weldable weldTo)
    {
        if (weldTo != null)
        {
            transform.SetParent(weldTo.transform);
            AddConnection(weldTo);
        }
    }

    public void Unweld()
    {
        if (transform.parent)
        {
            Weldable weldableParent = transform.parent.gameObject.GetComponentInParent<Weldable>(true);
            if (weldableParent)
            {
                transform.SetParent(null, true);
                RemoveConnection(weldableParent);
            }
        }
        DetachChildren();
    }

    private void OnWeld()
    {
        foreach (IWeldListener weldable in CollectConnectedIWeldListeners())
        {
            weldable.OnWeld();
        }
    }

    private void OnUnweld()
    {
        foreach (IWeldListener weldable in CollectConnectedIWeldListeners())
        {
            {
                weldable.OnUnweld();
            }
        }
    }

}