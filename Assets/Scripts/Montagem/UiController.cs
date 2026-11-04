using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Controlador central da UI do jogo: exibe a tela de vitória ao concluir a montagem,
/// mostra o nome da peça sob o cursor, controla a hotbar (scroll e teclas numéricas)
/// e trata o retorno ao menu principal.
/// O spawn de peças e a UI da caixa ficam SÓ na CaixaPecasUi.
/// </summary>
public class UiController : MonoBehaviour
{
    public const string HandToolName = "Mão";
    public const string ScrewdriverToolName = "Chave de Fenda";

    #region Singleton

    public static UiController Instance;

    #endregion

    #region Campos Serializados

    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private CanvasGroup _winUi;
    [SerializeField] private float fadeDuration = 1.5f;

    private Coroutine _fadeRoutine;

    [Header("HotBar")]

    [SerializeField] RectTransform imgSelecionado;
    [SerializeField] RectTransform[] posHotbarItens;
    [SerializeField] string[] nomeHotbarItens;
    [SerializeField] TextMeshProUGUI nomedoItemDisplay;
    private int posItemAtual;

    [Header("Cursores")]
    [Tooltip("Cursor usado quando o slot selecionado não tem cursor próprio (ex: Mão).")]
    [SerializeField] private Image cursorPadrao;
    [Tooltip("Mesma ordem/tamanho da hotbar. Deixe vazio (None) nos slots que usam o cursor padrão.")]
    [SerializeField] private Image[] cursoresHotbarItens;

    [Header("---Caixa de Peças---")]
    [Tooltip("Se vazio, usa a CaixaPecasUi que existir na cena.")]
    [SerializeField] private CaixaPecasUi caixaUi;

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

