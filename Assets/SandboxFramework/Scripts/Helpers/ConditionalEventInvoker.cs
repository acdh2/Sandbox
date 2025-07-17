using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ConditionalEventInvoker : MonoBehaviour
{

    [Serializable]
    public enum ContinueOption
    {
        Success,
        Fail,
        Both
    }

    [Serializable]
    public enum ConditionType
    {
        ObjectName,
        Tag,
        Layer,
        State,
        MaterialName,
        AnimatorState,
        Any
    }

    [Serializable]
    public class FilterCondition
    {
        public ConditionType conditionType;
        public string value;
        public bool negate;
    }

    public List<FilterCondition> conditions = new List<FilterCondition>();

    public enum LogicalOperator
    {
        And,
        Or
    }

    public LogicalOperator conditionLogic = LogicalOperator.And;
    public bool autoTrigger = true;
    public UnityEvent onConditionsMet;
    public UnityEvent onConditionsFailed;

    public float TimeToEvaluate = 0f; // Delay before evaluation
    private float evaluationTimer = -1f;

    // Nieuwe property voor volgorde van evaluatie
    [Tooltip("Lower values evaluate first. Higher values act like 'else' branches.")]
    public int evaluationOrder = 0;

    public ContinueOption ProceedOrderOnResult = ContinueOption.Both;

    private readonly HashSet<Collider> collidersInTrigger = new HashSet<Collider>();

    // Timer en huidige order bijhouden
    private int currentEvaluatingOrder = -1;

    private void Update()
    {
        if (evaluationTimer >= 0f)
        {
            evaluationTimer -= Time.deltaTime;
            if (evaluationTimer <= 0f)
            {
                evaluationTimer = -1f;
                EvaluateAndChain(currentEvaluatingOrder);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!enabled || other == null || other.gameObject == null)
            return;

        collidersInTrigger.Add(other);

        if (autoTrigger)
        {
            BeginEvaluation(0);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!enabled || other == null || other.gameObject == null)
            return;

        collidersInTrigger.Remove(other);

        if (autoTrigger)
        {
            BeginEvaluation(0);
        }
    }

    // Start evaluatie alleen als evaluationOrder == order
    public void BeginEvaluation(int order)
    {
        if (!enabled) return;
        
        if (evaluationOrder != order)
            return;

        currentEvaluatingOrder = order;

        if (TimeToEvaluate > 0f)
        {
            evaluationTimer = TimeToEvaluate;
        }
        else
        {
            EvaluateAndChain(order);
        }
    }

    // Evalute en indien nodig start volgende tier
    private void EvaluateAndChain(int order)
    {
        bool result = Evaluate();

        if (result)
        {
            onConditionsMet?.Invoke();
        }
        else
        {
            onConditionsFailed?.Invoke();
        }

        if (ProceedOrderOnResult == ContinueOption.Both ||
        (ProceedOrderOnResult == ContinueOption.Success && result) ||
        (ProceedOrderOnResult == ContinueOption.Fail && !result))
        {
            // Faalt, roep volgende evaluationOrder aan op alle ConditionalEventInvokers op dit GameObject
            int nextOrder = order + 1;
            var invokers = GetComponents<ConditionalEventInvoker>();
            foreach (var invoker in invokers)
            {
                if (invoker.evaluationOrder == nextOrder)
                {
                    invoker.BeginEvaluation(nextOrder);
                }
            }
        }
    }

    // Return true als condities voldaan zijn
    private bool Evaluate()
    {
        bool result = (conditionLogic == LogicalOperator.And);

        foreach (var condition in conditions)
        {
            bool anyMatch = false;

            if (condition.conditionType == ConditionType.Any)
            {
                bool hasAny = collidersInTrigger.Count > 0;
                anyMatch = condition.negate ? !hasAny : hasAny;
            }
            else
            {
                foreach (var col in collidersInTrigger)
                {
                    if (EvaluateCondition(col, condition))
                    {
                        anyMatch = true;
                        break;
                    }
                }
            }

            if (conditionLogic == LogicalOperator.And && !anyMatch)
            {
                result = false;
                break;
            }
            else if (conditionLogic == LogicalOperator.Or && anyMatch)
            {
                result = true;
                break;
            }
        }

        return result;
    }

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

        return condition.negate ? !match : match;
    }

}
