using System;
using System.Collections;
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
    }

    private void Update()
    {
        if (SceneManager.GetActiveScene().name != "ModoMontagem")
        {
            Destroy(gameObject);
        }
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

    #endregion
    public TextMeshProUGUI textNameUI => nameText;
}
