using UnityEngine;

public class SelectionManager : MonoBehaviour
{
    public static SelectionManager Instance { get; private set; }

    [Header("Raycast Settings")]
    [SerializeField] private LayerMask selectableLayer;
    [SerializeField] private float maxDistance = 100f;

    [Header("Interaction Timings")]
    [SerializeField] private float doubleClickThreshold = 0.3f;
    [SerializeField] private float holdThreshold = 0.5f;

    private Camera cachedCamera;
    private ISelectable currentSelected;
    private ISelectable heldObject; // Guarda o objeto focado durante o drag

    private float pointerDownTime;
    private float lastClickTime;
    private bool isHolding;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start() => CacheCamera();

    private void Update() => HandleInput();

    private void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            pointerDownTime = Time.time;
            isHolding = false;
            heldObject = GetSelectableUnderCursor(); // Captura o objeto no momento do clique
        }

        if (Input.GetMouseButton(0))
        {
            if (!isHolding && (Time.time - pointerDownTime >= holdThreshold))
            {
                isHolding = true;
            }

            // Envia o evento OnHold para o objeto capturado no inicio do clique
            if (isHolding && heldObject != null)
            {
                heldObject.OnHold();
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (!isHolding)
            {
                ISelectable hitObj = GetSelectableUnderCursor();

                if (hitObj != null)
                {
                    SelectObject(hitObj);

                    if (Time.time - lastClickTime <= doubleClickThreshold)
                    {
                        hitObj.OnDoubleClick();
                    }

                    lastClickTime = Time.time;
                }
                else
                {
                    DeselectCurrent();
                }
            }

            heldObject = null; // Libera a referência ao soltar o mouse
        }
    }

    private ISelectable GetSelectableUnderCursor()
    {
        if (cachedCamera == null) CacheCamera();
        if (cachedCamera == null) return null;

        Ray ray = cachedCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, selectableLayer))
        {
            return hit.collider.GetComponentInParent<ISelectable>();
        }

        return null;
    }

    private void SelectObject(ISelectable selectable)
    {
        if (currentSelected != null && currentSelected != selectable)
        {
            currentSelected.OnDeselect();
        }

        currentSelected = selectable;
        currentSelected.OnSelect();
    }

    private void DeselectCurrent()
    {
        if (currentSelected != null)
        {
            currentSelected.OnDeselect();
            currentSelected = null;
        }
    }

    private void CacheCamera()
    {
        cachedCamera = Camera.main;
    }
}