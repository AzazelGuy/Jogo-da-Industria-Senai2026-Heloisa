using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Representa um slot/encaixe onde uma peça (ComponenteBaseMontagem) pode ser instalada.
/// Controla a compatibilidade com peças/tipos aceitos, a visibilidade do socket na cena,
/// a ocupação do slot e o progresso dos parafusos/conectores associados a este encaixe.
/// </summary>
public class IdentifiyerEncaixe : MonoBehaviour
{
    #region Campos Serializados - Identificação

    [SerializeField] private string myID;
    [SerializeField] private MeshFilter usedModel;
    [SerializeField] private Tipo Tipo = Tipo.CPU;

    #endregion

    #region Campos Serializados - Compatibilidade

    [Header("Compatibilidade de Peça")]
    [SerializeField] private List<string> acceptedPieceIDs = new List<string>();
    [SerializeField] private List<Tipo> acceptedTypes = new List<Tipo>();

    #endregion

    #region Campos Serializados - Conectores & Parafusos

    [Header("Conectores & Parafusos")]
    [SerializeField] public List<Transform> ScrewsPositions = new List<Transform>();
    [SerializeField] public List<bool> CompletedScrews = new List<bool>();

    #endregion

    #region Campos Serializados - Minigame

    [Header("Minigame / Interação")]
    public bool hasScrewMiniStep = true;

    #endregion

    #region Estado Interno

    // Peça que está ocupando este slot (apenas em runtime, não serializado).
    private ComponenteBaseMontagem ocupante;

    // Última visibilidade pedida por outros scripts (null = ninguém pediu ainda).
    // A visibilidade real também depende de a sequência de etapas já ter liberado este slot.
    private bool? ultimaSolicitacaoVisivel;

    #endregion

    #region Propriedades Públicas

    public List<string> AcceptedPieceIDs => acceptedPieceIDs;
    public List<Tipo> AcceptedTypes => acceptedTypes;
    public string getID => myID;
    public bool hasScrew => hasScrewMiniStep;
    public bool IsOccupied => ocupante != null;

    #endregion

    #region Ciclo de Vida (Unity)

    private void OnEnable()
    {
        StepChecker.OnEtapaMudou += HandleEtapaMudou;

        // Se alguém já pediu visibilidade antes, reaplica considerando a etapa atual
        if (ultimaSolicitacaoVisivel.HasValue)
        {
            AplicarVisibilidade();
        }
    }

    private void OnDisable()
    {
        StepChecker.OnEtapaMudou -= HandleEtapaMudou;
    }

    private void Start()
    {
        UpdateModel(null);
    }

    private void HandleEtapaMudou(int indice, EtapaMontagem etapa)
    {
        if (ultimaSolicitacaoVisivel.HasValue)
        {
            AplicarVisibilidade();
        }
    }

    #endregion

    #region Ocupação do Slot

    /// <summary>
    /// Retorna true se o slot já está ocupado por uma peça diferente da informada.
    /// </summary>
    public bool IsOccupiedByOther(ComponenteBaseMontagem piece)
    {
        return ocupante != null && ocupante != piece;
    }

    /// <summary>
    /// Marca a peça informada como ocupante deste slot.
    /// </summary>
    public void Occupy(ComponenteBaseMontagem piece)
    {
        ocupante = piece;
    }

    #endregion

    #region Preview de Modelo (Socket)

    /// <summary>
    /// Atualiza a malha exibida no socket (usada como preview da peça que será encaixada ali).
    /// </summary>
    public void UpdateModel(Mesh NewModel)
    {
        if (usedModel != null)
        {
            usedModel.mesh = NewModel;
        }
    }

    #endregion

    #region Progresso de Conectores / Parafusos

    /// <summary>
    /// Recebe a confirmação de que um conector/parafuso específico foi colocado
    /// e, caso todos já estejam completos, aciona o retorno da câmera à visão geral.
    /// </summary>
    public void NotifyConnectorPlaced(int index)
    {
        if (index >= 0 && index < CompletedScrews.Count)
        {
            CompletedScrews[index] = true;

            // Notifica o StepChecker sobre o progresso dos conectores
            if (StepChecker.Instance != null)
            {
                StepChecker.Instance.CheckConnectorProgress(this);
            }
        }

        if (IsFullyAssembled() && CameraControllerMontagem.instance != null)
        {
            CameraControllerMontagem.instance.ReturnToOverview();
        }
    }

