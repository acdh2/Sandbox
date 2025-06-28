using UnityEngine;

public class KeepUpright : MonoBehaviour, ISeatListener
{

    void LateUpdate()
    {
        if (enabled)
        {
            transform.up = Vector3.up;
        }
    }

    public void OnSeat()
    {
        enabled = false;
    }

    public void OnUnseat()
    {
        enabled = true;
    }
}
