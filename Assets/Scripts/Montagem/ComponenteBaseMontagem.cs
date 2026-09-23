using UnityEngine;
using System.Collections.Generic;
using TMPro; // Adicionado para suporte ao TextMeshPro UI

/// <summary>
/// Componente principal de uma peça montável. Controla seleção, arraste em 3D,
/// snapping no encaixe correto, outline visual, integração com o StepChecker
/// e o disparo do zoom de câmera / minigame de parafusos ao ser encaixada.
/// </summary>
public class ComponenteBaseMontagem : MonoBehaviour, ISelectable
{
    #region Campos Serializados - Principais

    [Header("Configurações Principais")]
    [SerializeField] private Animator anim;
    [SerializeField] private SOPieceData InfoPeca;
    [SerializeField] private State cur_state;
    [SerializeField] private MeshFilter myModel;
    [SerializeField] private List<GameObject> LocaisEncaixe;
    [SerializeField] private GameObject ParticlesPlace;

    #endregion

    #region Campos Serializados - UI

    [Header("Configurações da UI")]
    [Tooltip("Elemento de Texto da UI que exibirá o nome da peça apontada.")]
    [SerializeField] private TextMeshProUGUI pieceNameText;

    #endregion

    #region Campos Serializados - StepChecker

    [Header("Configurações do StepChecker")]
    [SerializeField] private bool interactWithStepChecker = true;

    #endregion

    #region Campos Serializados - Snapping / Arraste

    [Header("Configurações de Snapping")]
    [SerializeField] private float snapDistance = 1.0f;

    [Header("Configurações de Arraste em 3D")]
    [SerializeField] private LayerMask surfaceLayerMask = ~0;
    [SerializeField] private float dragHeightOffset = 0.05f;

    #endregion

    #region Campos Serializados - Outline

    [Header("Configurações do Outline")]
    [SerializeField] private Material outlineMaterial;

    #endregion

    #region Estado Interno

    private Vector3 originalPosition;
    private Camera mainCamera;
    private IdentifiyerEncaixe targetSlot;
    private bool isSnapped = false;
    private bool isPlaced = false;
    private bool justSelected = false;

    private Renderer meshRenderer;
    private Material[] originalMaterials;
    private Material[] outlinedMaterials;

    /// <summary>
    /// Estado de seleção da peça.
    /// </summary>
    public enum State { Normal, Selected }

    #endregion

    #region Propriedades Públicas

    /// <summary>
    /// Retorna o Nome da peça configurado na SOPieceData ou o nome do GameObject como fallback.
    /// </summary>
    public string PieceName
    {
        get
        {
            if (InfoPeca != null && !string.IsNullOrEmpty(InfoPeca.Nome))
            {
                return InfoPeca.Nome;
            }
            return gameObject.name;
        }
    }

    /// <summary>
    /// Retorna true se a peça foi colocada e se seus parafusos/conectores (caso existam) foram 100% concluídos.
    /// </summary>
    public bool IsFullyAssembledWithScrews
    {
        get
        {
            if (!isPlaced) return false;

            // Se o slot onde a peça encaixou existir, valida o estado dos parafusos dele
            if (targetSlot != null)
            {
                return targetSlot.AreScrewsFullyDone();
            }

            return true;
        }
    }

    public string myID => InfoPeca != null ? InfoPeca.ID : "";
    public bool IsPlaced => isPlaced;

    #endregion

    #region Ciclo de Vida (Unity)

    private void Awake()
    {
        if (anim == null) anim = GetComponent<Animator>();
        originalPosition = transform.position;
        mainCamera = Camera.main;
        SetupOutlineMaterial();
    }

    private void OnEnable()
    {
        // Inscreve-se no evento de retorno à visão geral para reexibir/ocultar os slots locais
        if (CameraControllerMontagem.instance != null)
            CameraControllerMontagem.instance.OnReturnToOverview += HandleReturnToOverview;
    }

