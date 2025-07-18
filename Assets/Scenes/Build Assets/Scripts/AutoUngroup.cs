using System.Collections.Generic;
using UnityEngine;

public class AutoUngroup : MonoBehaviour
{
    void Start()
    {
        // Create a list to store all child transforms of this GameObject
        List<Transform> children = new List<Transform>();

        // Iterate over each child and add it to the list
        foreach (Transform child in transform)
        {
            children.Add(child);
        }

        // Detach each child from this GameObject
        // 'SetParent(null, true)' unparents the child and keeps its world position
        foreach (Transform child in children)
        {
            child.SetParent(null, true);
        }

        // Destroy this GameObject after ungrouping all its children
        Destroy(gameObject);
    }
}
