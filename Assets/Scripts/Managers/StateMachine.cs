using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class StateMachine : MonoBehaviour
{
    [Serializable]
    public class State
    {
        public string name;
        public UnityEvent OnStart;
        public UnityEvent OnStop;
    }

    public List<State> states = new List<State>();

    [Tooltip("Start state name, use 'None' for no active state")]
    public string startState = "None";

    private State currentState = null;

    public string CurrentState
    {
        get
        {
            if (currentState != null)
            {
                return currentState.name;
            }
            return "None";
        }
    }

    void Start()
    {
        SetState(startState);
    }

    public void SetState(string stateName)
    {
        if (string.Equals(stateName, "None", StringComparison.OrdinalIgnoreCase))
        {
            if (currentState != null)
            {
                currentState.OnStop?.Invoke();
                currentState = null;
            }
            return;
        }

        var newState = states.Find(s => string.Equals(s.name, stateName, StringComparison.OrdinalIgnoreCase));
        if (newState == null)
        {
            Debug.LogWarning($"StateMachine: State '{stateName}' not found!");
            return;
        }

        if (newState == currentState)
            return; // al actief, niets doen

        if (currentState != null)
            currentState.OnStop?.Invoke();

        currentState = newState;
        currentState.OnStart?.Invoke();
    }
}
