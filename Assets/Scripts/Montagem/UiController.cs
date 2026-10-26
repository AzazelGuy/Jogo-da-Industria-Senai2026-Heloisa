using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Controlador central da UI do jogo: exibe a tela de vitória ao concluir a montagem,
/// mostra o nome da peça sob o cursor e trata o retorno ao menu principal.
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
    private int posItemQnt;

    [Header("Spawn de Peças da Caixa")]
    [Tooltip("Ponto de referência na mesa onde a peça deve aparecer.")]
    [SerializeField] private Transform pontoSpawnMesa;
    [Tooltip("Painel/Canvas da UI da Caixa de Peças que deve fechar ao clicar.")]
    [SerializeField] private GameObject painelCaixaUI;

    #endregion

    #region Ciclo de Vida (Unity)

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        posItemQnt = posHotbarItens.Count() - 1;
        Debug.Log("Qnt de itens: " + posItemQnt.ToString());
        AtualizarHotbar();
    }

    private void Update()
    {
        if (SceneManager.GetActiveScene().name != "ModoMontagem")
        {
            Destroy(gameObject);
            return;
        }
        float scroll = Input.mouseScrollDelta.y; // ou Input.GetAxis("Mouse ScrollWheel")

        if (scroll > 0f)
        {
            // scroll pra cima
            posItemAtual--;
        }
        else if (scroll < 0f)
        {
            // scroll pra baixo
            posItemAtual++;
        }

        // faz a "meia volta" (wrap-around)
        posItemAtual = (posItemAtual + posItemQnt) % posItemQnt;
        AtualizarHotbar();
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
        Instance = null;
    }

    

    /// <summary>
    /// Chamado ao clicar no botão/ítem da peça na UI da caixa.
    /// Spawna a peça na mesa com física e fecha o menu.
    /// </summary>
    public void SpawnarPecaNaMesa(GameObject prefabPeca)
    {
        if (prefabPeca == null) return;

        // 1. Spawna a peça na posição da mesa (com rotação padrão ou do ponto)
        Vector3 posicaoSpawn = pontoSpawnMesa != null ? pontoSpawnMesa.position : Vector3.zero;
        Quaternion rotacaoSpawn = pontoSpawnMesa != null ? pontoSpawnMesa.rotation : Quaternion.identity;

        GameObject novaPeca = Instantiate(prefabPeca, posicaoSpawn, rotacaoSpawn);

        // 2. Garante que o Rigidbody está ativo com física caso precise
        Rigidbody rb = novaPeca.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false; // Permite que caia com física na mesa se desejar
        }

        // 3. Fecha a UI da caixa de peças
        if (painelCaixaUI != null)
        {
            painelCaixaUI.SetActive(false);
        }

        Debug.Log($"[UiController] Peça {prefabPeca.name} spawnada na mesa e UI fechada.");
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

    private void AtualizarHotbar()
    {
        imgSelecionado.position = posHotbarItens[posItemAtual].position;
        nomedoItemDisplay.text = nomeHotbarItens[posItemAtual];
        return;
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
        Destroy(StepChecker.Instance.gameObject);
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

    #endregion
    public TextMeshProUGUI textNameUI => nameText;
}
