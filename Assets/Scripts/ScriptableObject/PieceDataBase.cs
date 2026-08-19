using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Database de Peças", menuName = "Peças/ Novo Banco de Peças")]
public class PieceDataBase : ScriptableObject
{
    [Header("Todas as peças registradas no jogo")]
    [SerializeField] private List<SOPieceData> todasPecas = new List<SOPieceData>();

    // Internal Dictionary for fast string lookup
    private Dictionary<string, SOPieceData> pecaLookUp;

    private void OnEnable()
    {
        InitializeLookup();
    }

    private void InitializeLookup()
    {
        pecaLookUp = new Dictionary<string, SOPieceData>();

        foreach (var pecas in todasPecas)
        {
            if (pecas == null) continue;

            if (string.IsNullOrEmpty(pecas.ID))
            {
                Debug.LogWarning($"[Database de Peças] Asset '{pecas.name}' não tem ID!");
                continue;
            }

            if (!pecaLookUp.ContainsKey(pecas.ID))
            {
                pecaLookUp.Add(pecas.ID, pecas);
            }
            else
            {
                Debug.LogError($"[UpgradeDatabase] Duplicate upgrade ID found: '{pecas.ID}'!");
            }
        }
    }

    /// <summary>
    /// Converte um ID salvo em um asset 
    /// </summary>
    public SOPieceData GetPieceByID(string ID)
    {
        if (pecaLookUp == null || pecaLookUp.Count != todasPecas.Count)
        {
            InitializeLookup();
        }

        if (pecaLookUp.TryGetValue(ID, out SOPieceData foundUpgrade))
        {
            return foundUpgrade;
        }

        Debug.LogWarning($"[Database de Peças] ID '{ID}' não encontrado! 404");
        return null;
    }

    public List<SOPieceData> GettodasPecas() => todasPecas;

}
