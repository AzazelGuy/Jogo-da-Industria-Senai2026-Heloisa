using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GerenciadorTutorialPassos : MonoBehaviour
{
    [Header("Painéis dos passos")]
    [SerializeField] private GameObject painelPasso0;
    [SerializeField] private GameObject painelPasso1;
    [SerializeField] private GameObject painelPasso2;
    [SerializeField] private GameObject painelPasso3;
    [SerializeField] private GameObject painelPasso4;

    [Header("Fade das falas")]
    [SerializeField] private CanvasGroup[] canvasGroupPassos;
    [SerializeField, Min(0f)] private float duracaoFadeFala = 0.3f;

    [Header("Botão para avançar")]
    [SerializeField] private GameObject botaoProximoPasso;

    private int passoAtual;
    private bool estaEmTransicao;
    private bool tutorialConcluido;
    private Button botao;
    private CameraControllerMontagem cameraTutorial;

    private GameObject[] Paineis =>
        new[] { painelPasso0, painelPasso1, painelPasso2, painelPasso3, painelPasso4 };

    private void Awake()
    {
        Time.timeScale = 0f;
    }

    private void Start()
    {
        cameraTutorial = CameraControllerMontagem.instance;
        if (cameraTutorial != null)
        {
            cameraTutorial.StartTutorialTour();
        }
        else
        {
            Debug.LogWarning("[GerenciadorTutorialPassos] Não foi encontrado um CameraControllerMontagem ativo.", this);
        }

        if (botaoProximoPasso != null)
        {
            botao = botaoProximoPasso.GetComponent<Button>();
            if (botao != null)
            {
                botao.onClick.AddListener(AvancarPasso);
            }
            else
            {
                Debug.LogError("[GerenciadorTutorialPassos] O objeto do botão não possui um componente Button.", botaoProximoPasso);
            }
        }
        else
        {
            Debug.LogError("[GerenciadorTutorialPassos] O botão de avançar não foi atribuído.", this);
        }

        InicializarPassos();
    }

    private void OnDestroy()
    {
        if (botao != null)
        {
            botao.onClick.RemoveListener(AvancarPasso);
        }

        if (!tutorialConcluido)
        {
            Time.timeScale = 1f;
        }
    }

    public void AvancarPasso()
    {
        if (estaEmTransicao || tutorialConcluido) return;
        StartCoroutine(ExecutarTransicao());
    }

    private void InicializarPassos()
    {
        GameObject[] paineis = Paineis;
        for (int i = 0; i < paineis.Length; i++)
        {
            if (paineis[i] != null)
            {
                paineis[i].SetActive(i == 0);
            }

            if (canvasGroupPassos != null && i < canvasGroupPassos.Length && canvasGroupPassos[i] != null)
            {
                canvasGroupPassos[i].alpha = 0f;
            }
        }

        if (canvasGroupPassos != null && canvasGroupPassos.Length > 0 && canvasGroupPassos[0] != null)
        {
            StartCoroutine(FadeIn(canvasGroupPassos[0]));
        }
    }

    private IEnumerator ExecutarTransicao()
    {
        estaEmTransicao = true;
        GameObject[] paineis = Paineis;

        if (canvasGroupPassos != null && passoAtual < canvasGroupPassos.Length
            && canvasGroupPassos[passoAtual] != null)
        {
            yield return FadeOut(canvasGroupPassos[passoAtual]);
        }

        if (passoAtual < paineis.Length && paineis[passoAtual] != null)
        {
            paineis[passoAtual].SetActive(false);
        }

        if (passoAtual == paineis.Length - 1)
        {
            if (botaoProximoPasso != null)
            {
                botaoProximoPasso.SetActive(false);
            }

            if (cameraTutorial != null)
            {
                yield return cameraTutorial.FinishTutorialTour();
            }

            tutorialConcluido = true;
            Time.timeScale = 1f;
            estaEmTransicao = false;
            yield break;
        }

        passoAtual++;
        if (paineis[passoAtual] != null)
        {
            paineis[passoAtual].SetActive(true);
        }

        if (canvasGroupPassos != null && passoAtual < canvasGroupPassos.Length
            && canvasGroupPassos[passoAtual] != null)
        {
            yield return FadeIn(canvasGroupPassos[passoAtual]);
        }

        Time.timeScale = 0f;
        estaEmTransicao = false;
    }

    private IEnumerator FadeIn(CanvasGroup group)
    {
        Time.timeScale = 0f;
        group.alpha = 0f;
        float tempo = 0f;

        while (tempo < duracaoFadeFala)
        {
            tempo += Time.unscaledDeltaTime;
            group.alpha = Mathf.Clamp01(tempo / duracaoFadeFala);
            yield return null;
        }

        group.alpha = 1f;
    }

    private IEnumerator FadeOut(CanvasGroup group)
    {
        float alphaInicial = group.alpha;
        float tempo = 0f;

        while (tempo < duracaoFadeFala)
        {
            tempo += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(alphaInicial, 0f, Mathf.Clamp01(tempo / duracaoFadeFala));
            yield return null;
        }

        group.alpha = 0f;
    }
}
