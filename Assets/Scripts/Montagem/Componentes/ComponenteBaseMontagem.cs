using UnityEngine;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Componente principal de uma peça montável. Controla seleção, arraste em 3D,
/// snapping no encaixe correto, outline visual e integração com o StepChecker.
/// </summary>
public class ComponenteBaseMontagem : MonoBehaviour, ISelectable
{
    #region Campos Serializados - Referências e Configuração Principal

    [Header("Configurações Principais")]
    [Tooltip("Animações executadas ao selecionar ou soltar a peça.")]
    [SerializeField] private Animator anim;
    [Tooltip("Dados configuráveis da peça, incluindo nome e identificador.")]
    [SerializeField] private SOPieceData InfoPeca;
    [Tooltip("Estado atual de interação da peça.")]
    [SerializeField] private State cur_state;
    [Tooltip("Malha usada para exibir a peça e criar o outline.")]
    [SerializeField] private MeshFilter myModel;
    [Tooltip("Objetos de encaixe locais que podem ser habilitados após a montagem.")]
    [SerializeField] private List<GameObject> LocaisEncaixe;
    [Tooltip("Efeito visual instanciado quando a peça é colocada.")]
    [SerializeField] private GameObject ParticlesPlace;

    #endregion

    #region Campos Serializados - UI

    [Header("Configurações da UI")]
    [Tooltip("Elemento de Texto da UI que exibirá o nome da peça apontada.")]
    [SerializeField] private TextMeshProUGUI pieceNameText;

    #endregion

    #region Campos Serializados - StepChecker

    [Header("Configurações do StepChecker")]
    [Tooltip("Quando ativo, consulta as regras de progressão antes de permitir a interação.")]
    [SerializeField] private bool interactWithStepChecker = true;

    #endregion

    #region Campos Serializados - Snapping / Arraste

    [Header("Configurações de Snapping")]
    [Tooltip("Distância máxima usada para considerar um encaixe próximo.")]
    [SerializeField] private float snapDistance = 1.0f;

    [Header("Configurações de Arraste em 3D")]
    [Tooltip("Camadas de superfície disponíveis para a interação de arraste.")]
    [SerializeField] private LayerMask surfaceLayerMask = ~0;
    [Tooltip("Altura acima do slot ou da posição inicial enquanto a peça é arrastada.")]
    [SerializeField] private float dragHeightOffset = 0.05f;

    #endregion

    #region Estado Interno - Arraste e Encaixe

    // Posição usada como referência caso ainda não haja um slot-alvo.
    private Vector3 originalPosition;
    // Câmera usada para converter a posição do mouse em um raio no mundo.
    private Camera mainCamera;
    // Slot compatível atualmente destacado ou usado como destino do encaixe.
    private IdentifiyerEncaixe targetSlot;
    // Indica que a peça está alinhada com o slot durante o arraste.
    private bool isSnapped = false;
    // Indica que a peça já foi efetivamente colocada e não pode ser arrastada.
    private bool isPlaced = false;
    // Impede que o primeiro frame após a seleção também mova a peça.
    private bool justSelected = false;
    // Evita inscrever repetidamente o mesmo callback da câmera.
    private bool inscritoNaCamera = false;
    // Evita que o mesmo clique usado para soltar selecione a peça novamente.
    private bool bloquearReselecao = false;

    #endregion

    #region Estado Interno - Materiais do Outline

    // Renderer da malha que recebe o material adicional de contorno.
    private Renderer meshRenderer;
    // Materiais originais preservados para restaurar a aparência normal.
    private Material[] originalMaterials;
    // Materiais originais acrescidos do material de contorno.
    private Material[] outlinedMaterials;

    #endregion

    #region Estado da Peça

    /// <summary>
    /// Estados possíveis durante a interação do jogador com a peça.
    /// </summary>
    public enum State { Normal, Selected }

    #endregion

    #region Propriedades Públicas

    /// <summary>
    /// Obtém o nome configurado nos dados da peça ou, se não houver, o nome do objeto.
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
    /// Informa se a peça foi colocada e se todos os parafusos do slot foram concluídos.
    /// Peças sem um slot associado são consideradas completas após serem colocadas.
    /// </summary>
    public bool IsFullyAssembledWithScrews
    {
        get
        {
            if (!isPlaced) return false;
            if (targetSlot != null)
            {
                return targetSlot.AreScrewsFullyDone();
            }
            return true;
        }
    }

    /// <summary>Identificador configurado para esta peça; vazio quando não há dados associados.</summary>
    public string myID => InfoPeca != null ? InfoPeca.ID : "";

