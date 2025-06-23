using UnityEngine;
using UnityEngine.Events;

public class VehicleSeat : Seat
{
    public UnityEvent<float> OnSteer;
    public UnityEvent<float> OnThrottle;

    private bool isActive = false;

    protected override void OnSeat(GameObject player)
    {
        isActive = true;
    }

    protected override void OnUnseat(GameObject player)
    {
        isActive = false;
    }

    protected override void Update()
    {
        base.Update();

        if (isActive)
        {
            float xAxis = Input.GetAxis("Horizontal");
            float yAxis = Input.GetAxis("Vertical");

            print(xAxis + "," + yAxis);

            OnSteer?.Invoke(xAxis);
            OnThrottle?.Invoke(yAxis);
        }
    }
}
