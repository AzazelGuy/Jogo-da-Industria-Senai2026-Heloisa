using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class StepPieceInfo
{
    [Tooltip("ID da peça cadastrado na SOPieceData ou myID do ComponenteBaseMontagem.")]
    public string pieceID;

    [Tooltip("Descrição opcional ou nome amigável para exibição em UI.")]
    public string pieceName;

    [Tooltip("Indica se esta peça é obrigatória para considerar o computador totalmente montado.")]
    public bool isRequired = true;
}

public class StepChecker : MonoBehaviour
{
    public static StepChecker Instance { get; private set; }

    [Header("Configurações de Requisitos de Peças")]
    [Tooltip("Lista com as peças que fazem parte da montagem (a ordem na lista não impede a montagem em ordens diferentes).")]
    [SerializeField] private List<StepPieceInfo> pecasRequeridas = new List<StepPieceInfo>();

    [Header("Registro de Progresso")]
    [Tooltip("Lista de componentes que já foram posicionados na bancada/gabinete.")]
    [SerializeField] private List<ComponenteBaseMontagem> pecasColocadas = new List<ComponenteBaseMontagem>();

    // Eventos para atualização de UI / Lógica de Jogo
    public event Action<ComponenteBaseMontagem> OnPiecePlaced;
    public static event Action OnTudoFeito;

    public IReadOnlyList<ComponenteBaseMontagem> PecasColocadas => pecasColocadas;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }


    /// <summary>
    /// Registra que uma peça foi encaixada e valida se a montagem foi finalizada.
    /// </summary>
    public void RegisterPiecePlaced(ComponenteBaseMontagem peca)
    {
        if (peca == null || pecasColocadas.Contains(peca)) return;

        pecasColocadas.Add(peca);
        Debug.Log($"[StepChecker] Peça '{peca.myID}' encaixada ({pecasColocadas.Count} peças no total).");

        OnPiecePlaced?.Invoke(peca);

        if (IsComputerFullyAssembled())
        {
            Debug.Log("[StepChecker] O Computador está totalmente montado!");
            OnTudoFeito?.Invoke();
        }
    }

    /// <summary>
    /// Verifica se todas as peças da lista (ou da cena) foram encaixadas.
    /// </summary>
    /// <summary>
    /// Verifica se todas as peças da lista (ou da cena) foram encaixadas E se seus parafusos foram concluídos.
    /// </summary>
    public bool IsComputerFullyAssembled()
    {
        // 1. Se houver lista de peças requeridas configurada no Inspector:
        if (pecasRequeridas != null && pecasRequeridas.Count > 0)
        {
            foreach (var req in pecasRequeridas)
            {
                if (!req.isRequired) continue;

                // Altera para verificar 'IsFullyAssembledWithScrews' em vez de apenas 'IsPlaced'
                bool encontradaEInstalada = pecasColocadas.Exists(p => p != null && p.myID == req.pieceID && p.IsFullyAssembledWithScrews);
                if (!encontradaEInstalada)
                {
                    return false;
                }
            }
            return true;
        }

        // 2. Fallback: Se a lista estiver vazia, verifica se TODAS as peças na cena foram colocadas e parafusadas
        ComponenteBaseMontagem[] todasAsPecas = FindObjectsByType<ComponenteBaseMontagem>(FindObjectsSortMode.None);
        if (todasAsPecas.Length == 0) return false;

        foreach (ComponenteBaseMontagem peca in todasAsPecas)
        {
            if (!peca.IsFullyAssembledWithScrews)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Progresso de conectores (parafusos)
    /// </summary>
    public void CheckConnectorProgress(IdentifiyerEncaixe encaixe)
    {
        if (encaixe == null) return;

        int total = encaixe.CompletedScrews.Count;
        int concluidos = encaixe.GetCompletedCount();

        Debug.Log($"[StepChecker] Progresso de parafusos em '{encaixe.getID}': {concluidos}/{total}.");

        if (encaixe.IsFullyAssembled())
        {
            Debug.Log($"[StepChecker] Todos os parafusos/conectores de '{encaixe.getID}' foram instalados!");

            if (IsComputerFullyAssembled())
            {
                OnTudoFeito?.Invoke();
            }
        }
    }

    private void OnDestroy()
    {
        Instance = null;
    }
}