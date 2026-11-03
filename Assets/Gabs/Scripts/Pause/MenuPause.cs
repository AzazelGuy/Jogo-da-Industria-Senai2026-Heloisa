using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class MenuPause : MonoBehaviour
{
    [Header("--- PAINEL E BOTÃO DE PAUSE ---")]
    [Tooltip("Arraste aqui o GameObject do painel de pause da UI.")]
    public GameObject painelPause;

    [Tooltip("Arraste aqui o botão que abre o pause (para ele sumir quando o painel abrir).")]
    public GameObject botaoAbrirPause;

    [Header("--- CONFIGURAÇÃO DE FADE ---")]
    [Tooltip("Duração do efeito de fade ao abrir/fechar.")]
    public float duracaoFade = 0.2f;

    [Header("--- CONFIGURAÇÃO DE CENA ---")]
    [Tooltip("Digite exatamente o nome da cena do seu Menu Inicial.")]
    public string nomeCenaMenu = "MenuInicial";

    private bool jogoPausado = false;
    private CanvasGroup canvasGroupPause;
    private Coroutine corrotinaFade;

    private void Awake()
    {
        if (painelPause != null)
        {
            canvasGroupPause = painelPause.GetComponent<CanvasGroup>();
            if (canvasGroupPause == null)
            {
                canvasGroupPause = painelPause.AddComponent<CanvasGroup>();
            }
            canvasGroupPause.alpha = 0f;
            painelPause.SetActive(false);
        }
    }

    private void Update()
    {
        // Pressionar TAB para pausar/despausar
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (jogoPausado)
            {
                Continuar();
            }
            else
            {
                Pausar();
            }
        }
    }

    public void Pausar()
    {
        jogoPausado = true;
        Time.timeScale = 0f; // Congela o tempo

        // Esconde o botão de abrir o pause
        if (botaoAbrirPause != null)
        {
            botaoAbrirPause.SetActive(false);
        }

        if (painelPause != null)
        {
            painelPause.SetActive(true);
            if (corrotinaFade != null) StopCoroutine(corrotinaFade);
            corrotinaFade = StartCoroutine(RotinaFade(1f));
        }
    }

    public void Continuar()
    {
        jogoPausado = false;
        Time.timeScale = 1f; // Retorna o tempo normal

        // Reativa o botão de abrir o pause
        if (botaoAbrirPause != null)
        {
            botaoAbrirPause.SetActive(true);
        }

        if (painelPause != null)
        {
            if (corrotinaFade != null) StopCoroutine(corrotinaFade);
            corrotinaFade = StartCoroutine(RotinaFade(0f, true));
        }
    }

    private IEnumerator RotinaFade(float alfaAlvo, bool desativarAposFim = false)
    {
        float alfaInicial = canvasGroupPause.alpha;
        float tempo = 0f;

        while (tempo < duracaoFade)
        {
            tempo += Time.unscaledDeltaTime;
            canvasGroupPause.alpha = Mathf.Lerp(alfaInicial, alfaAlvo, tempo / duracaoFade);
            yield return null;
        }

        canvasGroupPause.alpha = alfaAlvo;

        if (desativarAposFim)
        {
            painelPause.SetActive(false);
        }
    }

    public void IrParaMenuInicial()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(nomeCenaMenu);
    }

    public void SairDoJogo()
    {
        Debug.Log("Saindo do jogo...");
        Application.Quit();
    }
}