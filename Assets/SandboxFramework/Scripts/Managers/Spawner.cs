using UnityEngine;

[DisallowMultipleComponent]
public class Spawner : MonoBehaviour
{

    public Transform spawnLocation;
    // Prefab om te spawnen
    public GameObject[] prefabs;

    // Schaalfactor voor het nieuwe object
    public float scale = 1f;

    void Start()
    {
        if (spawnLocation == null)
        {
            spawnLocation = transform;
        }
    }

    // Methode om het prefab te spawnen
    public void Spawn()
    {
        if (enabled)
        {
            if (spawnLocation == null)
            {
                spawnLocation = transform;
            }

            GameObject prefab;
            
            if (prefabs.Length > 0)
            {
                prefab = prefabs[Random.Range(0, prefabs.Length)];
                if (prefab == null)
                {
                    Debug.LogWarning("Prefab is niet ingesteld!");
                    return;
                }
            }
            else
            {
                Debug.LogWarning("Prefabs zijn niet ingesteld");
                return;
            }

            // Instantiate het prefab op dezelfde positie en rotatie als dit object
                GameObject instance = Instantiate(prefab, spawnLocation.position, spawnLocation.rotation);

            // Pas de schaal aan ten opzichte van de originele prefab schaal
            instance.transform.localScale = prefab.transform.localScale * scale;

            instance.name = prefab.name;
        }
    }
}
