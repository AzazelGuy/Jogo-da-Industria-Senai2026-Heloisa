using System.Collections.Generic;
using UnityEngine;

public class ObjectSpawner : MonoBehaviour
{
    [SerializeField] private List<GameObject> ObjectstoSpawn;
    private int indice = 0;
    private void Start()
    {
        foreach (GameObject obj in ObjectstoSpawn)
        {
            GameObject curobjt = Instantiate(obj,new Vector3(transform.position.y + (10f * indice),transform.position.y,transform.position.z), Quaternion.identity);
            indice++;
        }
    }
}
