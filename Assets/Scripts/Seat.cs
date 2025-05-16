using UnityEngine;

public class Seat : MonoBehaviour
{
    public Transform seatPoint;
    public string playerTag = "Player";
    public KeyCode exitKey = KeyCode.Space;
    public float DebounceDuration = 1f;

    private Transform seatedPlayer;
    // private CharacterController controller;
    private StarterAssets.FirstPersonController firstPersonController;

    private float debounceTime = 0f;

    void OnTriggerEnter(Collider other)
    {
        if (seatedPlayer != null) return;
        if (debounceTime > 0f) return;

        if (other.CompareTag(playerTag))
        {
            seatedPlayer = other.transform;

            // Zet CharacterController uit
            // controller = seatedPlayer.GetComponent<CharacterController>();
            // if (controller != null)
            // {
            //     controller.enabled = false;
            // }

            // Zet FirstPersonController uit (maakt niet uit welk script je gebruikt, als het maar MonoBehaviour is)
            firstPersonController = seatedPlayer.GetComponent<StarterAssets.FirstPersonController>();
            if (firstPersonController != null)
            {
                firstPersonController.SetMovementEnabled(false);
            }

            // Zet positie en parenting
            seatedPlayer.position = seatPoint.position;
            seatedPlayer.rotation = seatPoint.rotation;
            seatedPlayer.SetParent(seatPoint);
        }
    }

    void Update()
    {
        if (debounceTime > 0f)
        {
            debounceTime -= Time.deltaTime;
        }

        if (seatedPlayer != null && Input.GetKey(exitKey))
        {
            // Reset positie
            seatedPlayer.SetParent(null);

            // // Zet CharacterController weer aan
            // if (controller != null)
            // {
            //     controller.enabled = true;
            //     controller = null;
            // }

            // Zet FirstPersonController weer aan
            if (firstPersonController != null)
            {
                firstPersonController.SetMovementEnabled(true);
            }

            seatedPlayer = null;
            debounceTime = DebounceDuration;
        }
    }
}