    private void OnDisable()
    {
        if (CameraControllerMontagem.instance != null)
            CameraControllerMontagem.instance.OnReturnToOverview -= HandleReturnToOverview;
    }

    private void Start()
    {
        FindSlot();
        SetLocalSlotsVisible(false);
    }

    private void Update()
    {
        if (isPlaced) return;

        if (cur_state == State.Selected)
        {
            // Ignora o primeiro frame após a seleção para não processar o mesmo clique duas vezes
            if (justSelected)
            {
                justSelected = false;
                return;
            }

            HandleDraggingAndSnapping();

            if (Input.GetMouseButtonDown(0))
            {
                DropObject();
            }
        }
    }

    #endregion

    #region Implementação de Hover (Ponteiro UI)

    public void OnPointerEnter()
    {
        UpdateUIText(PieceName);
    }

    public void OnPointerExit()
    {
        UpdateUIText("");
    }

    private void UpdateUIText(string text)
    {
        if (pieceNameText != null)
        {
            pieceNameText.text = text;
        }
    }

    #endregion

    #region Implementação de ISelectable

    public void OnSelect()
    {
        FindSlot();
        SelectPeca();
    }

    public void OnDeselect() => Deselect();

    #endregion

    #region Seleção / Desseleção

    /// <summary>
    /// Marca a peça como selecionada, ativa o outline, notifica o GameManager
    /// e prepara o slot de destino para receber a peça.
    /// </summary>
    protected virtual void SelectPeca()
    {
        if (isPlaced || cur_state == State.Selected) return;

        cur_state = State.Selected;
        justSelected = true;

        if (mainCamera == null) mainCamera = Camera.main;

        if (anim != null) anim.SetTrigger("OnSelect");
        SetOutlineVisible(true);

        GameManager.Instance.SelectObject(gameObject);

        if (targetSlot != null && IsSlotAvailableForUse(targetSlot))
        {
            targetSlot.SetSocketVisible(true);
            if (myModel != null) targetSlot.UpdateModel(myModel.mesh);
        }
    }

    /// <summary>
    /// Cancela a seleção da peça, devolvendo-a à posição original (caso ainda não esteja colocada).
    /// </summary>
    protected virtual void Deselect()
    {
        if (isPlaced) return;

        cur_state = State.Normal;
        transform.position = originalPosition;
        isSnapped = false;

        SetOutlineVisible(false);
        if (anim != null) anim.SetTrigger("OnDeselect");
        if (targetSlot != null) targetSlot.UpdateModel(null);

        GameManager.Instance.ClearObject();
    }

    #endregion

    #region Arraste e Snapping

    /// <summary>
    /// Move a peça seguindo o mouse sobre um plano horizontal e verifica
    /// se ela está próxima o suficiente do slot de destino para "grudar" (snap).
    /// </summary>
    private void HandleDraggingAndSnapping()
    {
        if (mainCamera == null) mainCamera = Camera.main;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        float planeY = targetSlot != null
            ? targetSlot.transform.position.y + dragHeightOffset
            : originalPosition.y + dragHeightOffset;

        Plane interactionPlane = new Plane(Vector3.up, new Vector3(0, planeY, 0));
        Vector3 targetWorldPos = transform.position;

        if (interactionPlane.Raycast(ray, out float enter))
        {
            targetWorldPos = ray.GetPoint(enter);
        }

        if (targetSlot != null)
        {
            Vector3 slotPos = targetSlot.transform.position;
            if (Vector3.Distance(targetWorldPos, slotPos) <= snapDistance)
            {
                // Gruda na posição E copia a rotação exata do slot de destino
                transform.position = slotPos;
                transform.rotation = targetSlot.transform.rotation;
                isSnapped = true;
                return;
            }
        }

        transform.position = targetWorldPos;
        isSnapped = false;
    }

