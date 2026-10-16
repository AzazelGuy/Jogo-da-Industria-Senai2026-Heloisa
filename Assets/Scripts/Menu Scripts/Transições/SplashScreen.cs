using UnityEngine;
using System.Collections;

public class SplashScreen : MonoBehaviour
{
    [Header("Fundo da Intro")]
    public CanvasGroup fundoPreto;        // Canvas Group do FundoPreto

    [Header("Logos Individuais")]
    public CanvasGroup logoEstudio;      // Canvas Group da Logo_Estudio
    public CanvasGroup logoSesi;         // Canvas Group da Logo_SESI

    [Header("Menu Inicial")]
    public CanvasGroup menuInicialGroup; // Canvas Group do MenuInicial
    public RectTransform menuInicialRect; // RectTransform do MenuInicial

    [Header("Tempos (Segundos)")]
    public float tempoFade = 1f;
    public float tempoExibicao = 2f;
    public float tempoPausa = 0.5f;

    [Header("Pular")]
    public float tempoFadePulo = 0.35f;
    private bool pulando = false;

    [Header("Posição de Entrada")]
    public float posicaoInicialY = -1080f;

    private bool introFinalizada = false;

    void Start()
    {
        // Prepara os estados iniciais da UI
        if (fundoPreto != null)
        {
            fundoPreto.alpha = 1f;
            fundoPreto.gameObject.SetActive(true);
        }

        if (logoEstudio != null)
        {
            logoEstudio.alpha = 0f;
            logoEstudio.gameObject.SetActive(true);
        }

        if (logoSesi != null)
        {
            logoSesi.alpha = 0f;
            logoSesi.gameObject.SetActive(true);
        }

        if (menuInicialGroup != null && menuInicialRect != null)
        {
            menuInicialGroup.alpha = 0f;
            menuInicialRect.anchoredPosition = new Vector2(0, posicaoInicialY);
        }

        StartCoroutine(ExecutarSequenciaIntro());
    }

    IEnumerator ExecutarSequenciaIntro()
    {
        // --- 1. ETAPA LOGO ESTÚDIO ---
        yield return StartCoroutine(FadeObjeto(logoEstudio, 0f, 1f, tempoFade));
        yield return new WaitForSeconds(tempoExibicao);
        yield return StartCoroutine(FadeObjeto(logoEstudio, 1f, 0f, tempoFade));

        if (logoEstudio != null) logoEstudio.gameObject.SetActive(false);
        yield return new WaitForSeconds(tempoPausa);

        // --- 2. ETAPA LOGO SESI ---
        yield return StartCoroutine(FadeObjeto(logoSesi, 0f, 1f, tempoFade));
        yield return new WaitForSeconds(tempoExibicao);
        yield return StartCoroutine(FadeObjeto(logoSesi, 1f, 0f, tempoFade));

        if (logoSesi != null) logoSesi.gameObject.SetActive(false);
        yield return new WaitForSeconds(tempoPausa);

        // --- 3. ETAPA MENU INICIAL ---
        yield return StartCoroutine(TransicaoParaMenu(tempoFade));
    }

    /// <summary>
    /// Pula as logos e vai direto pro menu com um fade rápido.
    /// </summary>
    public void Pular()
    {
        if (pulando || introFinalizada) return;
        pulando = true;

        // Mata a sequência E os FadeObjeto filhos
        StopAllCoroutines();

        if (logoEstudio != null) logoEstudio.gameObject.SetActive(false);
        if (logoSesi != null) logoSesi.gameObject.SetActive(false);

        StartCoroutine(TransicaoParaMenu(tempoFadePulo));
    }

    IEnumerator TransicaoParaMenu(float duracao)
    {
        // Parte dos valores ATUAIS, então funciona mesmo se pular no meio da subida
        float fundoInicial = fundoPreto != null ? fundoPreto.alpha : 0f;
        float menuAlphaInicial = menuInicialGroup != null ? menuInicialGroup.alpha : 0f;
        Vector2 posInicial = menuInicialRect != null ? menuInicialRect.anchoredPosition : Vector2.zero;
        Vector2 posFinal = Vector2.zero;

        float tempo = 0f;
        while (tempo < duracao)
        {
            tempo += Time.deltaTime;
            float progresso = Mathf.Clamp01(tempo / duracao);

            if (fundoPreto != null) fundoPreto.alpha = Mathf.Lerp(fundoInicial, 0f, progresso);
            if (menuInicialGroup != null) menuInicialGroup.alpha = Mathf.Lerp(menuAlphaInicial, 1f, progresso);
            if (menuInicialRect != null) menuInicialRect.anchoredPosition = Vector2.Lerp(posInicial, posFinal, progresso);

            yield return null;
        }

        // Finalização da intro
        if (fundoPreto != null)
        {
            fundoPreto.alpha = 0f;
            fundoPreto.gameObject.SetActive(false);
        }

        Transform parentLogos = logoEstudio != null ? logoEstudio.transform.parent : null;
        if (parentLogos != null) parentLogos.gameObject.SetActive(false);

        if (menuInicialGroup != null && menuInicialRect != null)
        {
            menuInicialGroup.alpha = 1f;
            menuInicialRect.anchoredPosition = posFinal;
        }

        introFinalizada = true;
    }

    public bool EstaEmAndamento()
    {
        return !introFinalizada;
    }

    IEnumerator FadeObjeto(CanvasGroup objeto, float inicio, float fim, float duracao)
    {
        if (objeto == null) yield break;

        float tempo = 0f;
        while (tempo < duracao)
        {
            tempo += Time.deltaTime;
            objeto.alpha = Mathf.Lerp(inicio, fim, tempo / duracao);
            yield return null;
        }
        objeto.alpha = fim;
    }
}