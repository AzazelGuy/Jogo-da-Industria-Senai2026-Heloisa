/*
 * ======================================================================================
 * SCRIPT: GerenciadorTransicaoFase.cs (Corrigido)
 * PROJETO: Hyper Bytes
 * ======================================================================================
 */

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GerenciadorTransicaoFase : MonoBehaviour
{
    [Header("--- NOME DA PROXIMA FASE ---")]
    [Tooltip("Digite exatamente o nome da cena da proxima fase (ex: Fase02 ou MenuInicial).")]
    public string nomeProximaFase;

    [Header("--- MONITORAMENTO DE VITORIA ---")]
    [Tooltip("Arraste o objeto da fala de vitoria do Betinho que fica na UI.")]
    public GameObject painelFalaVitoria;

    [Header("--- BOTAO DE PROXIMA FASE ---")]
    [Tooltip("Arraste aqui o componente Button da UI que leva para a proxima fase.")]
    public Button botaoProximaFase;

    private bool painelFoiAtivado = false;

    private void Start()
    {
        // CORRECAO: Reseta o estado ao iniciar a cena para evitar que venha travado da fase anterior
        painelFoiAtivado = false;

        // Garante que o botao comeca desativado e invisivel ao iniciar a fase
        if (botaoProximaFase != null)
        {
            botaoProximaFase.gameObject.SetActive(false);

            // Remove qualquer listener antigo para evitar duplicacao de cliques
            botaoProximaFase.onClick.RemoveAllListeners();

            // Adiciona a funcao de clique ao botao via codigo de forma segura
            botaoProximaFase.onClick.AddListener(AvancarParaProximaFase);
        }

        // Se por acaso o painel de vitoria ja estiver ativo por engano ao iniciar, esconde ele
        if (painelFalaVitoria != null && painelFalaVitoria.activeSelf)
        {
            painelFalaVitoria.SetActive(false);
        }
    }

    private void Update()
    {
        // Monitora se o painel de vitoria foi ativado na tela e se ainda nao ativamos o botao
        if (!painelFoiAtivado && painelFalaVitoria != null && painelFalaVitoria.activeSelf)
        {
            painelFoiAtivado = true;

            // Ativa o botao de ir para a proxima fase na tela
            if (botaoProximaFase != null)
            {
                botaoProximaFase.gameObject.SetActive(true);
            }
        }
    }

    // Metodo chamado automaticamente quando o jogador clica no botao
    public void AvancarParaProximaFase()
    {
        if (!string.IsNullOrEmpty(nomeProximaFase))
        {
            SceneManager.LoadScene(nomeProximaFase);
        }
        else
        {
            Debug.LogWarning("O nome da proxima fase nao foi configurado no Inspector do Gerenciador de Transicao!");
        }
    }
}