    /// <summary>
    /// Finaliza o arraste: se a peça estiver "snapada" no slot, efetiva a colocação;
    /// caso contrário, desfaz a seleção.
    /// </summary>
    private void DropObject()
    {
        if (isSnapped && targetSlot != null)
        {
            // Garante que fixa tanto a posição quanto a rotação ao instalar
            transform.position = targetSlot.transform.position;
            transform.rotation = targetSlot.transform.rotation;

            cur_state = State.Normal;
            isPlaced = true;

            if (anim != null) anim.SetTrigger("OnDeselect");

            SetOutlineVisible(false);
            GameManager.Instance.ClearObject();
            targetSlot.UpdateModel(null);

            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            SetLocalSlotsVisible(true);

            if (interactWithStepChecker && StepChecker.Instance != null)
            {
                StepChecker.Instance.RegisterPiecePlaced(this);
            }

            TriggerCameraZoom();
            if (ParticlesPlace != null)
            {
                Instantiate(ParticlesPlace, transform.position, Quaternion.identity);
            }
        }
        else
        {
            OnDeselect();
        }
    }

    #endregion

    #region Busca e Gestão de Slots

    /// <summary>
    /// Retorna o ID da peça, usando a SOPieceData se disponível ou o myID como fallback.
    /// </summary>
    private string GetPieceId()
    {
        if (InfoPeca != null && !string.IsNullOrEmpty(InfoPeca.ID)) return InfoPeca.ID;
        return myID;
    }

    /// <summary>
    /// Verifica se um slot pode ser usado por esta peça: não pode ser um slot que pertence
    /// à própria peça, e se pertence a outra peça, essa peça precisa já estar colocada.
    /// </summary>
    private bool IsSlotAvailableForUse(IdentifiyerEncaixe slot)
    {
        if (slot == null) return false;

        ComponenteBaseMontagem parentPiece = slot.GetComponentInParent<ComponenteBaseMontagem>();
        if (parentPiece == this) return false;

        if (parentPiece != null)
        {
            return parentPiece.IsPlaced;
        }

        return true;
    }

    /// <summary>
    /// Procura um slot compatível, primeiro na hierarquia atual (pais/filhos) e,
    /// se não encontrar, em toda a cena.
    /// </summary>
    private void FindSlot()
    {
        targetSlot = null;
        string expectedId = GetPieceId();

        foreach (IdentifiyerEncaixe slot in FindCompatibleSlotsInCurrentHierarchy())
        {
            if (slot == null) continue;
            if ((slot.CanAcceptPieceId(expectedId) || slot.CanAcceptPiece(this) || slot.getID == expectedId)
                && IsSlotAvailableForUse(slot))
            {
                targetSlot = slot;
                break;
            }
        }

        if (targetSlot == null)
        {
            foreach (IdentifiyerEncaixe slot in FindObjectsByType<IdentifiyerEncaixe>(FindObjectsSortMode.None))
            {
                if (slot == null) continue;
                if ((slot.CanAcceptPieceId(expectedId) || slot.CanAcceptPiece(this) || slot.getID == expectedId)
                    && IsSlotAvailableForUse(slot))
                {
                    targetSlot = slot;
                    break;
                }
            }
        }

        SetLocalSlotsVisible(false);
        if (targetSlot != null)
        {
            targetSlot.gameObject.SetActive(true);
            targetSlot.SetSocketVisible(true);
        }
    }

    /// <summary>
    /// Percorre a hierarquia atual (subindo pelos pais) coletando todos os IdentifiyerEncaixe filhos.
    /// </summary>
    private IEnumerable<IdentifiyerEncaixe> FindCompatibleSlotsInCurrentHierarchy()
    {
        Transform current = transform;
        while (current != null)
        {
            foreach (IdentifiyerEncaixe slot in current.GetComponentsInChildren<IdentifiyerEncaixe>(true))
            {
                yield return slot;
            }
            current = current.parent;
        }
    }

