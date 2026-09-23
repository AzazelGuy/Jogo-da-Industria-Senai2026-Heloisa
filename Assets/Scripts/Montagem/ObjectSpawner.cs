using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Instancia uma lista de objetos, cada um na posição correspondente
/// (pelo índice) na lista de pontos de spawn.
/// </summary>
public class ObjectSpawner : MonoBehaviour
{
    #region Campos Serializados

    [SerializeField] private List<GameObject> ObjectstoSpawn;
    [SerializeField] private List<GameObject> PlacesToSpawn;

    #endregion

    #region Estado Interno

    private int indice = 0;

    #endregion

    #region Ciclo de Vida (Unity)

    private void Start()
    {
        // ATENÇÃO: assume que ObjectstoSpawn e PlacesToSpawn têm o mesmo tamanho;
        // se PlacesToSpawn tiver menos elementos, isso lançará um IndexOutOfRangeException.
        foreach (GameObject obj in ObjectstoSpawn)
        {
            GameObject curobjt = Instantiate(obj, PlacesToSpawn[indice].transform.position, Quaternion.identity);
            indice++;
        }
    }

    #endregion
}