    /// <summary>Indica se a peça foi colocada em um encaixe.</summary>
    public bool IsPlaced => isPlaced;

    /// <summary>Indica se o jogador está segurando a peça.</summary>
    public bool NaMao => cur_state == State.Selected;

    #endregion

    #region Ciclo de Vida (Unity)

    /// <summary>
    /// Inicializa referências locais e prepara os materiais usados pelo outline.
    /// </summary>
    private void Awake()
    {
        if (anim == null) anim = GetComponent<Animator>();
        originalPosition = transform.position;
        mainCamera = Camera.main;
        foreach(GameObject g in LocaisEncaixe)
        {
            g.SetActive(false);
        }
    }

    /// <summary>Registra o callback de retorno da câmera quando o objeto é habilitado.</summary>
    private void OnEnable()
    {
        TrySubscribeToCamera();
    }

    /// <summary>Remove o callback da câmera para evitar inscrições de objetos desativados.</summary>
    private void OnDisable()
    {
        UnsubscribeFromCamera();
    }

    /// <summary>
    /// Localiza o encaixe inicial e oculta os encaixes locais.
    /// </summary>
    private void Start()
    {
        TrySubscribeToCamera();
        FindSlot();
        SetLocalSlotsVisible(false);
    }

    /// <summary>
    /// Atualiza a interação de arraste e processa o clique que solta a peça.
    /// A peça não é atualizada enquanto está colocada ou durante estados que bloqueiam a interação.
    /// </summary>
    private void Update()
    {
        if (isPlaced) return;
        if (MistakeEvent.IsFeedbackActive) return;
        if (PauseMenuActions.instance != null && PauseMenuActions.instance.Paused) return;

        if (cur_state == State.Selected)
        {
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

    private void LateUpdate()
    {
        // O SelectionManager processa o mouse up antes de liberar uma nova seleção.
        if (bloquearReselecao && Input.GetMouseButtonUp(0))
        {
            bloquearReselecao = false;
        }
    }

    #endregion

    #region Inscrição no Evento da Câmera

    /// <summary>
    /// Inscreve esta peça no evento de retorno à visão geral, se o controlador já estiver disponível.
    /// </summary>
    private void TrySubscribeToCamera()
    {
        if (inscritoNaCamera) return;

        CameraControllerMontagem cam = CameraControllerMontagem.instance;
        if (cam == null) return;

        cam.OnReturnToOverview += HandleReturnToOverview;
        inscritoNaCamera = true;
    }

    /// <summary>Remove a inscrição existente no evento de retorno à visão geral.</summary>
    private void UnsubscribeFromCamera()
    {
        if (!inscritoNaCamera) return;

        CameraControllerMontagem cam = CameraControllerMontagem.instance;
        if (cam != null)
        {
            cam.OnReturnToOverview -= HandleReturnToOverview;
        }
        inscritoNaCamera = false;
    }

    #endregion

    #region Implementação de Hover (Ponteiro UI)

    /// <summary>Exibe o nome da peça quando o ponteiro entra sobre ela.</summary>
    public void OnPointerEnter() => UpdateUIText(PieceName);

    /// <summary>Limpa o nome exibido quando o ponteiro deixa a peça.</summary>
    public void OnPointerExit() => UpdateUIText("");

    /// <summary>Atualiza o texto associado à peça, se houver um elemento de UI configurado.</summary>
    private void UpdateUIText(string text)
    {
        if (pieceNameText != null) pieceNameText.text = text;
    }

    #endregion

    #region Implementação de ISelectable

    /// <summary>
    /// Valida ferramenta e progressão antes de localizar um slot e iniciar a seleção.
    /// </summary>
    public void OnSelect()
    {
        if (bloquearReselecao) return;

        if (!isPlaced && !IsRequiredToolSelected(UiController.HandToolName))
        {
            NotificarFerramentaNecessaria(UiController.HandToolName);
            return;
        }

        if (!isPlaced && interactWithStepChecker && StepChecker.Instance != null
            && !StepChecker.Instance.PodeColocarPeca(this, out string motivo))
        {
            StepChecker.Instance.NotificarBloqueio(motivo);
            return;
        }

        FindSlot();
        SelectPeca();
    }

    /// <summary>Verifica se a ferramenta exigida está selecionada na interface.</summary>
    private static bool IsRequiredToolSelected(string toolName)
    {
        return UiController.Instance != null && UiController.Instance.IsToolSelected(toolName);
    }

    /// <summary>
    /// Informa por meio do StepChecker ou do log que a ferramenta exigida não está selecionada.
    /// </summary>
    private static void NotificarFerramentaNecessaria(string toolName)
    {
        string message = $"Selecione '{toolName}' na Hotbar para pegar peças.";
        if (StepChecker.Instance != null)
            StepChecker.Instance.NotificarBloqueio(message);
        else
            Debug.LogWarning($"[ComponenteBaseMontagem] {message}");
    }

    /// <summary>Encerra a seleção atual.</summary>
    public void OnDeselect() => Deselect();

    /// <summary>
    /// Limpa o que a peça deixou espalhado (modelo fantasma no slot, estado de seleção, textos)
    /// antes de ser destruída pela caixa.
    /// </summary>
    public void PrepararParaGuardar()
    {
        if (cur_state == State.Selected)
        {
            cur_state = State.Normal;
            isSnapped = false;

            if (targetSlot != null) targetSlot.UpdateModel(null);
            GameManager.Instance.ClearObject();
        }

        UpdateUIText("");
    }

    #endregion

    #region Seleção / Desseleção

    /// <summary>
    /// Inicia a seleção: mostra o outline e apresenta o slot compatível.
    /// </summary>
    protected virtual void SelectPeca()
    {
        if (isPlaced || cur_state == State.Selected) return;

        cur_state = State.Selected;
        justSelected = true;

        if (mainCamera == null) mainCamera = Camera.main;

        if (anim != null) anim.SetTrigger("OnSelect");

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

        if (anim != null) anim.SetTrigger("OnDeselect");
        if (targetSlot != null) targetSlot.UpdateModel(null);

        GameManager.Instance.ClearObject();
    }

    #endregion

    #region Arraste e Snapping

    /// <summary>
    /// Move a peça sobre um plano horizontal e alinha sua posição e rotação ao slot próximo.
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
    /// Resolve o slot no momento da soltura e conclui a colocação ou devolve a peça à posição original.
    /// A busca sob o cursor tem prioridade; em seguida, é considerado o slot compatível mais próximo.
    /// </summary>
    private void DropObject()
    {
        bloquearReselecao = true;

        IdentifiyerEncaixe slotCompativelProximo = FindCompatibleSlotUnderCursor()
            ?? FindNearestCompatibleSlot(transform.position);
        if (slotCompativelProximo != null)
        {
            targetSlot = slotCompativelProximo;
            isSnapped = true;
        }

        if (isSnapped && targetSlot != null)
        {
            // Confirma a transformação antes de marcar o slot como ocupado.
            transform.position = targetSlot.transform.position;
            transform.rotation = targetSlot.transform.rotation;

            cur_state = State.Normal;
            isPlaced = true;
            targetSlot.Occupy(this);

            if (anim != null) anim.SetTrigger("OnDeselect");

            GameManager.Instance.ClearObject();
            targetSlot.UpdateModel(null);

            // O slot agora controla a colocação; a colisão da peça é desativada.
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
            // Avisa antes de mover a peça de volta, pois a checagem usa a posição atual.
            NotificarEncaixeIncompativelProximo();
            Deselect();
        }
    }

    /// <summary>
    /// Se a soltura ocorreu perto de um slot incompatível, comunica ao StepChecker qual foi.
    /// </summary>
    private void NotificarEncaixeIncompativelProximo()
    {
        if (!interactWithStepChecker || StepChecker.Instance == null) return;
        if (FindNearestCompatibleSlot(transform.position) != null) return;

        string pieceId = GetPieceId();
        IdentifiyerEncaixe slotMaisProximo = null;
        float menorDistancia = snapDistance;

        foreach (IdentifiyerEncaixe slot in FindObjectsByType<IdentifiyerEncaixe>(FindObjectsSortMode.None))
        {
            if (slot == null) continue;

            float distancia = Vector3.Distance(transform.position, slot.transform.position);
            if (distancia > menorDistancia) continue;

            bool acceptsPiece = slot.CanAcceptPieceId(pieceId)
                                || slot.CanAcceptPiece(this)
                                || slot.getID == pieceId;
            if (acceptsPiece) continue;

            if (slotMaisProximo == null || distancia < menorDistancia)
            {
                slotMaisProximo = slot;
                menorDistancia = distancia;
            }
        }

        if (slotMaisProximo != null)
            StepChecker.Instance.NotificarBloqueio(
                $"O encaixe '{slotMaisProximo.name}' não aceita a peça '{PieceName}'.");
    }

    #endregion

    #region Busca e Gestão de Slots

    #region Identificação e Disponibilidade

    /// <summary>
    /// Obtém o identificador da peça, preferindo o valor configurado nos dados associados.
    /// </summary>
    private string GetPieceId()
    {
        if (InfoPeca != null && !string.IsNullOrEmpty(InfoPeca.ID)) return InfoPeca.ID;
        return myID;
    }

    /// <summary>
    /// Verifica se o slot pode ser usado, considerando ocupação e relação com a peça pai.
    /// Slots de uma peça pai só podem ser usados quando essa peça já está colocada.
    /// </summary>
    private bool IsSlotAvailableForUse(IdentifiyerEncaixe slot)
    {
        if (slot == null) return false;
        if (slot.IsOccupiedByOther(this)) return false;

        ComponenteBaseMontagem parentPiece = slot.GetComponentInParent<ComponenteBaseMontagem>();
        if (parentPiece == this) return false;

        if (parentPiece != null)
        {
            return parentPiece.IsPlaced;
        }

        return true;
    }

    #endregion

    #region Localização de Encaixes

    /// <summary>Localiza o slot compatível disponível mais próximo dentro do limite de snapping.</summary>
    private IdentifiyerEncaixe FindNearestCompatibleSlot(Vector3 position)
    {
        IdentifiyerEncaixe nearestSlot = null;
        float nearestDistance = snapDistance;
        string expectedId = GetPieceId();

        foreach (IdentifiyerEncaixe slot in FindObjectsByType<IdentifiyerEncaixe>(FindObjectsSortMode.None))
        {
            if (slot == null || !IsSlotAvailableForUse(slot)) continue;

            bool acceptsPiece = slot.CanAcceptPieceId(expectedId)
                                || slot.CanAcceptPiece(this)
                                || slot.getID == expectedId;
            if (!acceptsPiece) continue;

            float distance = Vector3.Distance(position, slot.transform.position);
            if (distance <= nearestDistance)
            {
                nearestSlot = slot;
                nearestDistance = distance;
            }
        }

        return nearestSlot;
    }

    /// <summary>
    /// Procura sob o cursor um slot compatível, escolhendo o primeiro atingido pela câmera.
    /// </summary>
    private IdentifiyerEncaixe FindCompatibleSlotUnderCursor()
    {
        Camera camera = mainCamera != null ? mainCamera : Camera.main;
        if (camera == null) return null;

        Ray ray = camera.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 100f, ~0, QueryTriggerInteraction.Collide);
        IdentifiyerEncaixe closestCompatibleSlot = null;
        float closestDistance = float.MaxValue;
        string expectedId = GetPieceId();

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null
                || hit.collider.transform == transform
                || hit.collider.transform.IsChildOf(transform))
            {
                continue;
            }

            IdentifiyerEncaixe slot = hit.collider.GetComponentInParent<IdentifiyerEncaixe>();
            if (slot == null || !IsSlotAvailableForUse(slot) || hit.distance >= closestDistance) continue;

            bool acceptsPiece = slot.CanAcceptPieceId(expectedId)
                                || slot.CanAcceptPiece(this)
                                || slot.getID == expectedId;
            if (!acceptsPiece) continue;

            closestCompatibleSlot = slot;
            closestDistance = hit.distance;
        }

