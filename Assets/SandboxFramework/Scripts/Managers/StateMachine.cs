using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class StateMachine : MonoBehaviour
{
    [Serializable]
    public class State
    {
        public string name;
        public UnityEvent OnStart;
        public UnityEvent OnStop;
    }

    [Tooltip("Start state name, use 'None' for no active state")]
    [SerializeField]
    private string startState = "None";

    public List<State> states = new List<State>();

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
        if (!enabled) return;

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
