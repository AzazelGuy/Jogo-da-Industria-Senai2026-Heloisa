using UnityEngine;
using System.Collections.Generic;
using TMPro; // Adicionado para suporte ao TextMeshPro UI

public class ComponenteBase : MonoBehaviour, ISelectable
{
    [Header("Configurações Principais")]
    [SerializeField] private Animator anim;
    [SerializeField] private SOPieceData InfoPeca;
    [SerializeField] private State cur_state;
    [SerializeField] private MeshFilter myModel;
    [SerializeField] private List<GameObject> LocaisEncaixe;

    [Header("Configurações da UI")]
    [Tooltip("Elemento de Texto da UI que exibirá o nome da peça apontada.")]
    [SerializeField] private TextMeshProUGUI pieceNameText;

    [Header("Configurações do StepChecker")]
    [SerializeField] private bool interactWithStepChecker = true;

    [Header("Configurações de Snapping")]
    [SerializeField] private float snapDistance = 1.0f;

    [Header("Configurações de Arraste em 3D")]
    [SerializeField] private LayerMask surfaceLayerMask = ~0;
    [SerializeField] private float dragHeightOffset = 0.05f;

    [Header("Configurações do Outline")]
    [SerializeField] private Material outlineMaterial;

    private Vector3 originalPosition;
    private Camera mainCamera;
    private IdentifiyerEncaixe targetSlot;
    private bool isSnapped = false;
    private bool isPlaced = false;
    private bool justSelected = false;

    private Renderer meshRenderer;
    private Material[] originalMaterials;
    private Material[] outlinedMaterials;

    public enum State { Normal, Selected }

    /// <summary>
    /// Propriedade que retorna o Nome da peça configurado na SOPieceData ou o nome do GameObject.
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

    private void Awake()
    {
        if (anim == null) anim = GetComponent<Animator>();
        originalPosition = transform.position;
        mainCamera = Camera.main;
        SetupOutlineMaterial();
    }

    private void OnEnable()
    {
        if (CameraController.instance != null)
            CameraController.instance.OnReturnToOverview += HandleReturnToOverview;
    }

    private void OnDisable()
    {
        if (CameraController.instance != null)
            CameraController.instance.OnReturnToOverview -= HandleReturnToOverview;
    }

    private void Start()
    {
        FindSlot();
        SetLocalSlotsVisible(false);
    }

    private string GetPieceId()
    {
        if (InfoPeca != null && !string.IsNullOrEmpty(InfoPeca.ID)) return InfoPeca.ID;
        return myID;
    }

    private bool IsSlotAvailableForUse(IdentifiyerEncaixe slot)
    {
        if (slot == null) return false;

        ComponenteBase parentPiece = slot.GetComponentInParent<ComponenteBase>();
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

        if (anim != null) anim.SetTrigger("OnSelect");
        SetOutlineVisible(true);

        GameManager.Instance.SelectObject(gameObject);

        if (targetSlot != null && IsSlotAvailableForUse(targetSlot))
        {
            targetSlot.SetSocketVisible(true);
            if (myModel != null) targetSlot.UpdateModel(myModel.mesh);
        }
    }

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
            transform.position = targetSlot.transform.position;
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
        }
        else
        {
            OnDeselect();
        }
    }

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
            EncaixeBase slotBase = targetSlot.GetComponent<EncaixeBase>()
                ?? targetSlot.GetComponentInParent<EncaixeBase>()
                ?? targetSlot.GetComponentInChildren<EncaixeBase>();

            bool shouldSpawnScrews = slotBase != null && (slotIdentifier.hasScrew || slotIdentifier.ScrewsPositions.Count > 0);
            if (shouldSpawnScrews)
            {
                slotBase.MiniGameScrew();
            }
        }

        if (CameraController.instance != null && targetFocus != null)
        {
            CameraController.instance.FocusOnPiece(targetFocus);
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
    }

    public void OnDeselect() => Deselect();

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

    public string myID => InfoPeca != null ? InfoPeca.ID : "";
    public bool IsPlaced => isPlaced;
}