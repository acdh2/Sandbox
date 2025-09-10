using System;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public enum ComparisonMode
{
    SmallerThan,   // Checks if value is less than the comparison value
    GreaterThan    // Checks if value is greater than the comparison value
}

[Serializable]
public struct Condition
{
    public ComparisonMode comparisonMode;  // Comparison type for this condition
    public float value;                    // The value to compare against
    public UnityEvent OnEvaluateToTrue;    // Event triggered when condition evaluates to true
}

/// <summary>
/// Manages a single variable associated with this GameObject.
/// This script should be attached to a child GameObject of the main object.
/// The GameObject's name is used as an identifier for this variable and also as
/// the name of the corresponding Animator parameter to synchronize.
/// </summary>
public class Variable : MonoBehaviour
{
    [Header("Variable Settings")]
    [Tooltip("The current value of this variable.")]
    public float value;

    [Tooltip("Minimum allowed value for this variable.")]
    public float minValue = 0f;

    [Tooltip("Maximum allowed value for this variable.")]
    public float maxValue = 100f;

    [Tooltip("Rate of change of this variable per second (positive or negative).")]
    public float changePerSecond = 0f;

    [Header("UI & Event Coupling")]
    [Tooltip("Event fired whenever the variable's value changes, passing the new value.")]
    public UnityEvent<float> onValueChange;

    [Tooltip("Conditions that are evaluated whenever the value changes.")]
    public Condition[] conditions;

    private Animator animator = null;   // Reference to parent Animator component
    private string varName = "";        // The variable name, taken from GameObject name
    private float currentTime = 0f;     // Accumulates deltaTime to apply value changes per second

    /// <summary>
    /// Sets the rate of change per second for this variable.
    /// </summary>
    public void SetChangePerSecond(float value)
    {
        changePerSecond = value;
    }

    /// <summary>
    /// Checks if the animator has a parameter matching the variable name.
    /// </summary>
    bool VariableExistsInAnimator(Animator animator, string varName)
    {
        if (animator == null) return false;

        for (var i = 0; i < animator.parameterCount; i++)
        {
            var parameter = animator.GetParameter(i);
            if (parameter.name == varName && parameter.type == AnimatorControllerParameterType.Float)
            {
                return true;
            }
        }
        return false;
    }

    void Awake()
    {
        varName = gameObject.name;
        animator = GetComponentInParent<Animator>();

        if (!VariableExistsInAnimator(animator, varName))
        {
            animator = null;
        }

        // Initialize the value
        SetValue(value);
    }

    void Update()
    {
        if (!enabled) return;

        currentTime += Time.deltaTime;

        if (currentTime >= 1f)
        {
            int steps = Mathf.FloorToInt(currentTime);
            currentTime -= steps;

            SetValue(value + changePerSecond * steps);
        }
    }

    /// <summary>
    /// Changes the variable's value by a relative amount, with clamping and event triggers.
    /// </summary>
    public void ChangeValue(float amount)
    {
        SetValue(value + amount);
    }

    /// <summary>
    /// Sets the variable to an absolute value, clamped within min and max.
    /// Triggers onValueChange and evaluates conditions if value changed.
    /// </summary>
    public void SetValue(float newValue)
    {
        float clampedValue = Mathf.Clamp(newValue, minValue, maxValue);

        if (value != clampedValue)
        {
            float oldValue = value;
            value = clampedValue;

            // Always sync animator
            if (animator != null)
            {
                animator.SetFloat(varName, value);
            }

            // Fire value changed event
            onValueChange?.Invoke(value);

            if (enabled)
            {
                foreach (Condition condition in conditions)
                {
                    bool wasTrue = EvaluateCondition(condition, oldValue);
                    bool isTrue  = EvaluateCondition(condition, value);

                    // Edge-trigger: only invoke when transitioning from false → true
                    if (!wasTrue && isTrue)
                    {
                        condition.OnEvaluateToTrue?.Invoke();
                    }
                }
            }
        }
    }

    /// <summary>
    /// Evaluates a condition against a given value.
    /// </summary>
    private bool EvaluateCondition(Condition condition, float testValue)
    {
        switch (condition.comparisonMode)
        {
            case ComparisonMode.SmallerThan:
                return testValue < condition.value;
            case ComparisonMode.GreaterThan:
                return testValue > condition.value;
            default:
                return false;
        }
    }
}
