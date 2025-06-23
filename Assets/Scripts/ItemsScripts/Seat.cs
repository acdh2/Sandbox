using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Allows a player to sit on a seat when entering a trigger zone.
/// Pressing a key exits the seat and restores player control.
/// </summary>
public class Seat : MonoBehaviour
{
    [Header("Seat Settings")]
    public Transform seatPoint;
    public string playerTag = "Player";
    public KeyCode exitKey = KeyCode.Space;
    public float debounceDuration = 1f;

    public UnityEvent OnSeatedPlayer;
    public UnityEvent OnUnseatedPlayer;

    private Transform seatedPlayer;
    private Transform originalParent;
    private int originalLayer;
    private StarterAssets.FirstPersonController playerController;
    private float debounceTimer = 0f;

    public GameObject GetSeatedPlayer()
    {
        return seatedPlayer.gameObject;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!CanSeatPlayer(other)) return;

        SeatPlayer(other.transform);
    }

    protected virtual void Update()
    {
        UpdateDebounceTimer();

        if (IsExitRequested())
            ExitSeat();
    }

    /// <summary>
    /// Checks whether the collider belongs to a player that can sit.
    /// </summary>
    private bool CanSeatPlayer(Collider other)
    {
        return seatedPlayer == null &&
               debounceTimer <= 0f &&
               other.CompareTag(playerTag);
    }

    /// <summary>
    /// Seats the player by repositioning and disabling movement.
    /// </summary>
    private void SeatPlayer(Transform player)
    {
        seatedPlayer = player;
        playerController = player.GetComponent<StarterAssets.FirstPersonController>();

        // Disable player controls if available
        if (playerController != null)
            playerController.SetMovementEnabled(false);

        // Store hierarchy and layer for later restoration
        originalParent = player.parent?.parent;
        originalLayer = player.gameObject.layer;

        // Snap player to seat position and attach to seat
        player.SetPositionAndRotation(seatPoint.position, seatPoint.rotation);
        player.parent.SetParent(seatPoint);

        OnSeatedPlayer?.Invoke();
        OnSeat(player.gameObject);
    }

    protected virtual void OnSeat(GameObject player)
    {
    }
    protected virtual void OnUnseat(GameObject player)
    {
    }

    /// <summary>
    /// Updates the debounce cooldown timer.
    /// </summary>
    private void UpdateDebounceTimer()
    {
        if (debounceTimer > 0f)
            debounceTimer -= Time.deltaTime;
    }

    /// <summary>
    /// Returns true if seated and the exit key is pressed.
    /// </summary>
    private bool IsExitRequested()
    {
        return seatedPlayer != null && Input.GetKeyDown(exitKey);
    }

    /// <summary>
    /// Restores player control and hierarchy after standing up.
    /// </summary>
    private void ExitSeat()
    {
        // Reactivate controls
        if (playerController != null)
            playerController.SetMovementEnabled(true);

        // Restore hierarchy and orientation
        Transform playerTransform = seatedPlayer;
        playerTransform.parent.SetParent(originalParent);

        // Ensure the forward vector is preserved or fallback to upright
        Vector3 forward = playerTransform.forward.normalized;
        if (forward.magnitude > 0f)
            playerTransform.forward = forward;
        else
            playerTransform.up = Vector3.up;

        // Restore original layer
        playerTransform.gameObject.layer = originalLayer;

        GameObject player = seatedPlayer.gameObject;

        // Clear state and start debounce
        seatedPlayer = null;
        playerController = null;
        debounceTimer = debounceDuration;

        OnUnseatedPlayer?.Invoke();
        OnUnseat(player);
    }
}
