using UnityEngine;
using Unity.Cinemachine;

public class TeleportationBehaviour : MonoBehaviour
{
    public CinemachineCamera cinemachineVirtualCamera;
    public CharacterController characterController;

    private Transform teleportationTarget = null;

    void Start()
    {
        if (characterController == null)
        {
            Debug.LogWarning("No character controller assigned to TeleportBehaviour.");
            Destroy(this);
        }
    }

    void LateUpdate() 
    {
        if (teleportationTarget != null) {
            TeleportImmediatelyTo(teleportationTarget);
            teleportationTarget = null;
        }
    }

    public void TeleportTo(Transform targetTransform)
    {
        teleportationTarget = targetTransform;
    }

    private void TeleportImmediatelyTo(Transform targetTransform)
    {
        if (characterController != null)
        {
            var currentCameraPosition = Vector3.zero;
            if (cinemachineVirtualCamera != null && cinemachineVirtualCamera.Follow != null)
            {
                currentCameraPosition = cinemachineVirtualCamera.Follow.transform.position;
            }

            characterController.enabled = false;
            characterController.transform.position = targetTransform.position;
            characterController.transform.rotation = targetTransform.rotation;
            characterController.enabled = true;

            if (cinemachineVirtualCamera != null && cinemachineVirtualCamera.Follow != null)
            {
                cinemachineVirtualCamera.OnTargetObjectWarped(
                    cinemachineVirtualCamera.Follow,
                    cinemachineVirtualCamera.Follow.transform.position - currentCameraPosition
                );
            }
        }
        // else
        // {
        //     // Als er geen CharacterController is, gewoon positioneren
        //     transform.position = targetTransform.position;
        //     transform.rotation = targetTransform.rotation;
        // }
    }
}
