using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class MenuPause : MonoBehaviour
{
    [Header("--- PAINEL E BOTÃO DE PAUSE ---")]
    public GameObject painelPause;
    public GameObject botaoAbrirPause;

    [Header("--- ELEMENTOS SEPARADOS (PARA ANIMAÇÃO) ---")]
    [Tooltip("Arraste aqui a imagem de fundo que receberá o Fade.")]
    public CanvasGroup fundoPauseCanvasGroup; // Usado para o fade do fundo
    public RectTransform fundoPause;
    public RectTransform tituloPause;
    public RectTransform botaoContinue;
    public RectTransform botaoMenuInicial;
    public RectTransform mascoteBetinho;

    [Header("--- CONFIGURAÇÕES DE ANIMAÇÃO ---")]
    public float duracaoElemento = 0.25f;
    public float duracaoFade = 0.2f;

    [Header("--- CONFIGURAÇÃO DE BALANÇO (ITENS) ---")]
    public float velocidadeBalanco = 2f;
    public float intensidadeBalanco = 0.02f;

    [Header("--- ONDE APLICAR O BALANÇO ---")]
    public bool balancarTitulo = true;
    public bool balancarContinue = true;
    public bool balancarMenuInicial = true;
    public bool balancarMascote = true;

    [Header("--- CONFIGURAÇÃO DE CENA ---")]
    public string nomeCenaMenu = "MenuInicial";

    private bool jogoPausado = false;
    private Coroutine corrotinaPainel;
    private Coroutine corrotinaBalanco;
    private RectTransform botaoHoverAtual;

    private void Awake()
    {
        if (painelPause != null)
        {
            // Garante que o fundo começa invisível e desativado
            if (fundoPauseCanvasGroup != null) fundoPauseCanvasGroup.alpha = 0f;
            painelPause.SetActive(false);
        }
    }

    private void Update()
    {
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
        Time.timeScale = 0f;

        if (botaoAbrirPause != null) botaoAbrirPause.SetActive(false);

        if (painelPause != null)
        {
            painelPause.SetActive(true);
            if (corrotinaPainel != null) StopCoroutine(corrotinaPainel);
            if (corrotinaBalanco != null) StopCoroutine(corrotinaBalanco);

            corrotinaPainel = StartCoroutine(AnimarAberturaSequencial());
        }
    }

    public void Continuar()
    {
        jogoPausado = false;
        Time.timeScale = 1f;

        if (botaoAbrirPause != null) botaoAbrirPause.SetActive(true);

        if (painelPause != null)
        {
            if (corrotinaPainel != null) StopCoroutine(corrotinaPainel);
            if (corrotinaBalanco != null) StopCoroutine(corrotinaBalanco);

            corrotinaPainel = StartCoroutine(AnimarFechamento());
        }
    }

    private IEnumerator AnimarAberturaSequencial()
    {
        ResetarEscalaElemento(tituloPause);
        ResetarEscalaElemento(botaoContinue);
        ResetarEscalaElemento(botaoMenuInicial);
        ResetarEscalaElemento(mascoteBetinho);

        // 1. Inicia o Fade In do Fundo junto com o surgimento dos itens
        if (fundoPauseCanvasGroup != null)
        {
            fundoPause.localScale = Vector3.one; // Fundo fica estático, sem balanço
            StartCoroutine(FadeCanvasGroup(fundoPauseCanvasGroup, 0f, 1f, duracaoFade));
        }

        // 2. Animação em cascata dos elementos
        yield return StartCoroutine(EfeitoPopUp(tituloPause));
        yield return StartCoroutine(EfeitoPopUp(botaoContinue));
        yield return StartCoroutine(EfeitoPopUp(botaoMenuInicial));
        yield return StartCoroutine(EfeitoPopUp(mascoteBetinho));

        // 3. Inicia o balanço contínuo apenas nos itens selecionados
        corrotinaBalanco = StartCoroutine(BalançarElementos());
    }

    private IEnumerator EfeitoPopUp(RectTransform elemento)
    {
        if (elemento == null) yield break;

        elemento.gameObject.SetActive(true);
        float tempo = 0f;
        Vector3 escalaInicial = Vector3.zero;
        Vector3 escalaFinal = Vector3.one;

        while (tempo < duracaoElemento)
        {
            tempo += Time.unscaledDeltaTime;
            float progresso = tempo / duracaoElemento;
            float escalaAtual = Mathf.Sin(progresso * Mathf.PI * 0.5f);
            elemento.localScale = Vector3.Lerp(escalaInicial, escalaFinal * 1.1f, escalaAtual);
            yield return null;
        }

        elemento.localScale = escalaFinal;
    }

    private IEnumerator BalançarElementos()
    {
        while (true)
        {
            float fator = Mathf.Sin(Time.unscaledTime * velocidadeBalanco) * intensidadeBalanco;

            if (balancarTitulo && tituloPause != null)
                tituloPause.localScale = new Vector3(1f + fator, 1f + fator, 1f);

            if (balancarContinue && botaoContinue != null)
            {
                float escalaAlvo = (botaoHoverAtual == botaoContinue) ? 1.15f : (1f + fator);
                botaoContinue.localScale = Vector3.Lerp(botaoContinue.localScale, new Vector3(escalaAlvo, escalaAlvo, 1f), Time.unscaledDeltaTime * 10f);
            }

            if (balancarMenuInicial && botaoMenuInicial != null)
            {
                float escalaAlvo = (botaoHoverAtual == botaoMenuInicial) ? 1.15f : (1f + fator);
                botaoMenuInicial.localScale = Vector3.Lerp(botaoMenuInicial.localScale, new Vector3(escalaAlvo, escalaAlvo, 1f), Time.unscaledDeltaTime * 10f);
            }

            if (balancarMascote && mascoteBetinho != null)
                mascoteBetinho.localScale = new Vector3(1f + fator, 1f + fator, 1f);

            yield return null;
        }
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float inicio, float fim, float duracao)
    {
        float tempo = 0f;
        while (tempo < duracao)
        {
            tempo += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(inicio, fim, tempo / duracao);
            yield return null;
        }
        cg.alpha = fim;
    }

    public void OnMouseEnterBotao(RectTransform botao)
    {
        botaoHoverAtual = botao;
    }

    public void OnMouseExitBotao()
    {
        botaoHoverAtual = null;
    }

    private void ResetarEscalaElemento(RectTransform elemento)
    {
        if (elemento != null)
        {
            elemento.localScale = Vector3.zero;
            elemento.gameObject.SetActive(false);
        }
    }

    private IEnumerator AnimarFechamento()
    {
        if (corrotinaBalanco != null) StopCoroutine(corrotinaBalanco);
        botaoHoverAtual = null;

        // Faz o Fade Out do fundo antes de desativar
        if (fundoPauseCanvasGroup != null)
        {
            yield return StartCoroutine(FadeCanvasGroup(fundoPauseCanvasGroup, 1f, 0f, duracaoFade));
        }

        if (painelPause != null)
        {
            painelPause.SetActive(false);
        }
    }

    public void IrParaMenuInicial()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(nomeCenaMenu);
    }
}