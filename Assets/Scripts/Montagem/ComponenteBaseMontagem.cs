using UnityEngine;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Componente principal de uma peça montável. Controla seleção, arraste em 3D,
/// snapping no encaixe correto, outline visual, integração com o StepChecker
/// e gerenciamento de física (Rigidbody e MeshCollider) ao segurar, soltar ou encaixar.
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
    private bool inscritoNaCamera = false;

    private Renderer meshRenderer;
    private Material[] originalMaterials;
    private Material[] outlinedMaterials;

    /// <summary>
    /// Estado de seleção da peça.
    /// </summary>
    public enum State { Normal, Selected }

    #endregion

    #region Propriedades Públicas

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

    public string myID => InfoPeca != null ? InfoPeca.ID : "";
    public bool IsPlaced => isPlaced;
    public Tipo? TipoPeca => InfoPeca != null ? InfoPeca.tipoDePeca : (Tipo?)null;

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
        TrySubscribeToCamera();
    }

    private void OnDisable()
    {
        UnsubscribeFromCamera();
    }

    private void Start()
    {
        TrySubscribeToCamera();
        FindSlot();
        SetLocalSlotsVisible(false);

        // Garante que nasce com física ativa caso venha da caixa de peças (spawnado)
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null && !isPlaced)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }
    }

    private void Update()
    {
        if (isPlaced) return;
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

    #endregion

    #region Inscrição no Evento da Câmera

    private void TrySubscribeToCamera()
    {
        if (inscritoNaCamera) return;

        CameraControllerMontagem cam = CameraControllerMontagem.instance;
        if (cam == null) return;

        cam.OnReturnToOverview += HandleReturnToOverview;
        inscritoNaCamera = true;
    }

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

    public void OnPointerEnter() => UpdateUIText(PieceName);
    public void OnPointerExit() => UpdateUIText("");

    private void UpdateUIText(string text)
    {
        if (pieceNameText != null) pieceNameText.text = text;
    }

    #endregion

    #region Implementação de ISelectable

    public void OnSelect()
    {
        if (!isPlaced && interactWithStepChecker && StepChecker.Instance != null
            && !StepChecker.Instance.PodeColocarPeca(this, out string motivo))
        {
            StepChecker.Instance.NotificarBloqueio(motivo);
            return;
        }

        FindSlot();
        SelectPeca();
    }

    public void OnDeselect() => Deselect();

    #endregion

    #region Seleção / Desseleção

    protected virtual void SelectPeca()
    {
        if (isPlaced || cur_state == State.Selected) return;

        cur_state = State.Selected;
        justSelected = true;

        if (mainCamera == null) mainCamera = Camera.main;

        // DESLIGA a física e gravidade enquanto segura para o mouse controlar suavemente
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        if (anim != null) anim.SetTrigger("OnSelect");
        SetOutlineVisible(true);

        GameManager.Instance.SelectObject(gameObject);

        if (targetSlot != null && IsSlotAvailableForUse(targetSlot))
        {
            targetSlot.SetSocketVisible(true);
            if (myModel != null) targetSlot.UpdateModel(myModel.mesh);
        }
    }

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

        // Devolve a física caso solte sem encaixar
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }
    }

    #endregion

    #region Arraste e Snapping

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

    private void DropObject()
    {
        Rigidbody rb = GetComponent<Rigidbody>();

        if (isSnapped && targetSlot != null)
        {
            // Encaixou perfeitamente no slot
            transform.position = targetSlot.transform.position;
            transform.rotation = targetSlot.transform.rotation;

            cur_state = State.Normal;
            isPlaced = true;
            targetSlot.Occupy(this);

            if (anim != null) anim.SetTrigger("OnDeselect");

            SetOutlineVisible(false);
            GameManager.Instance.ClearObject();
            targetSlot.UpdateModel(null);

            // Desliga collider principal e trava a física de vez
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
            if (rb != null) rb.isKinematic = true;

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
            // Soltou fora: LIGA a física usando o MeshCollider para colidir com a mesa
            cur_state = State.Normal;
            isSnapped = false;

            SetOutlineVisible(false);
            if (anim != null) anim.SetTrigger("OnDeselect");
            if (targetSlot != null) targetSlot.UpdateModel(null);

            GameManager.Instance.ClearObject();

            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
            }
        }
    }

    #endregion

    #region Busca e Gestão de Slots

    private string GetPieceId()
    {
        if (InfoPeca != null && !string.IsNullOrEmpty(InfoPeca.ID)) return InfoPeca.ID;
        return myID;
    }

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

                Renderer r = s.GetComponent<Renderer>();
                if (r != null) r.enabled = visible;

                Collider c = s.GetComponent<Collider>();
                if (c != null) c.enabled = visible;
            }
        }
    }

    #endregion

    #region Zoom de Câmera / Minigame de Parafusos

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

    private void SetupOutlineMaterial()
    {
        meshRenderer = myModel != null ? myModel.GetComponent<Renderer>() : GetComponent<Renderer>();
        if (meshRenderer == null || outlineMaterial == null) return;

        originalMaterials = meshRenderer.sharedMaterials;
        outlinedMaterials = new Material[originalMaterials.Length + 1];
        for (int i = 0; i < originalMaterials.Length; i++) outlinedMaterials[i] = originalMaterials[i];
        outlinedMaterials[outlinedMaterials.Length - 1] = outlineMaterial;
    }

    private void SetOutlineVisible(bool visible)
    {
        if (meshRenderer == null || outlineMaterial == null) return;
        meshRenderer.materials = visible ? outlinedMaterials : originalMaterials;
    }

    #endregion
}