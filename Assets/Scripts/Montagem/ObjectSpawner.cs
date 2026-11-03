using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Instancia uma lista de objetos, cada um na posição correspondente
/// (pelo índice) na lista de pontos de spawn.
/// </summary>
public class ObjectSpawner : MonoBehaviour
{

    public static ObjectSpawner Instance { get; private set; }

    #region Campos Serializados

    [SerializeField] private List<Transform> LocaideSpawn;

    #endregion

    #region Ciclo de Vida (Unity)

    public void SpawnItem(GameObject item)
    {
        int pos = Random.Range(0, LocaideSpawn.Count);

        Instantiate(item, LocaideSpawn[pos]);
    }


    #endregion
}