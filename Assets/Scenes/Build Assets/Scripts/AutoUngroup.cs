using System.Collections.Generic;
using UnityEngine;

public class AutoUngroup : MonoBehaviour
{
    void Start()
    {
        List<Transform> children = new List<Transform>();
        foreach (Transform child in transform)
        {
            children.Add(child);
        }

        foreach (Transform child in children)
        {
            child.SetParent(null, true);
        }

        Destroy(gameObject);
    }

}
