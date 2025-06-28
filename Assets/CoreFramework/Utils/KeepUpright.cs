using UnityEngine;

public class KeepUpright : MonoBehaviour
{
    public Transform playerCapsule;

    void LateUpdate()
    {
        if (transform.parent == null)
        {
            transform.rotation = Quaternion.identity;
            float rotationY = playerCapsule.transform.rotation.eulerAngles.y;
            playerCapsule.transform.rotation = Quaternion.Euler(0, rotationY, 0);
        }
    }
}
