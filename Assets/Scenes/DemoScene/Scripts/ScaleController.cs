using UnityEngine;

/// <summary>
/// Scales this object evenly on all axes. Call SetScale from a UnityEvent (e.g. Variable.onValueChange).
/// </summary>
public class ScaleController : MonoBehaviour
{
    private float lastScale = 1f;

    public void SetScale(float scale)
    {
        // Only update the transform when the scale actually changes
        if (scale != lastScale)
        {
            transform.localScale = Vector3.one * scale;
            lastScale = scale;
        }
    }
}