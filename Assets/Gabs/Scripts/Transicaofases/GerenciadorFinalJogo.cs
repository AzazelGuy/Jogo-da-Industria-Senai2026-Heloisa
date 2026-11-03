/*
 * ======================================================================================
 * SCRIPT: GerenciadorFinalJogo.cs
 * PROJETO: Hyper Bytes
 * ======================================================================================
 */

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GerenciadorFinalJogo : MonoBehaviour
{
    [Header("--- NOME DA CENA DO MENU ---")]
    [Tooltip("Digite exatamente o nome da cena do Menu Inicial (ex: MenuInicial ou Menu).")]
    public string nomeCenaMenu = "MenuInicial";

    [Header("--- MONITORAMENTO DE VITORIA FINAL ---")]
    [Tooltip("Arraste aqui o objeto da fala de vitoria da fase final do Betinho.")]
    public GameObject painelFalaVitoriaFinal;

    [Header("--- BOTAO DE RETORNAR AO MENU ---")]
    [Tooltip("Arraste aqui o componente Button da UI que leva de volta ao Menu Inicial.")]
    public Button botaoMenuInicial;

    private bool painelFoiAtivado = false;

    private void Start()
    {
        // Garante que o botao comeca desativado ao iniciar a fase
        if (botaoMenuInicial != null)
        {
            botaoMenuInicial.gameObject.SetActive(false);

            // Limpa listeners antigos para seguranca e vincula a acao de retorno
            botaoMenuInicial.onClick.RemoveAllListeners();
            botaoMenuInicial.onClick.AddListener(RetornarAoMenu);
        }
    }

    private void Update()
    {
        // Monitora quando a fala de vitoria final aparece na tela
        if (!painelFoiAtivado && painelFalaVitoriaFinal != null && painelFalaVitoriaFinal.activeSelf)
        {
            painelFoiAtivado = true;

            // Ativa o botao para voltar ao Menu Inicial
            if (botaoMenuInicial != null)
            {
                botaoMenuInicial.gameObject.SetActive(true);
            }
        }
    }

    // Metodo chamado ao clicar no botao
    public void RetornarAoMenu()
    {
        // Reinicia a sessao de pontuacao global se necessario
        if (GerenciadorPontuacaoGlobal.Instancia != null)
        {
            GerenciadorPontuacaoGlobal.Instancia.IniciarNovaSessao();
        }

        // Carrega a cena do Menu Inicial
        if (!string.IsNullOrEmpty(nomeCenaMenu))
        {
            SceneManager.LoadScene(nomeCenaMenu);
        }
        else
        {
            Debug.LogWarning("O nome da cena do Menu Inicial nao foi configurado no Inspector!");
        }
    }
}