    /// <summary>
    /// Reage ao retorno da câmera para a visão geral, reexibindo os slots locais
    /// caso a peça já esteja colocada, ou o slot alvo caso ainda esteja pendente.
    /// </summary>
    private void HandleReturnToOverview()
    {
        if (!isPlaced)
        {
            SetLocalSlotsVisible(false);
            if (targetSlot != null && IsSlotAvailableForUse(targetSlot))
            {
                targetSlot.gameObject.SetActive(true);
            }
            return;
        }

        SetLocalSlotsVisible(true);
    }

    /// <summary>
    /// Ativa/desativa a visibilidade (renderer + collider) dos slots de encaixe filhos desta peça.
    /// </summary>
    private void SetLocalSlotsVisible(bool visible)
    {
        if (!isPlaced && visible) return;

        IdentifiyerEncaixe[] sockets = GetComponentsInChildren<IdentifiyerEncaixe>(true);
        foreach (IdentifiyerEncaixe socket in sockets)
        {
            if (socket != null)
            {
                socket.SetSocketVisible(visible);
            }
        }

        foreach (GameObject s in LocaisEncaixe)
        {
            if (s != null)
            {
                Renderer r = s.GetComponent<Renderer>();
                if (r != null) r.enabled = visible;

                Collider c = s.GetComponent<Collider>();
                if (c != null) c.enabled = visible;
            }
        }
    }

    #endregion

    #region Zoom de Câmera / Minigame de Parafusos

    /// <summary>
    /// Dispara o spawn dos parafusos (se aplicável) e o zoom da câmera no ponto de foco
    /// correspondente à peça ou ao slot em que ela foi encaixada.
    /// </summary>
    private void TriggerCameraZoom()
    {
        FocusPoint targetFocus = GetComponentInChildren<FocusPoint>();

        if (targetFocus == null && targetSlot != null)
        {
            targetFocus = targetSlot.GetComponentInChildren<FocusPoint>();
        }

        if (targetSlot != null)
        {
            IdentifiyerEncaixe slotIdentifier = targetSlot;
            EncaixeBaseMontagem slotBase = targetSlot.GetComponent<EncaixeBaseMontagem>()
                ?? targetSlot.GetComponentInParent<EncaixeBaseMontagem>()
                ?? targetSlot.GetComponentInChildren<EncaixeBaseMontagem>();

            bool shouldSpawnScrews = slotBase != null && (slotIdentifier.hasScrew || slotIdentifier.ScrewsPositions.Count > 0);
            if (shouldSpawnScrews)
            {
                slotBase.MiniGameScrew();
            }
        }

        if (CameraControllerMontagem.instance != null && targetFocus != null)
        {
            CameraControllerMontagem.instance.FocusOnPiece(targetFocus);
        }
    }

    #endregion

    #region Outline Visual

    /// <summary>
    /// Prepara o array de materiais com outline adicional, copiando os materiais originais
    /// do renderer e anexando o material de contorno ao final.
    /// </summary>
    private void SetupOutlineMaterial()
    {
        meshRenderer = myModel != null ? myModel.GetComponent<Renderer>() : GetComponent<Renderer>();
        if (meshRenderer == null || outlineMaterial == null) return;

        originalMaterials = meshRenderer.sharedMaterials;
        outlinedMaterials = new Material[originalMaterials.Length + 1];
        for (int i = 0; i < originalMaterials.Length; i++) outlinedMaterials[i] = originalMaterials[i];
        outlinedMaterials[outlinedMaterials.Length - 1] = outlineMaterial;
    }

    /// <summary>
    /// Alterna entre os materiais originais e os materiais com outline.
    /// </summary>
    private void SetOutlineVisible(bool visible)
    {
        if (meshRenderer == null || outlineMaterial == null) return;
        meshRenderer.materials = visible ? outlinedMaterials : originalMaterials;
    }

    #endregion
}
