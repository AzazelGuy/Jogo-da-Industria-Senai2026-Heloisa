using UnityEngine;

// A interface ISelectable permite que o GameManager ou o sistema de cliques interaja com este objeto
public class ComponenteBase : MonoBehaviour, ISelectable
{
    [Header("Configurações Principais")]
    [Tooltip("Animador opcional para tocar animações ao selecionar/deselecionar.")]
    [SerializeField] private Animator anim;

    [Tooltip("ID único desta peça (ex: 'RAM_Slot_1', 'GPU'). Deve ser idêntico ao ID do slot de destino.")]
    [SerializeField] private string expectedID;

    [Tooltip("Estado atual da peça (Normal = na bancada / Selected = sendo arrastada).")]
    [SerializeField] private State cur_state;

    [Tooltip("O MeshFilter original desta peça.")]
    [SerializeField] private MeshFilter myModel;

    [Header("Configurações de Snapping (Encaixe Magnético)")]
    [Tooltip("Distância máxima entre a peça e o slot para ela 'grudar' no lugar.")]
    [SerializeField] private float snapDistance = 1.0f;

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
            if (slot.getID == expectedID)
            {
                targetSlot = slot;
                break;
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

    private void HandleDraggingAndSnapping()
    {
        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = mainCamera.WorldToScreenPoint(originalPosition).z;

        Vector3 targetWorldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);
        targetWorldPos.z = originalPosition.z;

        if (targetSlot != null)
        {
            Vector3 slotPos = targetSlot.transform.position;
            slotPos.z = originalPosition.z;

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

    public void OnSelect()
    {
        if (isPlaced || cur_state == State.Selected) return;

        cur_state = State.Selected;
        justSelected = true;

        if (anim != null) anim.SetTrigger("OnSelect");

        SetOutlineVisible(true);

        GameManager.Instance.SelectObject(gameObject);

        if (targetSlot != null && myModel != null)
        {
            targetSlot.UpdateModel(myModel.mesh);
        }
    }

    private void DropObject()
    {
        if (isSnapped && targetSlot != null)
        {
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
        }
        else
        {
            OnDeselect();
        }
    }

    public void OnDeselect()
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

    public string myID => expectedID;
}