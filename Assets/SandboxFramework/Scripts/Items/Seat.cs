using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using System.Linq;
using Unity.VisualScripting;

/// <summary>
/// Allows a player to sit on a seat when entering a trigger zone.
/// Pressing a key exits the seat and restores player control.
/// </summary>
[RequireComponent(typeof(Weldable))]
[DisallowMultipleComponent]
public class Seat : MonoBehaviour, IWeldListener
{
    [Header("Seat Settings")]
    public Transform seatPoint;                 // Position and rotation where the player sits
    public string playerTag = "Player";         // Tag to identify player objects
    public float debounceDuration = 1f;         // Time after unseating before next seating allowed

    public UnityEvent onSeat;                    // Event fired when player sits
    public UnityEvent onUnseat;                  // Event fired when player leaves seat

    private Transform seatedPlayer;              // Currently seated player transform
    private Transform originalParent;            // Stored original parent to restore hierarchy
    private int originalLayer;                   // Stored original layer to restore after unseating
    private StarterAssets.FirstPersonController playerController; // Reference to player's controller script
    private float debounceTimer = 0f;            // Timer to prevent rapid reseating

    private bool isWelded;                       // Whether seat is welded (available for seating)

    // List of keys to check for input; can be customized in inspector
    public List<Key> keysToCheck = new List<Key>();

    /// <summary>
    /// Returns the GameObject of the currently seated player, or null if none.
    /// </summary>
    public GameObject GetSeatedPlayer()
    {
        return seatedPlayer?.gameObject;
    }

    public void OnAdded()
    {
    }

    public void OnRemoved()
    {
    }

    // Weld state handlers from IWeldListener interface
    public virtual void OnWeld()
    {
        isWelded = true;
    }
    public virtual void OnUnweld()
    {
        isWelded = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!enabled) return;

        if (CanSeatPlayer(other))
            SeatPlayer(other.transform);
    }

    protected IReadOnlyList<T> FindConnectedComponents<T>() where T : class
    {
        Weldable weldable = GetComponent<Weldable>();
        return Utils.FindAllInHierarchyAndConnections<T>(weldable);
        //return transform.root.GetComponentsInChildren<T>(true);
    }    

    /// <summary>
    /// Handles keyboard input to notify keypress listeners.
    /// </summary>
    private void HandleKeyboardInput()
    {
        // Gather pressed keys from the configured list
        List<Key> keysPressed = new List<Key>();
        foreach (Key key in keysToCheck)
        {
            if (Keyboard.current[key]?.wasPressedThisFrame == true)
            {
                keysPressed.Add(key);
            }
        }

        if (keysPressed.Count == 0) return;

        // Notify all IKeypressListener components in root hierarchy
        foreach (IKeypressListener keyPressListener in FindConnectedComponents<IKeypressListener>())
        {
            foreach (Key pressedKey in keysPressed)
            {
                keyPressListener.OnKeyPress(pressedKey);
            }
        }
    }

    protected virtual void Update()
    {
        if (!enabled) return;

        if (seatedPlayer != null)
            HandleKeyboardInput();

        UpdateDebounceTimer();

        if (IsExitRequested())
            ExitSeat();
    }

    /// <summary>
    /// Determines if a player can be seated based on state and tag.
    /// </summary>
    private bool CanSeatPlayer(Collider other)
    {
        if (!enabled) return false;
        return isWelded &&
               seatedPlayer == null &&
               debounceTimer <= 0f &&
               other.CompareTag(playerTag);
    }

    /// <summary>
    /// Moves player to seat point, disables movement, and stores original state.
    /// </summary>
    private void SeatPlayer(Transform player)
    {
        if (!enabled) return;

        seatedPlayer = player;
        playerController = player.GetComponent<StarterAssets.FirstPersonController>();

        // Disable player movement if controller found
        playerController?.SetMovementEnabled(false);

        // Store player's original hierarchy and layer
        originalParent = player.parent?.parent;
        originalLayer = player.gameObject.layer;

        // Snap player to seat and reparent under seatPoint
        player.SetPositionAndRotation(seatPoint.position, seatPoint.rotation);
        player.parent.SetParent(seatPoint);

        // Invoke seat events
        onSeat?.Invoke();
        OnSeat(player.gameObject);
        NotifyOnSeatListeners();
    }

    // Virtual methods for subclasses to override on seat/unseat
    protected virtual void OnSeat(GameObject player) { }
    protected virtual void OnUnseat(GameObject player) { }

    /// <summary>
    /// Notifies all listeners about unseating.
    /// </summary>
    protected virtual void NotifyOnUnseatListeners()
    {
        if (!enabled) return;
        foreach (ISeatListener seatListener in FindConnectedComponents<ISeatListener>())
        {
            seatListener.OnUnseat();
        }
    }

    /// <summary>
    /// Notifies all listeners about seating.
    /// </summary>
    protected virtual void NotifyOnSeatListeners()
    {
        if (!enabled) return;
        foreach (ISeatListener seatListener in FindConnectedComponents<ISeatListener>())
        {
            seatListener.OnSeat();
        }
    }

    /// <summary>
    /// Counts down the debounce timer to prevent rapid re-seating.
    /// </summary>
    private void UpdateDebounceTimer()
    {
        if (debounceTimer > 0f)
            debounceTimer -= Time.deltaTime;
    }

    /// <summary>
    /// Returns true if seated and exit key (Jump) is pressed.
    /// </summary>
    private bool IsExitRequested()
    {
        if (!enabled) return true;
        return seatedPlayer != null && InputSystem.GetButtonDown(InputButton.Jump);
    }

    /// <summary>
    /// Restores player control, hierarchy, and state upon exiting seat.
    /// </summary>
    private void ExitSeat()
    {
        if (seatedPlayer == null) return;

        GameObject player = seatedPlayer.gameObject;

        // Invoke unseat events
        onUnseat?.Invoke();
        OnUnseat(player);
        NotifyOnUnseatListeners();

        // Re-enable player movement
        playerController?.SetMovementEnabled(true);

        // Restore player's original hierarchy
        Transform playerTransform = seatedPlayer;
        playerTransform.parent.SetParent(originalParent);

        // Preserve forward vector or reset to upright
        Vector3 forward = playerTransform.forward.normalized;
        if (forward.sqrMagnitude > 0f)
            playerTransform.forward = forward;
        else
            playerTransform.up = Vector3.up;

        // Restore original layer
        playerTransform.gameObject.layer = originalLayer;

        // Clear seated player and reset debounce
        seatedPlayer = null;
        playerController = null;
        debounceTimer = debounceDuration;
    }
}
