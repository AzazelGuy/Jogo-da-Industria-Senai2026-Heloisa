using UnityEngine;
using System.Collections.Generic;

// A interface ISelectable permite que o GameManager ou o sistema de cliques interaja com este objeto
public class ComponenteBase : MonoBehaviour, ISelectable
{
    [Header("Configurações Principais")]
    [Tooltip("Animador opcional para tocar animações ao selecionar/deselecionar.")]
    [SerializeField] private Animator anim;

    [Tooltip("Informação da Peça")]
    [SerializeField] private SOPieceData InfoPeca;

    [Tooltip("Estado atual da peça (Normal = na bancada / Selected = sendo arrastada).")]
    [SerializeField] private State cur_state;

    [Tooltip("O MeshFilter original desta peça.")]
    [SerializeField] private MeshFilter myModel;

    [Tooltip("Locais de encaixe para ")]
    [SerializeField] private List<GameObject> LocaisEncaixe;

    [Header("Configurações de Snapping (Encaixe Magnético)")]
    [Tooltip("Distância máxima entre a peça e o slot para ela 'grudar' no lugar.")]
    [SerializeField] private float snapDistance = 1.0f;

    [Header("Configurações de Arraste em 3D")]
    [Tooltip("LayerMask de superfícies/bancada para evitar que a peça atravesse o chão.")]
    [SerializeField] private LayerMask surfaceLayerMask = ~0;

    [Tooltip("Elevação suave em Y ao arrastar para não colidir com a bancada.")]
    [SerializeField] private float dragHeightOffset = 0.05f;

    [Header("Configurações do Outline (Material)")]
    [Tooltip("Arraste aqui o Material 'M_Outline' que usa o Custom/OutlineShader.")]
    [SerializeField] private Material outlineMaterial;

    // --- VARIÁVEIS INTERNAS DE CONTROLE ---
    private Vector3 originalPosition;        // Guarda a posição original da peça na bancada
    private Camera mainCamera;               // Referência para a Câmera Principal
    private IdentifiyerEncaixe targetSlot;   // O slot exato onde esta peça deve ser instalada
    private bool isSnapped = false;          // True quando a peça está grudada no slot
    private bool isPlaced = false;           // True quando a peça é instalada definitivamente
    private bool justSelected = false;       // Trava para evitar soltar a peça no mesmo frame do clique

    // Controle de Arraste 3D
    private float dragDepth;                 // Distância da peça até a câmera no momento da seleção
    private Plane dragPlane;                 // Plano 3D dinâmico relativo à câmera

    // Controle de Materiais para o efeito de Outline
    private Renderer meshRenderer;
    private Material[] originalMaterials;   // Materiais originais da peça
    private Material[] outlinedMaterials;   // Materiais originais + o Material de Outline

    public enum State
    {
        Normal,
        Selected
    }

    private void Awake()
    {
        if (anim == null)
        {
            anim = GetComponent<Animator>();
        }

        originalPosition = transform.position;
        mainCamera = Camera.main;

        // Configura a lista de materiais usando o Material do Inspector
        SetupOutlineMaterial();
    }

    private void OnEnable()
    {
        if (CameraController.instance != null)
        {
            CameraController.instance.OnReturnToOverview += HandleReturnToOverview;
        }
    }

    private void OnDisable()
    {
        if (CameraController.instance != null)
        {
            CameraController.instance.OnReturnToOverview -= HandleReturnToOverview;
        }
    }

    private void Start()
    {
        FindSlot();
    }

    private string GetPieceId()
    {
        if (InfoPeca != null && !string.IsNullOrEmpty(InfoPeca.ID))
        {
            return InfoPeca.ID;
        }

        return myID;
    }

