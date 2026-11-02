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

    [Header("--- MONITORAMENTO DE VITÓRIA FINAL ---")]
    [Tooltip("Arraste aqui o objeto da fala de vitória da fase final do Betinho.")]
    public GameObject painelFalaVitoriaFinal;

    [Header("--- BOTÃO DE RETORNAR AO MENU ---")]
    [Tooltip("Arraste aqui o componente Button da UI que leva de volta ao Menu Inicial.")]
    public Button botaoMenuInicial;

    private bool painelFoiAtivado = false;

    private void Start()
    {
        // Garante que o botão começa desativado ao iniciar a fase
        if (botaoMenuInicial != null)
        {
            botaoMenuInicial.gameObject.SetActive(false);

            // Limpa listeners antigos para segurança e vincula a ação de retorno
            botaoMenuInicial.onClick.RemoveAllListeners();
            botaoMenuInicial.onClick.AddListener(RetornarAoMenu);
        }
    }

    private void Update()
    {
        // Monitora quando a fala de vitória final aparece na tela
        if (!painelFoiAtivado && painelFalaVitoriaFinal != null && painelFalaVitoriaFinal.activeSelf)
        {
            painelFoiAtivado = true;

            // Ativa o botão para voltar ao Menu Inicial
            if (botaoMenuInicial != null)
            {
                botaoMenuInicial.gameObject.SetActive(true);
            }
        }
    }

    // Método chamado ao clicar no botão
    public void RetornarAoMenu()
    {
        // Reinicia a sessão de pontuação global se necessário
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
            Debug.LogWarning("O nome da cena do Menu Inicial não foi configurado no Inspector!");
        }
    }
}