using System.Collections.Generic;
using UnityEngine;

public enum WeldMode
{
    None,
    AttachableOnly,
    ReceivableOnly,
    Both
}

public class Weldable : MonoBehaviour
{
    public bool CanAttach => mode == WeldMode.AttachableOnly || mode == WeldMode.Both;
    public bool CanReceive => mode == WeldMode.ReceivableOnly || mode == WeldMode.Both;
    public WeldMode mode = WeldMode.Both;

    void Start()
    {
        Weldable[] weldables = transform.root.GetComponentsInChildren<Weldable>();
        if (weldables.Length > 1)
        {
            foreach (IWeldable weldable in transform.root.GetComponentsInChildren<IWeldable>())
            {
                weldable.OnWeld();
            }
        }
    }
}

