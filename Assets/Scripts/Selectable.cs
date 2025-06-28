using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using UnityEngine;

public class Selectable : MonoBehaviour
{
    [SerializeField]
    private string objectDescription = "";

    [SerializeField]
    private bool isDraggable = false;

    public string ObjectDescription => objectDescription;
    public bool IsDraggable => isDraggable;
}