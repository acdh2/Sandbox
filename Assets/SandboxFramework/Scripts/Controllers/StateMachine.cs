using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

[DisallowMultipleComponent] // Prevent multiple instances of this component on one GameObject
public class StateMachine : MonoBehaviour
{
    [Serializable]
    public class State
    {
        public string name;           // Name identifier of the state
        [FormerlySerializedAs("OnStart")]
        public UnityEvent onStart;    // Event triggered when the state starts

        [FormerlySerializedAs("OnStop")]
        public UnityEvent onStop;     // Event triggered when the state stops
    }

    [Tooltip("Start state name, use 'None' for no active state")]
    [SerializeField]
    private string startState = "None";  // Initial state name, 'None' means no active state

    public List<State> states = new List<State>(); // List of all possible states

    private State currentState = null;   // Currently active state

    // Public read-only property to get the current state's name or 'None' if no active state
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
        // Set the initial state when the script starts
        SetState(startState);
    }

    /// <summary>
    /// Changes the current state to the specified state by name.
    /// If the state is "None", stops any active state.
    /// </summary>
    /// <param name="stateName">Name of the state to activate</param>
    public void SetState(string stateName)
    {
        if (!enabled) return; // Do nothing if this component is disabled

        // If stateName is "None", stop current state if any and clear active state
        if (string.Equals(stateName, "None", StringComparison.OrdinalIgnoreCase))
        {
            if (currentState != null)
            {
                currentState.onStop?.Invoke();
                currentState = null;
            }
            return;
        }

        // Find the state with the matching name (case-insensitive)
        var newState = states.Find(s => string.Equals(s.name, stateName, StringComparison.OrdinalIgnoreCase));
        if (newState == null)
        {
            Debug.LogWarning($"StateMachine: State '{stateName}' not found!");
            return;
        }

        // If the new state is already active, do nothing
        if (newState == currentState)
            return;

        // If there is a current active state, invoke its onStop event
        if (currentState != null)
            currentState.onStop?.Invoke();

        // Activate the new state and invoke its onStart event
        currentState = newState;
        currentState.onStart?.Invoke();
    }

    /// <summary>
    /// Changes to the next state in the list.
    /// After the last state it starts again with the first one.
    /// With two states this toggles between them.
    /// </summary>
    public void NextState()
    {
        if (!enabled) return; // Do nothing if this component is disabled

        // Do nothing if there are no states to switch to
        if (states.Count == 0)
            return;

        // Find the position of the current state in the list (-1 if no state is active)
        int currentIndex = states.IndexOf(currentState);

        // Go to the next state, wrapping around to the first state after the last one
        int nextIndex = (currentIndex + 1) % states.Count;
        SetState(states[nextIndex].name);
    }
}
