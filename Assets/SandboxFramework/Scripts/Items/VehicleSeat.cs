using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Extends Seat to add vehicle-specific functionality such as steering and throttle input handling.
/// Notifies vehicle listeners of seat and unseat events, as well as steering/throttle input.
/// </summary>
[DisallowMultipleComponent]
public class VehicleSeat : Seat
{
    // Tracks if the seat is currently occupied
    private bool isActive = false;

    /// <summary>
    /// Called when player sits on the seat.
    /// Activates the seat and resets vehicle input.
    /// </summary>
    protected override void OnSeat(GameObject player)
    {
        isActive = true;
        SendInput(0f, 0f);
    }

    /// <summary>
    /// Called when player leaves the seat.
    /// Resets vehicle input and deactivates the seat.
    /// </summary>
    protected override void OnUnseat(GameObject player)
    {
        SendInput(0f, 0f);
        isActive = false;
    }

    /// <summary>
    /// Called when this object is welded.
    /// Resets vehicle input.
    /// </summary>
    public override void OnWeld()
    {
        base.OnWeld();
        SendInput(0f, 0f);
    }

    /// <summary>
    /// Called when this object is unwelded.
    /// Resets vehicle input.
    /// </summary>
    public override void OnUnweld()
    {
        SendInput(0f, 0f);
        base.OnUnweld();
    }

    /// <summary>
    /// Notifies listeners about unseat events including vehicle-specific listeners.
    /// </summary>
    protected override void NotifyOnUnseatListeners()
    {
        if (!enabled) return;

        base.NotifyOnUnseatListeners();
        foreach (IVehicleListener vehicleListener in FindAllVehicleListeners())
        {
            vehicleListener.OnUnseat();
        }
    }

    /// <summary>
    /// Notifies listeners about seat events including vehicle-specific listeners.
    /// </summary>
    protected override void NotifyOnSeatListeners()
    {
        if (!enabled) return;

        base.NotifyOnSeatListeners();
        foreach (IVehicleListener vehicleListener in FindAllVehicleListeners())
        {
            vehicleListener.OnSeat();
        }
    }

    /// <summary>
    /// Unity Start lifecycle method.
    /// Sends initial zeroed input to ensure vehicle listeners start in a consistent state.
    /// </summary>
    private void Start()
    {
        SendInput(0f, 0f);
    }

    /// <summary>
    /// Unity Update lifecycle method.
    /// Handles input forwarding if seat is active.
    /// </summary>
    protected override void Update()
    {
        base.Update();

        if (isActive)
        {
            float steer = SandboxInput.GetAxis(InputAxis.Horizontal);
            float throttle = SandboxInput.GetAxis(InputAxis.Vertical);
            SendInput(steer, throttle);
        }
    }

    /// <summary>
    /// Sends steering and throttle input to all vehicle listeners.
    /// </summary>
    private void SendInput(float steer, float throttle)
    {
        if (!enabled) return;

        foreach (IVehicleListener listener in FindAllVehicleListeners())
        {
            listener.OnSteer(steer);
            listener.OnThrottle(throttle);
        }
    }

    /// <summary>
    /// Finds all vehicle listeners on this seat's welded structure.
    /// </summary>
    private IReadOnlyList<IVehicleListener> FindAllVehicleListeners()
    {
        return FindConnectedComponents<IVehicleListener>();
    }
}
