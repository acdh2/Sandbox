using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Destroyer : MonoBehaviour
{
    private readonly List<GameObject> objectsInside = new List<GameObject>();

    private void OnTriggerEnter(Collider other)
    {
        if (enabled)
        {
            if (!objectsInside.Contains(other.gameObject))
            {
                objectsInside.Add(other.gameObject);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (enabled)
        {
            if (objectsInside.Contains(other.gameObject))
            {
                objectsInside.Remove(other.gameObject);
            }
        }
    }
    void DestroyOverlappingItems()
    {
        StartCoroutine(DestroyOneFrameLater());
    }

    public IEnumerator DestroyOneFrameLater()
    {
        yield return null;

        foreach (GameObject obj in objectsInside)
        {
            if (obj != null)
                Destroy(obj);
        }

        objectsInside.Clear();
    }

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
    }
}