        AtualizarHotbar();
    }

    private void Update()
    {
        if (SceneManager.GetActiveScene().name != "ModoMontagem")
        {
            Destroy(gameObject);
            return;
        }

        // Duplicata que ainda vai ser destruída não deve processar input
        if (Instance != this) return;

        if (MistakeEvent.IsFeedbackActive) return;
        if (PauseMenuActions.instance != null && PauseMenuActions.instance.Paused) return;

        // Com a caixa de peças aberta, o scroll/teclas não mexem na hotbar (o scroll pode ser da lista da caixa)
        if (CaixaPecasUi.Instance != null && CaixaPecasUi.Instance.IsOpen) return;

        HandleHotbarPointerInput();
        HandleHotbarInput();
    }

    private void OnEnable()
    {
        // Add event listener
        StepChecker.OnTudoFeito += Completo;
    }

    private void OnDisable()
    {
        // Remove event listener
        StepChecker.OnTudoFeito -= Completo;
    }

    private void OnDestroy()
    {
        // Só limpa se for a instância "oficial"; senão a duplicata apagava a referência da original
        if (Instance == this) Instance = null;
    }

    #endregion

    #region Eventos do StepChecker

    /// <summary>
    /// Chamado quando o StepChecker sinaliza que a montagem foi 100% concluída.
    /// Exibe a tela de vitória.
    /// </summary>
    private void Completo()
    {
        ChangeActive(true);
    }

    /// <summary>
    /// Mostra (com fade) ou esconde (instantâneo) a tela de vitória.
    /// </summary>
    public void ChangeActive(bool b)
    {
        if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);

        if (b)
        {
            _fadeRoutine = StartCoroutine(FadeIn(_winUi, fadeDuration));
            Cursor.visible = true;
        }
        else
        {
            _winUi.alpha = 0f;
            _winUi.gameObject.SetActive(false);
        }
    }

    private IEnumerator FadeIn(CanvasGroup group, float duration)
    {
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
        group.gameObject.SetActive(true);

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime; // funciona mesmo com timeScale = 0
            group.alpha = Mathf.Clamp01(t / duration);
            yield return null;
        }

        group.alpha = 1f;
        group.interactable = true;
        group.blocksRaycasts = true;
        _fadeRoutine = null;
    }

    #endregion

    #region Hotbar

    private int TotalItensHotbar => posHotbarItens != null ? posHotbarItens.Length : 0;

    /// <summary>
    /// Lê scroll do mouse e teclas 1-9 (fileira de cima E teclado numérico; 0 = 10º item)
    /// e só atualiza a hotbar quando o item selecionado realmente muda.
    /// </summary>
    private void HandleHotbarInput()
    {
        int total = TotalItensHotbar;
        if (total <= 0) return;

        int novoIndice = posItemAtual;

        // Scroll: cima = item anterior, baixo = próximo
        float scroll = Input.mouseScrollDelta.y;
        if (scroll > 0f) novoIndice--;
        else if (scroll < 0f) novoIndice++;

        // Teclas numéricas: a tecla pressionada tem prioridade sobre o scroll
        int teclasDisponiveis = Mathf.Min(total, 9);
        for (int i = 0; i < teclasDisponiveis; i++)
        {
            if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + i))
                || Input.GetKeyDown((KeyCode)((int)KeyCode.Keypad1 + i)))
            {
                novoIndice = i;
                break;
            }
        }

        if (total >= 10 && (Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Keypad0)))
        {
            novoIndice = 9;
        }

        // "Meia volta" (wrap-around) seguro também para valores negativos
        novoIndice = ((novoIndice % total) + total) % total;

        if (novoIndice == posItemAtual) return;

        posItemAtual = novoIndice;
        AtualizarHotbar();
    }

    private void HandleHotbarPointerInput()
    {
        if (!Input.GetMouseButtonDown(0) || posHotbarItens == null) return;

        for (int i = 0; i < posHotbarItens.Length; i++)
        {
            RectTransform slot = posHotbarItens[i];
            if (!IsPointerOverSlot(slot)) continue;

            posItemAtual = i;
            AtualizarHotbar();
            return;
        }
    }

    /// <summary>Informa se o mouse está sobre um item da hotbar, evitando clique no mundo 3D.</summary>
    public bool IsPointerOverHotbar()
    {
        if (posHotbarItens == null) return false;

        foreach (RectTransform slot in posHotbarItens)
        {
            if (IsPointerOverSlot(slot)) return true;
        }

        return false;
    }

    private bool IsPointerOverSlot(RectTransform slot)
    {
        if (slot == null) return false;

        Canvas canvas = slot.GetComponentInParent<Canvas>();
        Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;

        return RectTransformUtility.RectangleContainsScreenPoint(slot, Input.mousePosition, eventCamera);
    }

    /// <summary>
    /// Move o marcador de seleção e atualiza o nome exibido do item atual.
    /// </summary>
    private void AtualizarHotbar()
    {
        if (TotalItensHotbar <= 0 || _winUi.isActiveAndEnabled) return;

        posItemAtual = Mathf.Clamp(posItemAtual, 0, TotalItensHotbar - 1);

        if (imgSelecionado != null && posHotbarItens[posItemAtual] != null)
        {
            imgSelecionado.position = posHotbarItens[posItemAtual].position;
        }

        if (nomedoItemDisplay != null)
        {
            nomedoItemDisplay.text = FerramentaSelecionadaAtual;
        }

        AtualizarCursor();
    }

    /// <summary>
    /// Liga só o cursor do item selecionado; se o slot não tiver cursor próprio, usa o padrão.
    /// </summary>
    private void AtualizarCursor()
    {
        if (_winUi.isActiveAndEnabled) return;
        Image alvo = null;

        if (cursoresHotbarItens != null && posItemAtual >= 0 && posItemAtual < cursoresHotbarItens.Length)
        {
            alvo = cursoresHotbarItens[posItemAtual];
        }

        if (alvo == null) alvo = cursorPadrao;

        if (cursorPadrao != null) cursorPadrao.enabled = false;

        if (cursoresHotbarItens != null)
        {
            foreach (Image img in cursoresHotbarItens)
            {
                if (img != null) img.enabled = false;
            }
        }

        if (alvo != null) alvo.enabled = true;
    }

    #endregion

    #region API Pública

    /// <summary>
    /// Carrega a cena do menu principal e limpa as instâncias persistentes
    /// (UiController e StepChecker).
    /// </summary>
    public void ReturntoMainMenu()
    {
        Cursor.visible = true;
        LoadingScreen.Carregar("MenuInicial");
        Destroy(gameObject);
        if (StepChecker.Instance != null) Destroy(StepChecker.Instance.gameObject);
    }

    /// <summary>
    /// Retorna o nome/ID da ferramenta atualmente selecionada na Hotbar.
    /// </summary>
    public string FerramentaSelecionadaAtual
    {
        get
        {
            if (nomeHotbarItens != null && posItemAtual >= 0 && posItemAtual < nomeHotbarItens.Length)
            {
                return nomeHotbarItens[posItemAtual];
            }
            return string.Empty;
        }
    }

    public bool IsToolSelected(string toolName)
    {
        return string.Equals(FerramentaSelecionadaAtual, toolName, System.StringComparison.Ordinal);
    }

    public void ShowBoxUI()
    {
        if (caixaUi == null) caixaUi = CaixaPecasUi.Instance;

        if (caixaUi == null)
        {
            Debug.LogWarning("[CaixadePecasAbrirMenu] Nenhuma CaixaPecasUi encontrada na cena!", this);
            return;
        }

        caixaUi.AbrirCaixa();
    }

    public TextMeshProUGUI textNameUI => nameText;

    #endregion
}