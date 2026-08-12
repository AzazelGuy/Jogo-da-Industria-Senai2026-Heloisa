using UnityEngine;

public class SelectionManager : MonoBehaviour
{
    // Singleton Instance
    public static SelectionManager Instance { get; private set; }

    [Header("Raycast Settings")]
    [SerializeField] private LayerMask selectableLayer;
    [SerializeField] private float maxDistance = 100f;

    private Camera cachedCamera;
    private ISelectable currentSelected;

    private void Awake()
    {
        // Singleton Enforcement
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        CacheCamera();
    }

    private void Update()
    {
        // Replace Input.GetMouseButtonDown(0) with Input System equivalent if needed
        if (Input.GetMouseButtonDown(0))
        {
            TrySelectObject();
        }
    }

    private void TrySelectObject()
    {
        if (cachedCamera == null)
        {
            CacheCamera();
            if (cachedCamera == null) return; // Guard clause if no camera exists
        }

        Ray ray = cachedCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, selectableLayer))
        {
            // Try to find an ISelectable interface on the hit collider or its parents
            ISelectable selectable = hit.collider.GetComponentInParent<ISelectable>();

            if (selectable != null)
            {
                // Deselect previous target
                if (currentSelected != null && currentSelected != selectable)
                {
                    currentSelected.OnDeselect();
                }

                currentSelected = selectable;
                currentSelected.OnSelect();
                return;
            }
        }

        // Clicked on empty space or non-selectable object -> Deselect current object
        if (currentSelected != null)
        {
            currentSelected.OnDeselect();
            currentSelected = null;
        }
    }

    private void CacheCamera()
    {
        cachedCamera = Camera.main;
        if (cachedCamera == null)
        {
            Debug.LogWarning("[SelectionManager] No camera tagged 'MainCamera' found in the scene.");
        }
    }
}