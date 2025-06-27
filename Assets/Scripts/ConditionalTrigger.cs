using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ConditionalTrigger : MonoBehaviour
{
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

    [Serializable]
    public class TriggerEvent
    {
        public List<FilterCondition> conditions = new List<FilterCondition>();
        public UnityEvent onTriggerEnter;
        public UnityEvent onTriggerExit;
    }

    public enum LogicalOperator
    {
        And,
        Or
    }

    public LogicalOperator conditionLogic = LogicalOperator.And;
    public List<TriggerEvent> triggerEvents = new List<TriggerEvent>();
 
    private void OnTriggerEnter(Collider other)
    {
        if (enabled)
        {
            EvaluateTriggerEvents(other, true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (enabled)
        {
            EvaluateTriggerEvents(other, false);
        }
    }

    private void EvaluateTriggerEvents(Collider other, bool isEnter)
    {
        if (other.gameObject == null) return;

        List<TriggerEvent> validEvents = new List<TriggerEvent>();

        // Eerst: verzamel alle triggerEvents die voldoen aan de voorwaarden
        foreach (var triggerEvent in triggerEvents)
        {
            bool result = (conditionLogic == LogicalOperator.And);

            foreach (var condition in triggerEvent.conditions)
            {
                bool conditionMet = EvaluateCondition(other, condition);

                if (conditionLogic == LogicalOperator.And)
                {
                    if (!conditionMet)
                    {
                        result = false;
                        break;
                    }
                }
                else // LogicalOperator.Or
                {
                    if (conditionMet)
                    {
                        result = true;
                        break;
                    }
                }
            }

            if (result)
            {
                validEvents.Add(triggerEvent);
            }
        }

        // Daarna: activeer alle events pas ná de evaluatie
        foreach (var evt in validEvents)
        {
            if (isEnter)
                evt.onTriggerEnter?.Invoke();
            else
                evt.onTriggerExit?.Invoke();

            //StartCoroutine(ActivateNextFrame(evt, isEnter));
        }
    }

    private IEnumerator ActivateNextFrame(TriggerEvent triggerEvent, bool isEnter)
    {
        yield return null;

        if (isEnter)
            triggerEvent.onTriggerEnter?.Invoke();
        else
            triggerEvent.onTriggerExit?.Invoke();
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
                    AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                    match = stateInfo.IsName(condition.value);
                }
                break;
        }

        return condition.negate ? !match : match;
    }

    private void Reset()
    {
        // Ensure collider is marked as trigger
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
    }
}
