using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class VehicleSeat : Seat, IWeldListener
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

    public override void OnWeld()
    {
        base.OnWeld();
        SendData(0f, 0f);
        //AddRigidbody(transform.root);
    }
    public override void OnUnweld()
    {
        SendData(0f, 0f);
        base.OnUnweld();
        //RemoveRigidbody(transform.root);
    }

    void AddRigidbody(Transform transform)
    {
        transform.gameObject.AddComponent<Rigidbody>();
    }

    void RemoveRigidbody(Transform transform)
    {
        Rigidbody rb = transform.gameObject.GetComponent<Rigidbody>();
        if (rb)
        {
            Destroy(rb);
        }
    }

    protected override void NotifyOnUnseatListeners()
    {
        base.NotifyOnSeatListeners();
        foreach (IVehicleListener vehicleListener in transform.root.GetComponentsInChildren<IVehicleListener>(true))
        {
            vehicleListener.OnUnseat();
        }
    }

    protected override void NotifyOnSeatListeners()
    {
        base.NotifyOnSeatListeners();
        foreach (IVehicleListener vehicleListener in transform.root.GetComponentsInChildren<IVehicleListener>(true))
        {
            vehicleListener.OnSeat();
        }
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
