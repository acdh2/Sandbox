using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
public enum InputAxis
{
    Horizontal,
    Vertical
}

[System.Serializable]
public enum InputButton
{
    Weld,
    Unweld,
    Rotate1,
    Rotate2,
    Jump
}

[DisallowMultipleComponent]
public class InputSystem : MonoBehaviour
{

    const float axesCutOffValue = 0.1f;
    const float axesSmoothingFactor = 0.1f;

    private static PlayerInputActions input;
    private static Dictionary<InputButton, InputAction> inputMap = new();

    private static float horizontalAxis = 0f;
    private static float verticalAxis = 0f;

    public void Awake()
    {
        input = new PlayerInputActions();
        input.Enable();
        inputMap = new(){
            { InputButton.Weld, input.Default.Weld },
            { InputButton.Unweld, input.Default.Unweld },
            { InputButton.Rotate1, input.Default.Rotate1 },
            { InputButton.Rotate2, input.Default.Rotate2 },
            { InputButton.Jump, input.Default.Jump },
        };
    }

    void Update()
    {
        float rawHorizontalAxis = input.Default.VehicleAxisHorizontal.ReadValue<float>();
        float rawVerticalAxis = input.Default.VehicleAxisVertical.ReadValue<float>();
        horizontalAxis = Mathf.Lerp(horizontalAxis, rawHorizontalAxis, axesSmoothingFactor);
        verticalAxis = Mathf.Lerp(verticalAxis, rawVerticalAxis, axesSmoothingFactor);
        if (Mathf.Abs(horizontalAxis) < axesCutOffValue) horizontalAxis = 0f;
        if (Mathf.Abs(verticalAxis) < axesCutOffValue) verticalAxis = 0f;
    }

    public static bool GetButtonDown(InputButton button)
    {
        if (input == null) return false;
        if (inputMap.TryGetValue(button, out var action))
            return action.WasPressedThisFrame();
        return false;
    }

    public static Vector2 GetPointerPosition()
    {
        if (input == null) return Vector2.zero;
        return input.Default.PointerPosition.ReadValue<Vector2>();
    }

    public static bool GetPointerDown()
    {
        if (input == null) return false;
        return input.Default.PointerPress.WasPressedThisFrame();
    }

    public static bool GetPointerUp()
    {
        if (input == null) return false;
        return input.Default.PointerPress.WasReleasedThisFrame();
    }

    public static bool GetPointerHeld()
    {
        if (input == null) return false;
        return input.Default.PointerPress.IsPressed();
    }

    public static float GetAxis(InputAxis axis)
    {
        if (input == null) return 0f;
        switch (axis)
        {
            case InputAxis.Horizontal:
                return horizontalAxis;

            case InputAxis.Vertical:
                return verticalAxis;
        }
        return 0f;
    }

}
