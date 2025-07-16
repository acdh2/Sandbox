using System;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public enum ComparisionMode
{
    SmallerThen,
    GreaterThen
}

[Serializable]
public struct Condition
{
    public ComparisionMode comparisionMode;
    public float value;
    public UnityEvent OnEvaluateToTrue;
}

/// <summary>
/// Manages a specific variable.
/// This script should be placed on a child GameObject of the main Object.
/// The GameObject's name will be used as the identifier for this variable and its Animator parameter.
/// </summary>
public class Variable : MonoBehaviour
{
    [Header("Variable Settings")]
    [Tooltip("The current value of this variable.")]
    public float value;

    [Tooltip("The minimum allowed value for this variable.")]
    public float minValue = 0f;

    [Tooltip("The maximum allowed value for this variable.")]
    public float maxValue = 100f;

    [Tooltip("The amount this variable changes per second (e.g., -1 for hunger, 0.5 for energy).")]
    public float changePerSecond = 0f;

    [Header("UI & Event Coupling")]
    [Tooltip("Event triggered when the PetVar's value changes, passing the new value.")]
    public UnityEvent<float> onValueChange;

    public Condition[] conditions;

    private Animator animator = null;
    private string varName = "";

    private float currentTime = 0f;

    public void SetChangePerSecond(float value)
    {
        changePerSecond = value;
    }

    bool VariableExistsInAnimator(Animator animator, string varName)
    {
        if (animator == null) return false;

        bool parameterWasFound = false;
        for (var i = 0; i < animator.parameterCount; i++)
        {
            var parameter = animator.GetParameter(i);
            if (parameter.name == varName)
            {
                parameterWasFound = true;
            }
        }
        return parameterWasFound;
    }

    void Awake()
    {
        varName = gameObject.name;

        animator = GetComponentInParent<Animator>();
        if (!VariableExistsInAnimator(animator, varName))
        {
            animator = null;
        }

        SetValue(value);
    }

    void Update()
    {
        currentTime += Time.deltaTime;

        if (currentTime >= 1f)
        {
            int steps = Mathf.FloorToInt(currentTime);
            currentTime -= steps;

            SetValue(value + changePerSecond * steps);

            if (animator != null)
            {
                animator.SetFloat(varName, value);
            }
        }
    }

    /// <summary>
    /// Changes the PetVar's value by a relative amount.
    /// The value will be clamped between minValue and maxValue.
    /// </summary>
    /// <param name="amount">The amount to add to the current value (can be positive or negative).</param>
    public void ChangeValue(float amount)
    {
        SetValue(value + amount); // Re-use SetValue for clamping and event triggering
    }

    /// <summary>
    /// Sets the PetVar's value to an absolute new value.
    /// The value will be clamped between minValue and maxValue.
    /// </summary>
    /// <param name="newValue">The absolute new value to set.</param>
    public void SetValue(float newValue)
    {
        float clampedValue = Mathf.Clamp(newValue, minValue, maxValue);

        if (value != clampedValue)
        {
            value = clampedValue;
            onValueChange?.Invoke(value);
            value = Mathf.FloorToInt(clampedValue);

            foreach (Condition condition in conditions)
            {
                HandleCondition(condition);
            }
        }
    }

    private void HandleCondition(Condition condition)
    {
        bool isTrue = false;
        switch (condition.comparisionMode)
        {
            case ComparisionMode.SmallerThen:
                isTrue = value < condition.value;
                break;
            case ComparisionMode.GreaterThen:
                isTrue = value > condition.value;
                break;
        }
        if (isTrue)
        {
            condition.OnEvaluateToTrue?.Invoke();
        }
    }
}