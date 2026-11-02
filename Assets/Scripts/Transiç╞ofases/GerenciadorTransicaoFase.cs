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
    [Header("--- NOME DA PRÓXIMA FASE ---")]
    [Tooltip("Digite exatamente o nome da cena da próxima fase (ex: Fase02 ou MenuInicial).")]
    public string nomeProximaFase;

    [Header("--- MONITORAMENTO DE VITÓRIA ---")]
    [Tooltip("Arraste o objeto da fala de vitória do Betinho que fica na UI.")]
    public GameObject painelFalaVitoria;

    [Header("--- BOTÃO DE PRÓXIMA FASE ---")]
    [Tooltip("Arraste aqui o componente Button da UI que leva para a próxima fase.")]
    public Button botaoProximaFase;

    private bool painelFoiAtivado = false;

    private void Start()
    {
        // CORREÇÃO: Reseta o estado ao iniciar a cena para evitar que venha travado da fase anterior
        painelFoiAtivado = false;

        // Garante que o botão começa desativado e invisível ao iniciar a fase
        if (botaoProximaFase != null)
        {
            botaoProximaFase.gameObject.SetActive(false);

            // Remove qualquer listener antigo para evitar duplicação de cliques
            botaoProximaFase.onClick.RemoveAllListeners();

            // Adiciona a função de clique ao botão via código de forma segura
            botaoProximaFase.onClick.AddListener(AvancarParaProximaFase);
        }

        // Se por acaso o painel de vitória já estiver ativo por engano ao iniciar, esconde ele
        if (painelFalaVitoria != null && painelFalaVitoria.activeSelf)
        {
            painelFalaVitoria.SetActive(false);
        }
    }

    private void Update()
    {
        // Monitora se o painel de vitória foi ativado na tela e se ainda não ativamos o botão
        if (!painelFoiAtivado && painelFalaVitoria != null && painelFalaVitoria.activeSelf)
        {
            painelFoiAtivado = true;

            // Ativa o botão de ir para a próxima fase na tela
            if (botaoProximaFase != null)
            {
                botaoProximaFase.gameObject.SetActive(true);
            }
        }
    }

    // Método chamado automaticamente quando o jogador clica no botão
    public void AvancarParaProximaFase()
    {
        if (!string.IsNullOrEmpty(nomeProximaFase))
        {
            SceneManager.LoadScene(nomeProximaFase);
        }
        else
        {
            Debug.LogWarning("O nome da próxima fase não foi configurado no Inspector do Gerenciador de Transição!");
        }
    }
}