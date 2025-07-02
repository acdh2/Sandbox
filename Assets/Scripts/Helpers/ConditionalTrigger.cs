using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ConditionalEvent : MonoBehaviour
{
    // Defines the types of conditions that can be checked
    [Serializable]
    public enum ConditionType
    {
        ObjectName,
        Tag,
        Layer,
        State,
        MaterialName,
        AnimatorState
    }

    // Represents a single filter condition with optional negation
    [Serializable]
    public class FilterCondition
    {
        public ConditionType conditionType;
        public string value;
        public bool negate;
    }

    // List of all conditions to evaluate
    public List<FilterCondition> conditions = new List<FilterCondition>();

    // Defines how multiple conditions are combined
    public enum LogicalOperator
    {
        And,
        Or
    }

    public LogicalOperator conditionLogic = LogicalOperator.And;

    // If true, evaluate automatically when colliders enter or exit
    public bool autoTrigger = true;

    // Event invoked when conditions are met
    public UnityEvent onConditionsMet;

    // Tracks colliders currently inside the trigger area
    private readonly HashSet<Collider> collidersInTrigger = new HashSet<Collider>();


    private void OnTriggerEnter(Collider other)
    {
        if (!enabled || other == null || other.gameObject == null)
            return;

        collidersInTrigger.Add(other);

        if (autoTrigger)
            Evaluate();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!enabled || other == null || other.gameObject == null)
            return;

        collidersInTrigger.Remove(other);

        if (autoTrigger)
            Evaluate();
    }

    /// <summary>
    /// Evaluates all conditions against all colliders currently inside the trigger,
    /// combining the results with the specified logical operator.
    /// Invokes the event if conditions are met.
    /// </summary>
    private void Evaluate()
    {
        // Initialize result depending on logical operator
        bool result = (conditionLogic == LogicalOperator.And);

        foreach (var condition in conditions)
        {
            // Check if any collider satisfies this condition
            bool anyMatch = false;
            foreach (var col in collidersInTrigger)
            {
                if (EvaluateCondition(col, condition))
                {
                    anyMatch = true;
                    break;
                }
            }

            if (conditionLogic == LogicalOperator.And && !anyMatch)
            {
                // For AND, one failure is enough to fail overall
                result = false;
                break;
            }
            else if (conditionLogic == LogicalOperator.Or && anyMatch)
            {
                // For OR, one success is enough to succeed overall
                result = true;
                break;
            }
        }

        if (result)
            onConditionsMet?.Invoke();
    }

    /// <summary>
    /// Checks if a single collider satisfies a single filter condition,
    /// taking into account negation.
    /// </summary>
    private bool EvaluateCondition(Collider other, FilterCondition condition)
    {
        bool match = false;
        string targetValue = condition.value.ToLowerInvariant();

        switch (condition.conditionType)
        {
            case ConditionType.ObjectName:
                match = other.name.Equals(condition.value, StringComparison.OrdinalIgnoreCase);
                break;

            case ConditionType.Tag:
                match = other.CompareTag(condition.value);
                break;

            case ConditionType.Layer:
                match = LayerMask.LayerToName(other.gameObject.layer)
                                    .Equals(condition.value, StringComparison.OrdinalIgnoreCase);
                break;

            case ConditionType.State:
                var stateMachine = other.gameObject.GetComponent<StateMachine>();
                if (stateMachine != null)
                {
                    match = string.Equals(stateMachine.CurrentState, condition.value, StringComparison.OrdinalIgnoreCase);
                }
                break;

            case ConditionType.MaterialName:
                var renderer = other.GetComponent<Renderer>();
                if (renderer != null && renderer.sharedMaterial != null)
                {
                    // Use Contains to allow partial matching of material names
                    match = renderer.sharedMaterial.name.ToLowerInvariant().Contains(targetValue);
                }
                break;

            case ConditionType.AnimatorState:
                var animator = other.GetComponent<Animator>();
                if (animator != null)
                {
                    for (int i = 0; i < animator.layerCount; i++)
                    {
                        var stateInfo = animator.GetCurrentAnimatorStateInfo(i);
                        if (stateInfo.IsName(condition.value))
                        {
                            match = true;
                            break;
                        }
                    }
                }
                break;
        }

        // Apply negation if requested
        return condition.negate ? !match : match;
    }

    // Ensure the collider is set to trigger by default when resetting the component
    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
    }
}
