using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ConditionalEvent : MonoBehaviour
{
    [System.Serializable]
    public enum ConditionType
    {
        ObjectName,
        Tag,
        Layer,
        State,
        MaterialName,
        AnimatorState
    }

    [Serializable]
    public class FilterCondition
    {
        public ConditionType conditionType;
        public string value;
        public bool negate;
    }

    public List<FilterCondition> conditions = new List<FilterCondition>();

    [System.Serializable]
    public enum LogicalOperator
    {
        And,
        Or
    }

    public LogicalOperator conditionLogic = LogicalOperator.And;

    public bool autoTrigger = true;

    public UnityEvent onConditionsMet;

    private HashSet<Collider> collidersInTrigger = new HashSet<Collider>();

    private void OnTriggerEnter(Collider other)
    {
        if (!enabled || other.gameObject == null)
            return;

        collidersInTrigger.Add(other);

        if (autoTrigger)
        {
            Evaluate();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!enabled || other.gameObject == null)
            return;

        collidersInTrigger.Remove(other);

        if (autoTrigger)
        {
            Evaluate();
        }
    }

    private void Evaluate()
    {
        bool result = (conditionLogic == LogicalOperator.And) ? true : false;

        foreach (var condition in conditions)
        {
            bool anyMatch = false;

            foreach (var col in collidersInTrigger)
            {
                if (EvaluateCondition(col, condition))
                {
                    anyMatch = true;
                    break;
                }
            }

            if (conditionLogic == LogicalOperator.And)
            {
                if (!anyMatch)
                {
                    result = false;
                    break;
                }
            }
            else // LogicalOperator.Or
            {
                if (anyMatch)
                {
                    result = true;
                    break;
                }
            }
        }

        if (result)
        {
            onConditionsMet?.Invoke();
        }
    }

    private bool EvaluateCondition(Collider other, FilterCondition condition)
    {
        bool match = false;
        string targetValue = condition.value.ToLowerInvariant();

        switch (condition.conditionType)
        {
            case ConditionType.ObjectName:
                match = other.name.ToLowerInvariant() == targetValue;
                break;

            case ConditionType.Tag:
                match = string.Equals(other.tag, condition.value, StringComparison.OrdinalIgnoreCase);
                break;

            case ConditionType.Layer:
                match = LayerMask.LayerToName(other.gameObject.layer).ToLowerInvariant() == targetValue;
                break;

            case ConditionType.State:
                StateMachine stateMachine = other.gameObject.GetComponent<StateMachine>();
                if (stateMachine != null)
                {
                    match = string.Equals(stateMachine.CurrentState, condition.value, StringComparison.OrdinalIgnoreCase);
                }
                break;

            case ConditionType.MaterialName:
                Renderer renderer = other.GetComponent<Renderer>();
                if (renderer != null && renderer.sharedMaterial != null)
                {
                    match = renderer.sharedMaterial.name.ToLowerInvariant().Contains(targetValue);
                }
                break;

            case ConditionType.AnimatorState:
                Animator animator = other.GetComponent<Animator>();
                if (animator != null)
                {
                    for (int i = 0; i < animator.layerCount; i++)
                    {
                        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(i);
                        if (stateInfo.IsName(condition.value))
                        {
                            match = true;
                            break;
                        }
                    }
                }
                break;
        }

        return condition.negate ? !match : match;
    }

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
    }
}
