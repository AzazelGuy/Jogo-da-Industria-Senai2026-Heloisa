using System.Collections;
using UnityEngine;

public class MenuInventario : MonoBehaviour
{
    [Header("Referencias da UI")]
    public CanvasGroup fundoEscuro;
    public CanvasGroup painelFundoTela;

    [Header("Configuracoes de Animacao")]
    public float duracaoAnimacao = 0.25f;

    private bool estaAberto = false;
    private Coroutine coroutineAnimacao;

    private void Start()
    {
        // Garante que ambos comecem invisiveis e sem bloquear cliques
        ConfigurarEstadoInicial(fundoEscuro);
        ConfigurarEstadoInicial(painelFundoTela);
    }

    private void Update()
    {
        // Fecha o menu se a tecla ESC for pressionada e o menu estiver aberto
        if (estaAberto && Input.GetKeyDown(KeyCode.Escape))
        {
            AlternarTela();
        }
    }

    public void AlternarTela()
    {
        estaAberto = !estaAberto;

        if (coroutineAnimacao != null)
        {
            StopCoroutine(coroutineAnimacao);
        }

        coroutineAnimacao = StartCoroutine(AnimarMenu(estaAberto));
    }

    private IEnumerator AnimarMenu(bool abrir)
    {
        float tempoAtual = 0f;

        float alphaInicialFundo = fundoEscuro != null ? fundoEscuro.alpha : 0f;
        float alphaInicialPainel = painelFundoTela != null ? painelFundoTela.alpha : 0f;

        float alphaFinal = abrir ? 1f : 0f;

        // Habilita interações imediatamente se estiver abrindo
        if (abrir)
        {
            SetCanvasGroupState(fundoEscuro, true);
            SetCanvasGroupState(painelFundoTela, true);
        }

        while (tempoAtual < duracaoAnimacao)
        {
            tempoAtual += Time.deltaTime;
            float progresso = tempoAtual / duracaoAnimacao;

            if (fundoEscuro != null)
                fundoEscuro.alpha = Mathf.Lerp(alphaInicialFundo, alphaFinal, progresso);

            if (painelFundoTela != null)
                painelFundoTela.alpha = Mathf.Lerp(alphaInicialPainel, alphaFinal, progresso);

            yield return null;
        }

        // Garante o valor final cravado
        if (fundoEscuro != null) fundoEscuro.alpha = alphaFinal;
        if (painelFundoTela != null) painelFundoTela.alpha = alphaFinal;

        // Se estiver fechando, desabilita a interatividade/bloqueio de cliques
        if (!abrir)
        {
            SetCanvasGroupState(fundoEscuro, false);
            SetCanvasGroupState(painelFundoTela, false);
        }
    }

    private void ConfigurarEstadoInicial(CanvasGroup cg)
    {
        if (cg != null)
        {
            cg.alpha = 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }
    }

    private void SetCanvasGroupState(CanvasGroup cg, bool ativo)
    {
        if (cg != null)
        {
            cg.interactable = ativo;
            cg.blocksRaycasts = ativo;
        }
    }
}