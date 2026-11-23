using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public enum ComparisonModeInt
{
    SmallerThan,   // Checks if value is less than the comparison value
    GreaterThan,   // Checks if value is greater than the comparison value
    Equals,             // Checks if value is exactly equal to the comparison value
    SmallerThanOrEqual, // Checks if value is less than or equal to the comparison value
    GreaterThanOrEqual  // Checks if value is greater than or equal to the comparison value
}

[Serializable]
public struct ConditionInt
{
    public ComparisonModeInt comparisonMode;  // Comparison type for this condition
    public int value;                         // The value to compare against

    [FormerlySerializedAs("OnEvaluateToTrue")]
    public UnityEvent onEvaluateToTrue;       // Event triggered when condition evaluates to true

    [FormerlySerializedAs("OnEvaluateToFalse")]
    public UnityEvent onEvaluateToFalse;      // Event triggered when condition evaluates to false
}

[Serializable]
public struct ScriptCondition
{
    public ComparisonModeInt comparisonMode;  // Comparison type for this condition

    [FormerlySerializedAs("OnEvaluateToTrue")]
    public UnityEvent onEvaluateToTrue;       // Event triggered when condition evaluates to true

    [FormerlySerializedAs("OnEvaluateToFalse")]
    public UnityEvent onEvaluateToFalse;      // Event triggered when condition evaluates to false
}

/// <summary>
/// Stores an integer value and lets you set it manually or randomly.
/// Can evaluate conditions and fire UnityEvents when they become true.
/// </summary>
public class RandomValue : MonoBehaviour
{
    [Header("Value Settings")]
    public int value = 0;

    public int minValue = 0;
    public int maxValue = 10;

    [Header("Startup Settings")]
    [Tooltip("If true, sets a random value on Start.")]
    public bool randomOnStart = false;

    [Header("Auto Random Settings")]
    [Tooltip("If true, will automatically pick a new random value every intervalSeconds.")]
    public bool autoRandom = false;
    public float intervalSeconds = 1f;

    [Header("Events & Conditions")]
    [Tooltip("Called whenever a value is set (also when it is the same as before).")]
    public UnityEvent<int> onValueChange;

    [Tooltip("Conditions that are evaluated when the value changes, or manually via EvaluateConditions.")]
    public ConditionInt[] conditions;

    [Tooltip("Conditions that are evaluated when the RunScriptCondition method is called")]
    public ScriptCondition[] scriptConditions;

    private float timer = 0f;

    void Start()
    {
        if (randomOnStart)
        {
            SetRandomValue();
        }
        else
        {
            SetValue(value);
        }
    }

    void Update()
    {
        if (!autoRandom) return;

        timer += Time.deltaTime;
        if (timer >= intervalSeconds)
        {
            timer -= intervalSeconds;
            SetRandomValue();
        }
    }

    /// <summary>
    /// Sets the value to an exact number and evaluates conditions.
    /// </summary>
    public void SetValue(int newValue)
    {
        newValue = Mathf.Clamp(newValue, minValue, maxValue);

        value = newValue;
        onValueChange?.Invoke(value);

        EvaluateConditions();
    }

    /// <summary>
    /// Sets the value to a random number between min and max (inclusive).
    /// </summary>
    public void SetRandomValue()
    {
        int newValue = UnityEngine.Random.Range(minValue, maxValue + 1);
        SetValue(newValue);
    }

    /// <summary>
    /// Evaluates all conditions against the current value.
    /// Can be called manually (as UnityEvent) to re-fire events even if the value hasn't changed.
    /// </summary>
    public void EvaluateConditions()
    {
        foreach (var condition in conditions)
        {
            if (EvaluateCondition(condition.comparisonMode, condition.value, value))
            {
                condition.onEvaluateToTrue?.Invoke();
            }
            else
            {
                condition.onEvaluateToFalse?.Invoke();
            }
        }
    }

    /// <summary>
    /// Returns true if testValue compares to compareValue according to comparisonMode.
    /// Example: SmallerThan with compareValue 5 is true for testValue 3.
    /// </summary>
    private bool EvaluateCondition(ComparisonModeInt comparisonMode, int compareValue, int testValue)
    {
        switch (comparisonMode)
        {
            case ComparisonModeInt.SmallerThan:
                return testValue < compareValue;
            case ComparisonModeInt.GreaterThan:
                return testValue > compareValue;
            case ComparisonModeInt.Equals:
                return testValue == compareValue;
            case ComparisonModeInt.SmallerThanOrEqual:
                return testValue <= compareValue;
            case ComparisonModeInt.GreaterThanOrEqual:
                return testValue >= compareValue;
            default:
                return false;
        }
    }

    /// <summary>
    /// Compares the current value against valueToTest using the scriptConditions
    /// and fires their events. Handy to call from a UnityEvent.
    /// </summary>
    public void RunScriptCondition(int valueToTest)
    {
        foreach (var condition in scriptConditions)
        {
            if (EvaluateCondition(condition.comparisonMode, valueToTest, value))
            {
                condition.onEvaluateToTrue?.Invoke();
            }
            else
            {
                condition.onEvaluateToFalse?.Invoke();
            }
        }
    }
}
