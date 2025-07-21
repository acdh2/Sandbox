using System;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public enum ComparisonMode
{
    SmallerThen,   // Checks if value is less than the comparison value
    GreaterThen    // Checks if value is greater than the comparison value
}

[Serializable]
public struct Condition
{
    public ComparisonMode comparisonMode; // Comparison type for this condition
    public float value;                      // The value to compare against
    public UnityEvent OnEvaluateToTrue;     // Event triggered when condition evaluates to true
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

    public Condition[] conditions; // Array of conditions to evaluate on value change

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
        // Use this GameObject's name as the variable name
        varName = gameObject.name;

        // Get Animator component from parent
        animator = GetComponentInParent<Animator>();

        // If Animator doesn't have the parameter, clear animator reference
        if (!VariableExistsInAnimator(animator, varName))
        {
            animator = null;
        }

        // Initialize the value (clamp and invoke events)
        SetValue(value);
    }

    void Update()
    {
        if (!enabled) return;

        // Accumulate deltaTime to apply changes per whole seconds
        currentTime += Time.deltaTime;

        if (currentTime >= 1f)
        {
            // Calculate whole seconds passed
            int steps = Mathf.FloorToInt(currentTime);
            currentTime -= steps;

            // Update the variable value by the accumulated amount
            SetValue(value + changePerSecond * steps);

            // Update Animator parameter if available
            if (animator != null)
            {
                animator.SetFloat(varName, value);
            }
        }
    }

    /// <summary>
    /// Changes the variable's value by a relative amount, with clamping and event triggers.
    /// </summary>
    /// <param name="amount">Amount to add (can be negative).</param>
    public void ChangeValue(float amount)
    {
        SetValue(value + amount);
    }

    /// <summary>
    /// Sets the variable to an absolute value, clamped within min and max.
    /// Triggers onValueChange and evaluates conditions if value changed.
    /// </summary>
    /// <param name="newValue">New absolute value.</param>
    public void SetValue(float newValue)
    {
        // Clamp value within allowed range
        float clampedValue = Mathf.Clamp(newValue, minValue, maxValue);

        if (value != clampedValue)
        {
            value = clampedValue;

            // Fire the value changed event with the new value
            onValueChange?.Invoke(value);

            if (enabled)
            {
                // Evaluate all conditions with the new value
                foreach (Condition condition in conditions)
                {
                    HandleCondition(condition);
                }
            }
        }
    }

    /// <summary>
    /// Checks the condition against the current value and invokes event if true.
    /// </summary>
    private void HandleCondition(Condition condition)
    {
        bool isTrue = false;
        switch (condition.comparisonMode)
        {
            case ComparisonMode.SmallerThen:
                isTrue = value < condition.value;
                break;
            case ComparisonMode.GreaterThen:
                isTrue = value > condition.value;
                break;
        }

        if (isTrue)
        {
            condition.OnEvaluateToTrue?.Invoke();
        }
    }
}
