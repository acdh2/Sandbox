using UnityEngine;
using UnityEngine.UI;

public class CrossHairPositioning : MonoBehaviour
{
    RectTransform rectTransform;
    Canvas canvas;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }

    void Update()
    {
        // if (Cursor.lockState == CursorLockMode.Locked)
        // {
        //     // Muis is gelocked: zet crosshair in het midden
        //     rectTransform.anchoredPosition = Vector2.zero;
        // }
        // else
        // {
            // Muis is vrij: volg de muispositie
            Vector2 mousePos;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                Input.mousePosition,
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                out mousePos))
            {
                rectTransform.anchoredPosition = mousePos;
            }
        // }
    }
}
