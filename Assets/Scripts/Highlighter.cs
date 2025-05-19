using UnityEngine;

public class Highlighter : MonoBehaviour
{
    Color _startcolor;
    [SerializeField] Renderer _renderer;

    void OnMouseEnter ()
    {
        _startcolor = _renderer.material.color;
        _renderer.material.color = Color.red;
    }

    void OnMouseExit ()
    {
        _renderer.material.color = _startcolor;
    }    
}
