using System.Collections.Generic;
using UnityEngine;

public class ObjectSpawner : MonoBehaviour
{
    [SerializeField] private List<GameObject> ObjectstoSpawn;
    private int indice = 0;

    [SerializeField] private List<GameObject> PlacesToSpawn;
    private void Start()
    {
        foreach (GameObject obj in ObjectstoSpawn)
        {
            GameObject curobjt = Instantiate(obj, PlacesToSpawn[indice].transform.position, Quaternion.identity);
            indice++;
        }
    }
}
