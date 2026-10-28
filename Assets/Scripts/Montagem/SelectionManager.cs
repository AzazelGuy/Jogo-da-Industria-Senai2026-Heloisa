using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Detecta o objeto ISelectable sob o cursor via raycast e gerencia hover, seleção,
/// duplo clique e hold (clique segurado), repassando as chamadas correspondentes
/// da interface ISelectable.
/// </summary>
public class SelectionManager : MonoBehaviour
{
    #region Singleton

    public static SelectionManager Instance { get; private set; }

    #endregion

    #region Campos Serializados

    [Header("Raycast Settings")]
    [SerializeField] private LayerMask selectableLayer;
    [SerializeField] private float maxDistance = 100f;

    [Header("Interaction Timings")]
    [SerializeField] private float doubleClickThreshold = 0.3f;
    [SerializeField] private float holdThreshold = 0.5f;

    #endregion

    #region Estado Interno

    private Camera cachedCamera;
    private ISelectable currentSelected;
    private ISelectable currentHovered; // Guarda a peça sendo focada no Hover
    private ISelectable heldObject;

    private float pointerDownTime;
    private float lastClickTime;
    private bool isHolding;
    private bool ignorarCliqueAtual; // evita "clique fantasma" no mundo ao fechar a caixa de peças

    #endregion

    #region Ciclo de Vida (Unity)

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

    private void Update()
    {
        if (SceneManager.GetActiveScene().name != "ModoMontagem")
        {
            Destroy(gameObject);
        }
        if (PauseMenuActions.instance != null && PauseMenuActions.instance.Paused)
        {
            return;
        }
        HandleHover();

        // Com a caixa de peças aberta, o mundo 3D não recebe cliques
        if (CaixaPecasUi.Instance != null && CaixaPecasUi.Instance.IsOpen)
        {
            ignorarCliqueAtual = true;
            return;
        }

        HandleInput();
    }
    #endregion

    #region Hover

    /// <summary>
    /// Processa o estado de passagem de mouse (Hover) continuamente a cada frame,
    /// disparando OnPointerEnter/OnPointerExit e atualizando o texto de nome na UI.
    /// </summary>
    private void HandleHover()
    {
        ISelectable hoveredNow = GetSelectableUnderCursor();

        if (hoveredNow != currentHovered)
        {
            if (currentHovered != null)
            {
                currentHovered.OnPointerExit();
                if (UiController.Instance.textNameUI != null) UiController.Instance.textNameUI.text = "";
            }

            currentHovered = hoveredNow;

            if (currentHovered != null)
            {
                currentHovered.OnPointerEnter();

                if (UiController.Instance.textNameUI != null && currentHovered is ComponenteBaseMontagem comp)
                {
                    UiController.Instance.textNameUI.text = comp.PieceName;
                }
            }
        }
    }

    #endregion

    #region Input (Clique, Hold, Duplo Clique)

    /// <summary>
    /// Processa o clique do mouse: detecta início/fim do clique, hold (segurar)
    /// e duplo clique, repassando para o ISelectable apropriado.
    /// </summary>
    private void HandleInput()
    {
        // Depois de fechar a caixa, espera o botão ser solto antes de aceitar cliques de novo
        if (ignorarCliqueAtual)
        {
            bool algumBotao = Input.GetMouseButton(0) || Input.GetMouseButtonUp(0)
                           || Input.GetMouseButton(1) || Input.GetMouseButtonUp(1);
            if (algumBotao) return;
            ignorarCliqueAtual = false;
        }

        // Botão direito: guardar a peça (a que está na mão ou a apontada pelo mouse)
        if (Input.GetMouseButtonDown(1))
        {
            ISelectable alvo = ObterAlvoDoBotaoDireito();
            alvo?.OnRightClick();
        }

        if (Input.GetMouseButtonDown(0))
        {
            pointerDownTime = Time.time;
            isHolding = false;
            heldObject = GetSelectableUnderCursor();
        }

        if (Input.GetMouseButton(0))
        {
            if (!isHolding && (Time.time - pointerDownTime >= holdThreshold))
            {
                isHolding = true;
            }

            if (isHolding && heldObject != null)
            {
                heldObject.OnHold();
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            // Só trata como clique/seleção se não tiver virado um "hold"
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

            heldObject = null;
        }
    }

    #endregion

    #region Seleção

    /// <summary>
    /// Seleciona um novo objeto, desselecionando o anterior (se diferente).
    /// </summary>
    private void SelectObject(ISelectable selectable)
    {
        if (currentSelected != null && currentSelected != selectable)
        {
            currentSelected.OnDeselect();
        }

        currentSelected = selectable;
        currentSelected.OnSelect();
    }

    /// <summary>
    /// Desseleciona o objeto atualmente selecionado (caso exista).
    /// </summary>
    private void DeselectCurrent()
    {
        if (currentSelected != null)
        {
            currentSelected.OnDeselect();
            currentSelected = null;
        }
    }

    #endregion

    #region Botão Direito / Limpeza de Referências

    /// <summary>
    /// Peça na mão tem prioridade; se não houver, usa o que está sob o cursor.
    /// </summary>
    private ISelectable ObterAlvoDoBotaoDireito()
    {
        if (currentSelected is ComponenteBaseMontagem naMao && naMao.NaMao) return naMao;
        return currentHovered;
    }

    /// <summary>
    /// Remove qualquer referência ao objeto (seleção, hover, hold). Deve ser chamado
    /// antes de destruir um ISelectable, senão o manager tenta falar com um objeto morto.
    /// </summary>
    public void Esquecer(ISelectable alvo)
    {
        if (alvo == null) return;

        if (currentSelected == alvo) currentSelected = null;
        if (heldObject == alvo) heldObject = null;

        if (currentHovered == alvo)
        {
            currentHovered = null;
            if (UiController.Instance != null && UiController.Instance.textNameUI != null)
            {
                UiController.Instance.textNameUI.text = "";
            }
        }
    }

    #endregion

    #region Utilitários

    /// <summary>
    /// Dispara um raycast a partir da câmera principal na posição do mouse
    /// e retorna o ISelectable encontrado (procurando também nos pais do collider atingido).
    /// </summary>
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

    /// <summary>
    /// Atualiza a referência em cache para a câmera principal (Camera.main).
    /// </summary>
    private void CacheCamera()
    {
        cachedCamera = Camera.main;
    }

    #endregion
}