    /// <summary>
    /// Retorna quantos parafusos/conectores deste encaixe já foram concluídos.
    /// </summary>
    public int GetCompletedCount()
    {
        int count = 0;
        foreach (bool completed in CompletedScrews)
        {
            if (completed) count++;
        }
        return count;
    }

    /// <summary>
    /// Retorna true se este encaixe possui parafusos configurados e todos já foram concluídos.
    /// </summary>
    public bool IsFullyAssembled()
    {
        return GetCompletedCount() == CompletedScrews.Count && CompletedScrews.Count > 0;
    }

    /// <summary>
    /// Retorna true se este slot não precisa de minigame de parafusos
    /// ou se todos os seus parafusos/conectores já foram instalados.
    /// </summary>
    public bool AreScrewsFullyDone()
    {
        // Se não usa minigame de parafuso ou se a lista de posições é vazia, considera concluído
        if (!hasScrewMiniStep || ScrewsPositions.Count == 0) return true;

        // Se possui parafusos, valida se todos foram colocados
        return IsFullyAssembled();
    }

    #endregion

    #region Visibilidade do Socket

    /// <summary>
    /// Ativa/desativa a visibilidade (renderer + collider) de todo o socket na cena.
    /// </summary>
    public void SetSocketVisible(bool visible)
    {
        ultimaSolicitacaoVisivel = visible;
        AplicarVisibilidade();
    }

    /// <summary>
    /// Aplica a visibilidade/colisão do socket: só fica ligado se foi pedido E se a sequência
    /// de etapas já liberou este slot (ex.: o slot do cooler só aparece depois da pasta térmica).
    /// </summary>
    private void AplicarVisibilidade()
    {
        bool visible = ultimaSolicitacaoVisivel.GetValueOrDefault(false)
            && (StepChecker.Instance == null || StepChecker.Instance.SlotLiberado(this));

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (renderer != null)
            {
                renderer.enabled = visible;
            }
        }

        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        foreach (Collider collider in colliders)
        {
            if (collider != null)
            {
                collider.enabled = visible;
            }
        }
    }

    #endregion

    #region Compatibilidade de Peças

    /// <summary>
    /// Verifica se este encaixe aceita a peça informada, seja por lista de IDs aceitos,
    /// por ID igual ao do slot ou pelo tipo da peça (lista de tipos aceitos).
    /// </summary>
    public bool CanAcceptPiece(ComponenteBaseMontagem piece)
    {
        if (piece == null) return false;

        string pieceId = piece.myID;

        if (!string.IsNullOrEmpty(pieceId) && acceptedPieceIDs.Contains(pieceId))
        {
            return true;
        }

        if (!string.IsNullOrEmpty(pieceId) && !string.IsNullOrEmpty(myID) && pieceId == myID)
        {
            return true;
        }

        // Compara o tipo DA PEÇA (antes comparava o tipo do próprio slot, o que aceitava qualquer peça).
        if (acceptedTypes.Count == 0) return false;

        return piece.TipoPeca.HasValue && acceptedTypes.Contains(piece.TipoPeca.Value);
    }

    /// <summary>
    /// Verifica se este encaixe aceita um determinado ID de peça (lista de IDs ou ID próprio).
    /// </summary>
    public bool CanAcceptPieceId(string pieceId)
    {
        if (string.IsNullOrEmpty(pieceId)) return false;
        if (acceptedPieceIDs.Contains(pieceId)) return true;
        if (!string.IsNullOrEmpty(myID) && myID == pieceId) return true;
        return false;
    }

    /// <summary>
    /// Adiciona um novo ID de peça à lista de aceitos, evitando duplicatas.
    /// </summary>
    public void AddAcceptedPieceId(string pieceId)
    {
        if (string.IsNullOrEmpty(pieceId) || acceptedPieceIDs.Contains(pieceId)) return;
        acceptedPieceIDs.Add(pieceId);
    }

    /// <summary>
    /// Adiciona um novo Tipo à lista de tipos aceitos, evitando duplicatas.
    /// </summary>
    public void AddAcceptedType(Tipo type)
    {
        if (!acceptedTypes.Contains(type))
        {
            acceptedTypes.Add(type);
        }
    }

    #endregion
}