    private void FindSlot()
    {
        targetSlot = null;

        string expectedId = GetPieceId();
        foreach (IdentifiyerEncaixe slot in FindCompatibleSlotsInCurrentHierarchy())
        {
            if (slot == null) continue;

            if (slot.CanAcceptPieceId(expectedId) || slot.CanAcceptPiece(this) || slot.getID == expectedId)
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
                if (slot.CanAcceptPieceId(expectedId) || slot.CanAcceptPiece(this) || slot.getID == expectedId)
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
        if (isPlaced) return;

        SetLocalSlotsVisible(true);
        if (targetSlot != null)
        {
            targetSlot.gameObject.SetActive(true);
        }
    }

    private void SetLocalSlotsVisible(bool visible)
    {
        // Mantém o socket na hierarquia ativo, mas desliga só visual/collider.
        // Isso evita que o GameObject pai fique completamente morto e impossível de reativar.
        IdentifiyerEncaixe[] sockets = GetComponentsInChildren<IdentifiyerEncaixe>(true);
        foreach (IdentifiyerEncaixe socket in sockets)
        {
            if (socket != null)
            {
                socket.SetSocketVisible(visible);
            }
        }

        // Compatibilidade com a lista antiga, caso ainda exista preenchida no Inspector.
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
    private void Update()
    {
        if (isPlaced) return;

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

    public void OnSelect()
    {
        FindSlot();
        SelectPeca();
    }

    protected virtual void SelectPeca()
    {
        if (isPlaced || cur_state == State.Selected) return;

        cur_state = State.Selected;
        justSelected = true;

        if (mainCamera == null) mainCamera = Camera.main;

        // Calcula a profundidade inicial da peça em relação à câmera atual
        dragDepth = mainCamera.WorldToScreenPoint(transform.position).z;

        if (anim != null) anim.SetTrigger("OnSelect");

        SetOutlineVisible(true);

        GameManager.Instance.SelectObject(gameObject);

        if (targetSlot != null)
        {
            targetSlot.SetSocketVisible(true);
            if (myModel != null)
            {
                targetSlot.UpdateModel(myModel.mesh);
            }
        }
    }

    private void HandleDraggingAndSnapping()
    {
        if (mainCamera == null) mainCamera = Camera.main;

        // 1. Gera o raio a partir do ponteiro do mouse
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        // 2. Usa a altura do slot de destino, quando existir. Isso resolve prefabs aninhados
        //    (ex.: CPU dentro da placa-mãe, que fica acima da bancada).
        float planeY = targetSlot != null
            ? targetSlot.transform.position.y + dragHeightOffset
            : originalPosition.y + dragHeightOffset;

        Plane interactionPlane = new Plane(Vector3.up, new Vector3(0, planeY, 0));

        Vector3 targetWorldPos = transform.position;

        // 3. Projeta o raio no plano correto da peça/slot
        if (interactionPlane.Raycast(ray, out float enter))
        {
            targetWorldPos = ray.GetPoint(enter);
        }

        // 4. Snapping magnético 3D com o slot de destino
        if (targetSlot != null)
        {
            Vector3 slotPos = targetSlot.transform.position;
            float distance = Vector3.Distance(targetWorldPos, slotPos);

            if (distance <= snapDistance)
            {
                transform.position = slotPos;
                isSnapped = true;
                return;
            }
        }

        // 5. Atualiza a posição mantendo o deslizamento na superfície correta
        transform.position = targetWorldPos;
        isSnapped = false;
    }

    private void DropObject()
    {
        if (isSnapped && targetSlot != null)
        {
            // 1. Fixa a peça na posição do slot
            transform.position = targetSlot.transform.position;
            cur_state = State.Normal;
            isPlaced = true;

            if (anim != null) anim.SetTrigger("OnDeselect");

            SetOutlineVisible(false);

            GameManager.Instance.ClearObject();
            targetSlot.UpdateModel(null);

            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = false;
            }

            if (LocaisEncaixe.Count > 0)
            {
                SetLocalSlotsVisible(true);
            }
            // 2. Dispara a aproximação suave de Câmera (CameraController)
            TriggerCameraZoom();
        }
        else
        {
            OnDeselect();
        }
    }

    /// <summary>
    /// Localiza o Ponto de Foco (FocusPoint) e comanda a Câmera a aproximar
    /// </summary>
    private void TriggerCameraZoom()
    {
        // Busca o FocusPoint na PEÇA ou em seus filhos
        FocusPoint targetFocus = GetComponentInChildren<FocusPoint>();

        // Se não houver na peça, busca no SLOT
        if (targetFocus == null && targetSlot != null)
        {
            targetFocus = targetSlot.GetComponentInChildren<FocusPoint>();
        }

        if (targetSlot != null)
        {
            IdentifiyerEncaixe slotIdentifier = targetSlot;
            EncaixeBase slotBase = targetSlot.GetComponent<EncaixeBase>()
                ?? targetSlot.GetComponentInParent<EncaixeBase>()
                ?? targetSlot.GetComponentInChildren<EncaixeBase>();

            bool shouldSpawnScrews = slotBase != null && (slotIdentifier.hasScrew || slotIdentifier.ScrewsPositions.Count > 0);
            if (shouldSpawnScrews)
            {
                slotBase.MiniGameScrew();
            }
            else if (slotIdentifier != null && slotIdentifier.hasScrew)
            {
                Debug.LogWarning($"[ComponenteBase] O slot '{targetSlot.name}' tem hasScrew=true, mas não encontrou um EncaixeBase válido para disparar o minigame.");
            }
        }

        // Executa o Zoom se o controlador e o ponto existirem
        if (CameraController.instance != null)
        {
            if (targetFocus != null)
            {
                CameraController.instance.FocusOnPiece(targetFocus);
            }
            else
            {
                Debug.LogWarning($"[ComponenteBase] Peça '{gameObject.name}' foi encaixada, mas nenhum 'FocusPoint' foi encontrado nela ou no slot!");
            }
        }
        else
        {
            Debug.LogError("[ComponenteBase] CameraController não foi encontrado na cena! Verifique se ele está na Main Camera.");
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

        if (targetSlot != null)
        {
            targetSlot.UpdateModel(null);
        }

        GameManager.Instance.ClearObject();
    }

    public void OnDeselect()
    {
        Deselect();
    }

    #region Sistema de Aplicação do Material de Outline

    private void SetupOutlineMaterial()
    {
        meshRenderer = myModel != null ? myModel.GetComponent<Renderer>() : GetComponent<Renderer>();

        if (meshRenderer == null || outlineMaterial == null)
        {
            if (outlineMaterial == null)
            {
                Debug.LogWarning($"Atenção: O 'Outline Material' não foi atribuído no Inspector do objeto {gameObject.name}!");
            }
            return;
        }

        // Salva os materiais originais do objeto
        originalMaterials = meshRenderer.sharedMaterials;

        // Cria a lista com o Material de Outline no final
        outlinedMaterials = new Material[originalMaterials.Length + 1];
        for (int i = 0; i < originalMaterials.Length; i++)
        {
            outlinedMaterials[i] = originalMaterials[i];
        }
        outlinedMaterials[outlinedMaterials.Length - 1] = outlineMaterial;
    }

    private void SetOutlineVisible(bool visible)
    {
        if (meshRenderer == null || outlineMaterial == null) return;

        // Alterna entre a lista comum de materiais e a lista com o material extra de Outline
        meshRenderer.materials = visible ? outlinedMaterials : originalMaterials;
    }

    #endregion

    public string myID => InfoPeca != null ? InfoPeca.ID : "";

}