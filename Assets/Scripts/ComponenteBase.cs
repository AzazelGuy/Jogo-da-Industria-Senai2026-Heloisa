using UnityEngine;
using System.Collections.Generic;

// A interface ISelectable permite que o GameManager ou o sistema de cliques interaja com este objeto
public class ComponenteBase : MonoBehaviour, ISelectable
{
    [Header("Configura��es Principais")]
    [Tooltip("Animador opcional para tocar anima��es ao selecionar/deselecionar.")]
    [SerializeField] private Animator anim;

    [Tooltip("Informa��o da Pe�a")]
    [SerializeField] private SOPieceData InfoPeca;

    [Tooltip("Estado atual da pe�a (Normal = na bancada / Selected = sendo arrastada).")]
    [SerializeField] private State cur_state;

    [Tooltip("O MeshFilter original desta pe�a.")]
    [SerializeField] private MeshFilter myModel;

    [Tooltip("Locais de encaixe para ")]
    [SerializeField] private List<GameObject> LocaisEncaixe;

    [Header("Configura��es de Snapping (Encaixe Magn�tico)")]
    [Tooltip("Dist�ncia m�xima entre a pe�a e o slot para ela 'grudar' no lugar.")]
    [SerializeField] private float snapDistance = 1.0f;

    [Header("Configura��es de Arraste em 3D")]
    [Tooltip("LayerMask de superf�cies/bancada para evitar que a pe�a atravesse o ch�o.")]
    [SerializeField] private LayerMask surfaceLayerMask = ~0;

    [Tooltip("Eleva��o suave em Y ao arrastar para n�o colidir com a bancada.")]
    [SerializeField] private float dragHeightOffset = 0.05f;

    [Header("Configura��es do Outline (Material)")]
    [Tooltip("Arraste aqui o Material 'M_Outline' que usa o Custom/OutlineShader.")]
    [SerializeField] private Material outlineMaterial;

    // --- VARI�VEIS INTERNAS DE CONTROLE ---
    private Vector3 originalPosition;        // Guarda a posi��o original da pe�a na bancada
    private Camera mainCamera;               // Refer�ncia para a C�mera Principal
    private IdentifiyerEncaixe targetSlot;   // O slot exato onde esta pe�a deve ser instalada
    private bool isSnapped = false;          // True quando a pe�a est� grudada no slot
    private bool isPlaced = false;           // True quando a pe�a � instalada definitivamente
    private bool justSelected = false;       // Trava para evitar soltar a pe�a no mesmo frame do clique

    // Controle de Arraste 3D
    private float dragDepth;                 // Dist�ncia da pe�a at� a c�mera no momento da sele��o
    private Plane dragPlane;                 // Plano 3D din�mico relativo � c�mera

    // Controle de Materiais para o efeito de Outline
    private Renderer meshRenderer;
    private Material[] originalMaterials;   // Materiais originais da pe�a
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

        // Calcula a profundidade inicial da pe�a em rela��o � c�mera atual
        dragDepth = mainCamera.WorldToScreenPoint(transform.position).z;

        if (anim != null) anim.SetTrigger("OnSelect");

        SetOutlineVisible(true);

        GameManager.Instance.SelectObject(gameObject);

        if (targetSlot != null && myModel != null)
        {
            targetSlot.UpdateModel(myModel.mesh);
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

        // 4. Snapping magn�tico 3D com o slot de destino
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

        // 5. Atualiza a posi��o mantendo o deslizamento na superf�cie correta
        transform.position = targetWorldPos;
        isSnapped = false;
    }

    private void DropObject()
    {
        if (isSnapped && targetSlot != null)
        {
            // 1. Fixa a pe�a na posi��o do slot
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
            // 2. Dispara a aproxima��o suave de C�mera (CameraController)
            TriggerCameraZoom();
        }
        else
        {
            OnDeselect();
        }
    }

    /// <summary>
    /// Localiza o Ponto de Foco (FocusPoint) e comanda a C�mera a aproximar
    /// </summary>
    private void TriggerCameraZoom()
    {
        // Busca o FocusPoint na PE�A ou em seus filhos
        FocusPoint targetFocus = GetComponentInChildren<FocusPoint>();

        // Se n�o houver na pe�a, busca no SLOT
        if (targetFocus == null && targetSlot != null)
        {
            targetFocus = targetSlot.GetComponentInChildren<FocusPoint>();
            if (targetSlot.GetComponentInParent<IdentifiyerEncaixe>().hasScrew)
            {
                targetSlot.GetComponentInParent<EncaixeBase>().MiniGameScrew();
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
                Debug.LogWarning($"[ComponenteBase] Pe�a '{gameObject.name}' foi encaixada, mas nenhum 'FocusPoint' foi encontrado nela ou no slot!");
            }
        }
        else
        {
            Debug.LogError("[ComponenteBase] CameraController n�o foi encontrado na cena! Verifique se ele est� na Main Camera.");
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

    #region Sistema de Aplica��o do Material de Outline

    private void SetupOutlineMaterial()
    {
        meshRenderer = myModel != null ? myModel.GetComponent<Renderer>() : GetComponent<Renderer>();

        if (meshRenderer == null || outlineMaterial == null)
        {
            if (outlineMaterial == null)
            {
                Debug.LogWarning($"Aten��o: O 'Outline Material' n�o foi atribu�do no Inspector do objeto {gameObject.name}!");
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