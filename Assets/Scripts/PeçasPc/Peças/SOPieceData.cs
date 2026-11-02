using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Peça", menuName = "Peças/ Nova Peça")]
public class SOPieceData : ScriptableObject
{
    public string ID;
    public string Nome;
    [TextArea(3, 6)]
    public string Descricao;

    public List<GameObject> prefab;
}