        return closestCompatibleSlot;
    }

    /// <summary>
    /// Localiza o encaixe compatível da peça, priorizando sua hierarquia e depois a cena inteira.
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
    /// Percorre os encaixes da hierarquia atual e de seus ancestrais, incluindo objetos inativos.
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
    #endregion

    #region Visibilidade

    /// <summary>
    /// Atualiza a visibilidade dos encaixes quando a câmera retorna à visão geral.
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
    /// Mostra ou oculta os encaixes associados a esta peça e seus elementos visuais/interativos.
    /// Peças ainda não colocadas não exibem seus encaixes locais como destinos ativos.
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
                IdentifiyerEncaixe slotDoObjeto = s.GetComponentInParent<IdentifiyerEncaixe>();
                if (slotDoObjeto != null)
                {
                    slotDoObjeto.SetSocketVisible(visible);
                    continue;
                }
                else
                {
                    s.SetActive(true);
                }

                Renderer r = s.GetComponent<Renderer>();
                if (r != null) r.enabled = visible;

                UsoFerramentaMontagem acaoFerramenta = s.GetComponent<UsoFerramentaMontagem>();
                Collider c = s.GetComponent<Collider>();
                if (c != null && (acaoFerramenta == null || acaoFerramenta.enabled))
                {
                    c.enabled = visible;
                }

            }
        }
    }

    #endregion

    #endregion

    #region Zoom de Câmera e Minigame de Parafusos

    /// <summary>
    /// Inicia o minigame de parafusos quando necessário e direciona a câmera ao ponto de foco.
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
}
//Puta que pariu esse código
/*
Nunca tente fazer um jogo assim crianças! 
Senão eu vou estragular todas
*/
