using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class VehicleSeat : Seat, IWeldable
{
    private bool isActive = false;

    protected override void OnSeat(GameObject player)
    {
        isActive = true;
        SendData(0f, 0f);
    }

    protected override void OnUnseat(GameObject player)
    {
        SendData(0f, 0f);
        isActive = false;
    }

    public void OnWeld()
    {
        SendData(0f, 0f);
    }
    public void OnUnweld()
    {
        SendData(0f, 0f);
    }

    void Start()
    {
        SendData(0f, 0f);
    }

    protected override void Update()
    {
        base.Update();

        if (isActive)
        {
            float xAxis = Input.GetAxis("Horizontal");
            float yAxis = Input.GetAxis("Vertical");
            
            SendData(xAxis, yAxis);
        }
    }

    private void SendData(float steer, float throttle)
    {
        foreach (IVehicleListener vehicleListener in FindAllVehicleListeners())
        {
            print(steer + "," + throttle);

            vehicleListener.OnSteer(steer);
            vehicleListener.OnThrottle(throttle);
        }
    }

    private List<IVehicleListener> FindAllVehicleListeners()
    {
        Transform root = transform.root;
        return new List<IVehicleListener>(root.GetComponentsInChildren<IVehicleListener>(true));
    }

}
