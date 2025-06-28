using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using UnityEngine.UIElements;

public class VehicleSeatListener : KeyPressListener, IVehicleListener, IWeldable
{
    [System.Serializable]
    public enum WheelPosition
    {
        None,
        FrontLeft, FrontRight,
        RearLeft, RearRight,
        Left, Right, Front, Rear
    }

    [System.Serializable]
    public struct PositionalEvent
    {
        public WheelPosition position;
        public UnityEvent onWheelPosition;
    }

    [System.Serializable]
    public struct ValuePreprocessing
    {
        public bool invert;
        public bool makeAbsolute;

        public float cutoffMin;
        public float cutoffMax;

        public float mappingMin;
        public float mappingMax;

        public ValuePreprocessing(bool invert = false, bool makeAbsolute = false,
                          float cutoffMin = -1f, float cutoffMax = 1f,
                          float mappingMin = -1f, float mappingMax = 1f,
                          float scaleRearLeft = 1f, float scaleRearRight = 1f,
                          float scaleFrontLeft = 1f, float scaleFrontRight = 1f)
        {
            this.invert = invert;
            this.makeAbsolute = makeAbsolute;
            this.cutoffMin = cutoffMin;
            this.cutoffMax = cutoffMax;
            this.mappingMin = mappingMin;
            this.mappingMax = mappingMax;
        }

    }

    [Header("Steering")]
    public ValuePreprocessing steeringSettings;
    public UnityEvent<float> onSteer;


    [Header("Throttle")]
    public ValuePreprocessing throttlingSettings;
    public UnityEvent<float> onThrottle;

    public UnityEvent onSeat;

    public UnityEvent onUnseat;

    private WheelPosition wheelPosition = WheelPosition.None;

    public List<PositionalEvent> onWheelPosition = new List<PositionalEvent>();

    void Start()
    {
        OnSteer(0f);
        OnThrottle(0f);
    }

    public void OnSeat() => onSeat?.Invoke();

    public void OnUnseat() => onUnseat?.Invoke();

    private bool steerListenerEnabled = true;
    private bool throttleListenerEnabled = true;

    void Reset()
    {
        steeringSettings = new ValuePreprocessing
        {
            cutoffMin = -1f,
            cutoffMax = 1f,
            mappingMin = -1f,
            mappingMax = 1f,
        };

        throttlingSettings = new ValuePreprocessing
        {
            cutoffMin = -1f,
            cutoffMax = 1f,
            mappingMin = -1f,
            mappingMax = 1f,
        };
    }

    float PreprocessValue(float value, ValuePreprocessing settings)
    {
        if (settings.invert) value = 1f - value;
        if (settings.makeAbsolute) value = Mathf.Abs(value);

        value = Mathf.Clamp(value, settings.cutoffMin, settings.cutoffMax);

        float inputMin = settings.cutoffMin;
        float inputMax = settings.cutoffMax;
        float outputMin = settings.mappingMin;
        float outputMax = settings.mappingMax;

        float inputRange = inputMax - inputMin;
        float returnValue = 0.0f;
        if (Mathf.Approximately(inputRange, 0f))
        {
            returnValue = outputMin;
        }
        else
        {
            float t = (value - inputMin) / inputRange;
            returnValue = Mathf.Lerp(outputMin, outputMax, t);
        }

        return returnValue;
    }

    public void SetSteerListenerEnabled(bool value)
    {
        steerListenerEnabled = value;
    }

    public void SetThrottleListenerEnabled(bool value)
    {
        throttleListenerEnabled = value;
    }

    public void OnSteer(float value)
    {
        if (steerListenerEnabled)
        {
            value = PreprocessValue(value, steeringSettings);
            onSteer?.Invoke(value);
        }
    }

    public void OnThrottle(float value)
    {
        if (throttleListenerEnabled)
        {
            value = PreprocessValue(value, throttlingSettings);
            onThrottle?.Invoke(value);
        }
    }

    bool ArePositionsEquivalent(WheelPosition a, WheelPosition b)
    {
        if (a == b)
            return true;

        // Define groepen die gelijk zijn
        bool aIsFront = a == WheelPosition.FrontLeft || a == WheelPosition.FrontRight || a == WheelPosition.Front;
        bool bIsFront = b == WheelPosition.FrontLeft || b == WheelPosition.FrontRight || b == WheelPosition.Front;

        if (aIsFront && bIsFront)
            return true;

        bool aIsRear = a == WheelPosition.RearLeft || a == WheelPosition.RearRight || a == WheelPosition.Rear;
        bool bIsRear = b == WheelPosition.RearLeft || b == WheelPosition.RearRight || b == WheelPosition.Rear;

        if (aIsRear && bIsRear)
            return true;

        bool aIsLeft = a == WheelPosition.FrontLeft || a == WheelPosition.RearLeft || a == WheelPosition.Left;
        bool bIsLeft = b == WheelPosition.FrontLeft || b == WheelPosition.RearLeft || b == WheelPosition.Left;

        if (aIsLeft && bIsLeft)
            return true;

        bool aIsRight = a == WheelPosition.FrontRight || a == WheelPosition.RearRight || a == WheelPosition.Right;
        bool bIsRight = b == WheelPosition.FrontRight || b == WheelPosition.RearRight || b == WheelPosition.Right;

        if (aIsRight && bIsRight)
            return true;

        return false;
    }    

    private VehicleSeat FindClosestSeat(Transform origin)
    {
        HashSet<Transform> visited = new HashSet<Transform>();
        Queue<Transform> queue = new Queue<Transform>();
        queue.Enqueue(origin);
        visited.Add(origin);

        while (queue.Count > 0)
        {
            Transform current = queue.Dequeue();

            // Check of dit een VehicleSeat is
            VehicleSeat seat = current.GetComponent<VehicleSeat>();
            if (seat != null)
                return seat;

            // Voeg eerst de parent toe (indien nog niet bezocht)
            Transform parent = current.parent;
            if (parent != null && !visited.Contains(parent))
            {
                queue.Enqueue(parent);
                visited.Add(parent);
            }

            // Voeg daarna alle kinderen toe
            foreach (Transform child in current)
            {
                if (!visited.Contains(child))
                {
                    queue.Enqueue(child);
                    visited.Add(child);
                }
            }
        }

        return null; // Geen seat gevonden
    }

    public void OnWeld()
    {
        wheelPosition = WheelPosition.None;
        VehicleSeat seat = FindClosestSeat(transform);
        if (seat)
        {
            Vector3 offset = seat.transform.InverseTransformPoint(transform.position);

            if (offset.z >= 0)
            {
                if (offset.x >= 0)
                    wheelPosition = WheelPosition.FrontRight;
                else
                    wheelPosition = WheelPosition.FrontLeft;
            }
            else
            {
                if (offset.x >= 0)
                    wheelPosition = WheelPosition.RearRight;
                else
                    wheelPosition = WheelPosition.RearLeft;
            }
        }

        foreach (PositionalEvent positionalEvent in onWheelPosition)
        {
            if (ArePositionsEquivalent(positionalEvent.position, wheelPosition))
            {
                positionalEvent.onWheelPosition?.Invoke();
            }
        }
    }

    public void OnUnweld()
    {
        wheelPosition = WheelPosition.None;
    }
}

