using UnityEngine;
using UnityEngine.Events;

public class VehicleListener : MonoBehaviour, IVehicleListener, ISeatListener
{
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
                          float mappingMin = -1f, float mappingMax = 1f)
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

    public void OnSeat() => onSeat?.Invoke();

    public void OnUnseat() => onUnseat?.Invoke();

    void Reset()
    {
        steeringSettings = new ValuePreprocessing
        {
            cutoffMin = -1f,
            cutoffMax = 1f,
            mappingMin = -1f,
            mappingMax = 1f
        };

        throttlingSettings = new ValuePreprocessing
        {
            cutoffMin = -1f,
            cutoffMax = 1f,
            mappingMin = -1f,
            mappingMax = 1f
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
        if (Mathf.Approximately(inputRange, 0f))
            return outputMin;

        float t = (value - inputMin) / inputRange;
        return Mathf.Lerp(outputMin, outputMax, t);
    }

    public void OnSteer(float value)
    {
        value = PreprocessValue(value, steeringSettings);
        onSteer?.Invoke(value);
    }

    public void OnThrottle(float value)
    {
        value = PreprocessValue(value, throttlingSettings);
        onThrottle?.Invoke(value);
    }
}

