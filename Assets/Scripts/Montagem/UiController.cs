using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Controlador central da UI do jogo: exibe a tela de vitória ao concluir a montagem,
/// mostra o nome da peça sob o cursor, controla a hotbar (scroll e teclas numéricas)
/// e trata o retorno ao menu principal.
/// O spawn de peças e a UI da caixa ficam SÓ na CaixaPecasUi.
/// </summary>
public class UiController : MonoBehaviour
{
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

        if (PauseMenuActions.instance != null && PauseMenuActions.instance.Paused) return;

        // Com a caixa de peças aberta, o scroll/teclas não mexem na hotbar (o scroll pode ser da lista da caixa)
        if (CaixaPecasUi.Instance != null && CaixaPecasUi.Instance.IsOpen) return;

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
            if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i))
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

    /// <summary>
    /// Move o marcador de seleção e atualiza o nome exibido do item atual.
    /// </summary>
    private void AtualizarHotbar()
    {
        if (TotalItensHotbar <= 0) return;

        posItemAtual = Mathf.Clamp(posItemAtual, 0, TotalItensHotbar - 1);

        if (imgSelecionado != null && posHotbarItens[posItemAtual] != null)
        {
            imgSelecionado.position = posHotbarItens[posItemAtual].position;
        }

        if (nomedoItemDisplay != null)
        {
            nomedoItemDisplay.text = FerramentaSelecionadaAtual;
        }
    }

    #endregion

    #region API Pública

    /// <summary>
    /// Carrega a cena do menu principal e limpa as instâncias persistentes
    /// (UiController e StepChecker).
    /// </summary>
    public void ReturntoMainMenu()
    {
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

    public TextMeshProUGUI textNameUI => nameText;

    #endregion
}
