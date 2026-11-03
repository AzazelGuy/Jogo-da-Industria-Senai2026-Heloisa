using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadingScreen : MonoBehaviour
{
    public static LoadingScreen Instance;

    [Header("Referências")]
    [SerializeField] private CanvasGroup group;      // no PAINEL filho, não neste objeto
    [SerializeField] private RectTransform spinner;  // a imagem que gira

    [Header("Tempos")]
    [SerializeField] private float fadeDuration = 0.4f;
    [SerializeField] private float tempoLoading = 2f;

    [Header("Spinner")]
    [SerializeField] private float velocidadeGiro = 270f; // graus por segundo

    [SerializeField]

    private bool carregando;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        group.alpha = 0f;
        group.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>Uso: LoadingScreen.Carregar("ModoMontagem");</summary>
    public static void Carregar(string cena)
    {
        if (Instance == null)
        {
            Debug.LogWarning("LoadingScreen não existe na cena, carregando direto.");
            SceneManager.LoadScene(cena);
            return;
        }
        Instance.Iniciar(cena);
    }

    private void Iniciar(string cena)
    {
        if (carregando) return; // evita duplo clique
        StartCoroutine(Rotina(cena));
    }

    private IEnumerator Rotina(string cena)
    {
        carregando = true;
        Time.timeScale = 1f;
        spinner.localRotation = Quaternion.identity;

        // Fade in (bloqueia cliques no que tá atrás)
        group.gameObject.SetActive(true);
        group.blocksRaycasts = true;
        yield return Fade(0f, 1f);

        // Carrega em segundo plano, mas segura a ativação
        AsyncOperation op = SceneManager.LoadSceneAsync(cena);
        op.allowSceneActivation = false;

        float tempo = 0f;
        // 0.9 = cena pronta esperando ativação
        while (tempo < tempoLoading || op.progress < 0.9f)
        {
            tempo += Time.unscaledDeltaTime;
            spinner.Rotate(0f, 0f, -velocidadeGiro * Time.unscaledDeltaTime);
            yield return null;
        }

        // Tempo acabou: entra na cena de destino
        op.allowSceneActivation = true;
        while (!op.isDone)
        {
            spinner.Rotate(0f, 0f, -velocidadeGiro * Time.unscaledDeltaTime);
            yield return null;
        }

        // espera a cena nova "assentar" (Awake/Start pesados) antes de revelar
        yield return new WaitForSecondsRealtime(0.15f);

        yield return Fade(1f, 0f);

        group.blocksRaycasts = false;
        group.gameObject.SetActive(false);
        carregando = false;
    }

    private IEnumerator Fade(float de, float para)
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            // limita o delta pra um frame travado não pular o fade inteiro
            t += Mathf.Min(Time.unscaledDeltaTime, 0.033f);
            group.alpha = Mathf.Lerp(de, para, t / fadeDuration);
            yield return null;
        }
        group.alpha = para;
    }
}