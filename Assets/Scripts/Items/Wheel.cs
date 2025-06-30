using UnityEngine;

public class Wheel : MonoBehaviour
{
    private WheelCollider wheelCollider;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        wheelCollider = GetComponentInParent<WheelCollider>();
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (wheelCollider.enabled)
        {
            Vector3 position;
            Quaternion orientation;
            wheelCollider.GetWorldPose(out position, out orientation);
            transform.rotation = orientation;
            transform.position = position;
        }
    }
}
