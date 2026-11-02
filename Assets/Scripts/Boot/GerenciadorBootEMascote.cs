/*
 * ======================================================================================
 * SCRIPT: GerenciadorBootEMascote.cs (Corrigido para manter a fala de vitória estática)
 * PROJETO: Hyper Bytes
 * ======================================================================================
 */

using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GerenciadorBootEMascote : MonoBehaviour
{
    public static GerenciadorBootEMascote Instancia;

    [Header("--- ESTADO DO PC ---")]
    public bool pcEstaLigado = true;
    public bool pecaCertaRemovida = false;

    [Header("--- BOTÕES DE BOOT (UI) ---")]
    public GameObject botaoBootVerde;
    public GameObject botaoBootVermelho;

    [Header("--- HUD DA FASE ---")]
    [Tooltip("Arraste aqui o GameObject ContainerHud.")]
    public GameObject containerHud;
    [Tooltip("Duração do Fade da HUD.")]
    public float duracaoFadeHud = 0.3f;

    [Header("--- CONFIGURAÇÕES DE ANIMAÇÃO ---")]
    public float intensidadePulso = 1.15f;
    public float duracaoPulso = 0.1f;

    [Header("--- PAINÉIS DE FALA DO BETINHO ---")]
    public GameObject falaPcLigado;
    public GameObject falaPecaErrada;
    public GameObject falaOrdemPeca;
    public GameObject falaVitoria;

    [Header("--- TEMPO E FADES DA FALA ---")]
    public float duracaoFadeIn = 0.3f;
    public float tempoExibicaoFala = 8.0f;
    public float duracaoFadeOut = 1.0f;

    private bool animandoBoot = false;
    private bool mouseCongeladoVitoria = false;

    private Coroutine corrotinaFalaAtual;
    private Coroutine corrotinaHudAtual;
    private GameObject painelFalaAtivo;
    private CanvasGroup canvasGroupHud;

    private void Awake()
    {
        Instancia = this;
    }

    private void Start()
    {
        ConfigurarHudCanvasGroup();
        EsconderTodasAsFalas();
        AtualizarVisualBotoes();
        mouseCongeladoVitoria = false;

        if (containerHud != null)
        {
            containerHud.SetActive(true);
            if (canvasGroupHud != null) canvasGroupHud.alpha = 1f;
        }
    }

    private void ConfigurarHudCanvasGroup()
    {
        if (containerHud != null)
        {
            canvasGroupHud = containerHud.GetComponent<CanvasGroup>();
            if (canvasGroupHud == null)
            {
                canvasGroupHud = containerHud.AddComponent<CanvasGroup>();
            }
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            ReiniciarFase();
        }

        if (mouseCongeladoVitoria) return;

        if (painelFalaAtivo != null && (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)))
        {
            IniciarFadeOutImediato();
        }
    }

    public void ReiniciarFase()
    {
        Debug.Log("Reiniciando a fase...");
        Scene sceneAtual = SceneManager.GetActiveScene();
        SceneManager.LoadScene(sceneAtual.name);
    }

    public void ClicarNoBotaoBoot()
    {
        if (animandoBoot || mouseCongeladoVitoria) return;
        StartCoroutine(AnimarPulsoEProcessarBoot());
    }

    private IEnumerator AnimarPulsoEProcessarBoot()
    {
        animandoBoot = true;
        EsconderTodasAsFalas();

        GameObject botaoAtivo = pcEstaLigado ? botaoBootVerde : botaoBootVermelho;

        if (botaoAtivo != null)
        {
            Vector3 escalaOriginal = botaoAtivo.transform.localScale;
            Vector3 escalaMaior = escalaOriginal * intensidadePulso;

            float t = 0;
            while (t < duracaoPulso) { t += Time.deltaTime; botaoAtivo.transform.localScale = Vector3.Lerp(escalaOriginal, escalaMaior, t / duracaoPulso); yield return null; }
            t = 0;
            while (t < duracaoPulso) { t += Time.deltaTime; botaoAtivo.transform.localScale = Vector3.Lerp(escalaMaior, escalaOriginal, t / duracaoPulso); yield return null; }
            botaoAtivo.transform.localScale = escalaOriginal;
        }

        pcEstaLigado = !pcEstaLigado;
        AtualizarVisualBotoes();
        animandoBoot = false;
    }

    private void AtualizarVisualBotoes()
    {
        if (botaoBootVerde != null) botaoBootVerde.SetActive(pcEstaLigado);
        if (botaoBootVermelho != null) botaoBootVermelho.SetActive(!pcEstaLigado);
    }

    public bool TentarInteragirComPeca()
    {
        if (mouseCongeladoVitoria) return false;
        ExibirFala(falaPcLigado);
        return false;
    }

    public void MostrarAvisoOrdemIncorreta()
    {
        if (mouseCongeladoVitoria) return;
        ExibirFala(falaOrdemPeca);
    }

    public void VencerFase()
    {
        pecaCertaRemovida = true;
        mouseCongeladoVitoria = true;
        OcultarHudComFade();
        ExibirFala(falaVitoria);
    }

    public void ValidarPecaRemovida(bool ehAPecaComDefeito)
    {
        if (mouseCongeladoVitoria) return;

        if (ehAPecaComDefeito)
        {
            VencerFase();
        }
        else
        {
            OcultarHudComFade();
            ExibirFala(falaPecaErrada);
        }
    }

    public void OcultarHudComFade()
    {
        if (containerHud == null) return;
        if (corrotinaHudAtual != null) StopCoroutine(corrotinaHudAtual);
        corrotinaHudAtual = StartCoroutine(RotinaFadeHud(false));
    }

    public void MostrarHudComFade()
    {
        if (containerHud == null || mouseCongeladoVitoria) return;
        if (corrotinaHudAtual != null) StopCoroutine(corrotinaHudAtual);
        corrotinaHudAtual = StartCoroutine(RotinaFadeHud(true));
    }

    private IEnumerator RotinaFadeHud(bool mostrar)
    {
        if (canvasGroupHud == null) ConfigurarHudCanvasGroup();
        if (containerHud == null) yield break;

        if (mostrar) containerHud.SetActive(true);

        float alvoAlpha = mostrar ? 1f : 0f;
        float alphaInicial = canvasGroupHud.alpha;
        float tempo = 0f;

        while (tempo < duracaoFadeHud)
        {
            tempo += Time.deltaTime;
            canvasGroupHud.alpha = Mathf.Lerp(alphaInicial, alvoAlpha, tempo / duracaoFadeHud);
            yield return null;
        }

        canvasGroupHud.alpha = alvoAlpha;

        if (!mostrar)
        {
            containerHud.SetActive(false);
        }
    }

    private void ExibirFala(GameObject painel)
    {
        EsconderTodasAsFalas();

        if (painel == null) return;

        OcultarHudComFade();

        painelFalaAtivo = painel;
        painelFalaAtivo.SetActive(true);

        CanvasGroup group = painelFalaAtivo.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = painelFalaAtivo.AddComponent<CanvasGroup>();
        }

        group.alpha = 0f;
        corrotinaFalaAtual = StartCoroutine(RotinaCompletaFade(painelFalaAtivo, group));
    }

    private IEnumerator RotinaCompletaFade(GameObject painel, CanvasGroup group)
    {
        float tempoDecorrido = 0f;
        while (tempoDecorrido < duracaoFadeIn)
        {
            tempoDecorrido += Time.deltaTime;
            group.alpha = Mathf.Lerp(0f, 1f, tempoDecorrido / duracaoFadeIn);
            yield return null;
        }
        group.alpha = 1f;

        // ALTERAÇÃO: Se o painel ativo for o de vitória, ele trava aqui e NÃO some sozinho!
        if (painel == falaVitoria)
        {
            yield break; // Encerra a corrotina aqui, mantendo a fala de vitória estática na tela
        }

        // Demais falas continuam aguardando o tempo normal para sumir
        yield return new WaitForSeconds(tempoExibicaoFala);

        yield return StartCoroutine(ExecutarFadeOut(painel, group));
    }

    private void IniciarFadeOutImediato()
    {
        if (corrotinaFalaAtual != null)
        {
            StopCoroutine(corrotinaFalaAtual);
            corrotinaFalaAtual = null;
        }

        if (painelFalaAtivo != null)
        {
            CanvasGroup group = painelFalaAtivo.GetComponent<CanvasGroup>();
            if (group != null)
            {
                StartCoroutine(ExecutarFadeOut(painelFalaAtivo, group));
            }
            else
            {
                painelFalaAtivo.SetActive(false);
                painelFalaAtivo = null;
                MostrarHudComFade();
            }
        }
    }

    private IEnumerator ExecutarFadeOut(GameObject painel, CanvasGroup group)
    {
        float tempoDecorrido = 0f;
        float alphaInicial = group.alpha;

        while (tempoDecorrido < duracaoFadeOut)
        {
            tempoDecorrido += Time.deltaTime;
            group.alpha = Mathf.Lerp(alphaInicial, 0f, tempoDecorrido / duracaoFadeOut);
            yield return null;
        }

        group.alpha = 0f;
        painel.SetActive(false);

        if (painelFalaAtivo == painel)
        {
            painelFalaAtivo = null;
        }

        if (!pecaCertaRemovida)
        {
            MostrarHudComFade();
        }
    }

    public void EsconderTodasAsFalas()
    {
        if (corrotinaFalaAtual != null)
        {
            StopCoroutine(corrotinaFalaAtual);
            corrotinaFalaAtual = null;
        }

        DesativarPainel(falaPcLigado);
        DesativarPainel(falaPecaErrada);
        DesativarPainel(falaOrdemPeca);
        DesativarPainel(falaVitoria);

        painelFalaAtivo = null;

        if (!pecaCertaRemovida)
        {
            MostrarHudComFade();
        }
    }

    private void DesativarPainel(GameObject painel)
    {
        if (painel != null)
        {
            CanvasGroup group = painel.GetComponent<CanvasGroup>();
            if (group != null)
            {
                group.alpha = 0f;
            }
            painel.SetActive(false);
        }
    }
}