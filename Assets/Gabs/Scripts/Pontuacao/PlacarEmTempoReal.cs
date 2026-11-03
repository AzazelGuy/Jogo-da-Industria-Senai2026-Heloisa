/*
 * ======================================================================================
 * SCRIPT: PlacarEmTempoReal.cs (Versao Integrada com o Gerenciador do Mascote)
 * PROJETO: Hyper Bytes
 * ======================================================================================
 */

using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class PlacarEmTempoReal : MonoBehaviour
{
    [Header("--- CONFIGURACAO DE PONTOS ---")]
    public int pontuacaoInicialDaFase = 1000;
    public int penalidadePorErro = 200;

    [Header("--- UI ---")]
    public TextMeshProUGUI textoPontuacao;
    public Color corNormal = Color.white;
    public Color corErro = Color.red;

    [Header("--- RETORNO AO ZERAR ---")]
    [Tooltip("Arraste aqui a cena do Tutorial da sua pasta de Assets.")]
    public Object cenaTutorial;

    [Header("--- PAINEL DE AVISO (FALHA / ZEROU) ---")]
    [Tooltip("Arraste aqui o GameObject Painel_Derrota.")]
    public GameObject painelAvisoZero;

    private int pontosDaFaseAtual;
    private FeedBackVisual feedbackVisual;
    private int ultimoErroRegistrado = 0;
    private bool jogoEncerradoPorZero = false;
    private bool aguardandoCliqueParaSair = false;
    private bool progressoSalvo = false;

    private void Start()
    {
        pontosDaFaseAtual = pontuacaoInicialDaFase;
        feedbackVisual = FindFirstObjectByType<FeedBackVisual>();

        if (textoPontuacao != null)
        {
            textoPontuacao.color = corNormal;
        }

        if (painelAvisoZero != null)
        {
            painelAvisoZero.SetActive(false);
        }

        AtualizarTextoUI();
    }

    private void Update()
    {
        if (jogoEncerradoPorZero)
        {
            // Assim que o painel estiver ativo, o jogador clica na tela para avancar ao tutorial
            if (aguardandoCliqueParaSair && (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)))
            {
                aguardandoCliqueParaSair = false;
                CarregarTutorialImediatamente();
            }
            return;
        }

        if (!progressoSalvo && GerenciadorBootEMascote.Instancia != null && GerenciadorBootEMascote.Instancia.pecaCertaRemovida)
        {
            progressoSalvo = true;
            SalvarProgressoDaFase();
        }

        if (feedbackVisual != null)
        {
            if (feedbackVisual.errosNestaFase > ultimoErroRegistrado)
            {
                ultimoErroRegistrado = feedbackVisual.errosNestaFase;

                int pontosCalculados = pontuacaoInicialDaFase - (feedbackVisual.errosNestaFase * penalidadePorErro);
                if (pontosCalculados < 0) pontosCalculados = 0;

                pontosDaFaseAtual = pontosCalculados;
                AtualizarTextoUI();
                VerificarSePontuacaoZerou();

                StopAllCoroutines();
                StartCoroutine(PiscarTextoVermelho());
            }
        }
    }

    private void AtualizarTextoUI()
    {
        if (textoPontuacao != null)
        {
            int pontosAnteriores = 0;
            if (GerenciadorPontuacaoGlobal.Instancia != null)
            {
                pontosAnteriores = GerenciadorPontuacaoGlobal.Instancia.ObterPontuacaoTotal();
            }

            int pontuacaoTotalExibida = pontosAnteriores + pontosDaFaseAtual;
            textoPontuacao.text = "Pontos: " + pontuacaoTotalExibida;
        }
    }

    private void VerificarSePontuacaoZerou()
    {
        int pontosAnteriores = 0;
        if (GerenciadorPontuacaoGlobal.Instancia != null)
        {
            pontosAnteriores = GerenciadorPontuacaoGlobal.Instancia.ObterPontuacaoTotal();
        }

        int pontuacaoTotalExibida = pontosAnteriores + pontosDaFaseAtual;

        if (pontuacaoTotalExibida <= 0 && !jogoEncerradoPorZero)
        {
            jogoEncerradoPorZero = true;

            if (GerenciadorPontuacaoGlobal.Instancia != null)
            {
                GerenciadorPontuacaoGlobal.Instancia.IniciarNovaSessao();
            }

            // Oculta a HUD com o fade padrao do jogo
            if (GerenciadorBootEMascote.Instancia != null)
            {
                GerenciadorBootEMascote.Instancia.OcultarHudComFade();
            }

            // Ativa o Painel de Derrota e garante opacidade total pelo CanvasGroup
            if (painelAvisoZero != null)
            {
                painelAvisoZero.SetActive(true);
                CanvasGroup group = painelAvisoZero.GetComponent<CanvasGroup>();
                if (group == null) group = painelAvisoZero.AddComponent<CanvasGroup>();
                group.alpha = 1f; // Opacidade 100% solida, sem transparencias
            }

            // Libera a escuta do clique para avancar
            aguardandoCliqueParaSair = true;
        }
    }

    private void CarregarTutorialImediatamente()
    {
        if (painelAvisoZero != null)
        {
            painelAvisoZero.SetActive(false);
        }

        // Carrega a cena do Tutorial configurada no Inspetor ou pelo nome
        if (cenaTutorial != null)
        {
            SceneManager.LoadScene(cenaTutorial.name);
        }
        else
        {
            SceneManager.LoadScene("Tutorial");
        }
    }

    public void SalvarProgressoDaFase()
    {
        if (GerenciadorPontuacaoGlobal.Instancia != null)
        {
            int errosFase = (feedbackVisual != null) ? feedbackVisual.errosNestaFase : 0;
            GerenciadorPontuacaoGlobal.Instancia.AdicionarProgressoDaFase(pontosDaFaseAtual, errosFase);
        }
    }

    private IEnumerator PiscarTextoVermelho()
    {
        if (textoPontuacao == null) yield break;

        for (int i = 0; i < 2; i++)
        {
            textoPontuacao.color = corErro;
            yield return new WaitForSeconds(0.15f);

            textoPontuacao.color = corNormal;
            yield return new WaitForSeconds(0.15f);
        }

        textoPontuacao.color = corNormal;
    }
}