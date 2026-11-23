using UnityEngine;

/// <summary>
/// Keeps this object standing upright: it may turn left/right (Y axis),
/// but tilting forward/backward or sideways is undone every frame.
/// Only applies when the object has no parent (e.g. is not welded to something).
/// </summary>
[DisallowMultipleComponent]
public class KeepUpright : MonoBehaviour
{
    void LateUpdate()
    {
        if (transform.parent == null)
        {
            float rotationY = transform.eulerAngles.y;
            transform.rotation = Quaternion.Euler(0f, rotationY, 0f);
        }
    }
}
