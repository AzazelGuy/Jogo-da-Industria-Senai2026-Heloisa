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

    private void Start()
    {
        IdentifiyerEncaixe[] slots = FindObjectsByType<IdentifiyerEncaixe>(FindObjectsSortMode.None);
        foreach (IdentifiyerEncaixe slot in slots)
        {
            if (slot.getID == InfoPeca.ID)
            {
                targetSlot = slot;
                break;
            }
        }

        foreach (GameObject s in LocaisEncaixe)
        {
            s.SetActive(false);
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

        if (targetSlot != null && myModel != null)
        {
            targetSlot.UpdateModel(myModel.mesh);
        }
    }

    private void HandleDraggingAndSnapping()
    {
        if (mainCamera == null) mainCamera = Camera.main;

        // 1. Lança um raio a partir da posição do mouse na tela
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        // 2. Cria um plano 3D dinâmico alinhado com a visão da câmera na profundidade da peça
        dragPlane = new Plane(-mainCamera.transform.forward, transform.position);

        Vector3 targetWorldPos;

        if (dragPlane.Raycast(ray, out float enter))
        {
            targetWorldPos = ray.GetPoint(enter);
        }
        else
        {
            // Fallback usando a profundidade salva
            Vector3 mouseScreenPos = Input.mousePosition;
            mouseScreenPos.z = dragDepth;
            targetWorldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);
        }

        // 3. Impede que a peça atravesse superfícies ou a bancada
        /*if (Physics.Raycast(targetWorldPos + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 1.5f, surfaceLayerMask))
        {
            float minY = hit.point.y + dragHeightOffset;
            if (targetWorldPos.y < minY)
            {
                targetWorldPos.y = minY;
            }
        }*/
        targetWorldPos.y = transform.position.y;

        // 4. Snapping magnético em 3D com o slot correto
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
                foreach(GameObject s in LocaisEncaixe)
                {
                    s.SetActive(true);
                }
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