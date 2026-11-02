/*
 * ======================================================================================
 * SCRIPT: FeedBackVisual.cs (Atualizado com Contagem de Erros)
 * PROJETO: Hyper Bytes
 * ======================================================================================
 */

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FeedBackVisual : MonoBehaviour
{
    [Header("--- CONFIGURACAO DO ALERTA DE ERRO (VERMELHO) ---")]
    public Image painelVermelho;
    [Range(0.1f, 1f)] public float intensidadeVermelho = 0.4f;
    public Button[] botoesErro;

    [Header("--- CONFIGURACAO DO ALERTA DE ACERTO (VERDE) ---")]
    public Image painelVerde;
    [Range(0.1f, 1f)] public float intensidadeVerde = 0.4f;
    public Button[] botoesAcerto;

    [Header("--- PARAMETROS GERAIS DE VELOCIDADE ---")]
    public float velocidadePiscar = 7f;

    // Variavel para contar os erros cometidos nesta fase especifica
    [HideInInspector] public int errosNestaFase = 0;

    private Coroutine corrotinaAtual;

    private void Start()
    {
        if (painelVermelho != null)
        {
            Color cor = painelVermelho.color;
            cor.a = 0f;
            painelVermelho.color = cor;
            painelVermelho.gameObject.SetActive(false);
        }

        if (painelVerde != null)
        {
            Color cor = painelVerde.color;
            cor.a = 0f;
            painelVerde.color = cor;
            painelVerde.gameObject.SetActive(false);
        }

        if (botoesErro != null)
        {
            foreach (Button botao in botoesErro)
            {
                if (botao != null)
                {
                    botao.onClick.AddListener(() => DispararErro());
                }
            }
        }

        if (botoesAcerto != null)
        {
            foreach (Button botao in botoesAcerto)
            {
                if (botao != null)
                {
                    botao.onClick.AddListener(() => TentarClicarBotaoAcerto());
                }
            }
        }
    }

    private void TentarClicarBotaoAcerto()
    {
        if (GerenciadorBootEMascote.Instancia != null && GerenciadorBootEMascote.Instancia.pcEstaLigado)
        {
            DispararErro();
            GerenciadorBootEMascote.Instancia.TentarInteragirComPeca();
        }
        else
        {
            DispararAcerto();
        }
    }

    // SEMPRE QUE A TELA VERMELHA PISCA, O ERRO E REGISTRADO AQUI!
    public void DispararErro()
    {
        errosNestaFase++; // Incrementa o erro da fase atual

        if (painelVermelho == null) return;

        if (corrotinaAtual != null)
        {
            StopCoroutine(corrotinaAtual);
        }

        corrotinaAtual = StartCoroutine(RotinaPiscarVermelhoDuasVezes());
    }

    public void DispararAcerto()
    {
        if (painelVerde == null) return;

        if (corrotinaAtual != null)
        {
            StopCoroutine(corrotinaAtual);
        }

        corrotinaAtual = StartCoroutine(RotinaPiscarVerdeDuasVezes());
    }

    private IEnumerator RotinaPiscarVermelhoDuasVezes()
    {
        painelVermelho.gameObject.SetActive(true);
        Color cor = painelVermelho.color;

        for (int i = 0; i < 2; i++)
        {
            cor.a = 0f;
            painelVermelho.color = cor;

            while (cor.a < intensidadeVermelho)
            {
                cor.a += Time.unscaledDeltaTime * velocidadePiscar;
                painelVermelho.color = cor;
                yield return null;
            }

            while (cor.a > 0f)
            {
                cor.a -= Time.unscaledDeltaTime * velocidadePiscar;
                painelVermelho.color = cor;
                yield return null;
            }
        }

        cor.a = 0f;
        painelVermelho.color = cor;
        painelVermelho.gameObject.SetActive(false);
    }

    private IEnumerator RotinaPiscarVerdeDuasVezes()
    {
        painelVerde.gameObject.SetActive(true);
        Color cor = painelVerde.color;

        for (int i = 0; i < 2; i++)
        {
            cor.a = 0f;
            painelVerde.color = cor;

            while (cor.a < intensidadeVerde)
            {
                cor.a += Time.unscaledDeltaTime * velocidadePiscar;
                painelVerde.color = cor;
                yield return null;
            }

            while (cor.a > 0f)
            {
                cor.a -= Time.unscaledDeltaTime * velocidadePiscar;
                painelVerde.color = cor;
                yield return null;
            }
        }

        cor.a = 0f;
        painelVerde.color = cor;
        painelVerde.gameObject.SetActive(false);
    }
}