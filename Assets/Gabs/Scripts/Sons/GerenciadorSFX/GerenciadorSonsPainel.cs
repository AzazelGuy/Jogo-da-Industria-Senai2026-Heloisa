/*
 * ======================================================================================
 * SCRIPT: GerenciadorSonsPainel.cs
 * PROJETO: Hyper Bytes
 * ======================================================================================
 */

using UnityEngine;

public class GerenciadorSonsPainel : MonoBehaviour
{
    [Header("--- PAINEIS DE UI A MONITORAR ---")]
    [Tooltip("Arraste aqui o GameObject do Painel de Alerta de Erro.")]
    public GameObject painelErro;

    [Tooltip("Arraste aqui o GameObject do Painel de Alerta de Acerto.")]
    public GameObject painelAcerto;

    private bool erroFoiAtivadoAnteriormente = false;
    private bool acertoFoiAtivadoAnteriormente = false;

    private void Update()
    {
        // 1. Monitora o Painel de Erro
        if (painelErro != null)
        {
            // Se o painel acabou de ser ativado e antes estava desativado
            if (painelErro.activeSelf && !erroFoiAtivadoAnteriormente)
            {
                erroFoiAtivadoAnteriormente = true;
                TocarSomDeErro();
            }
            // Se o painel foi fechado/desativado, reseta o gatilho para poder tocar novamente no proximo erro
            else if (!painelErro.activeSelf)
            {
                erroFoiAtivadoAnteriormente = false;
            }
        }

        // 2. Monitora o Painel de Acerto
        if (painelAcerto != null)
        {
            // Se o painel acabou de ser ativado e antes estava desativado
            if (painelAcerto.activeSelf && !acertoFoiAtivadoAnteriormente)
            {
                acertoFoiAtivadoAnteriormente = true;
                TocarSomDeAcerto();
            }
            // Se o painel foi fechado/desativado, reseta o gatilho
            else if (!painelAcerto.activeSelf)
            {
                acertoFoiAtivadoAnteriormente = false;
            }
        }
    }

    private void TocarSomDeErro()
    {
        // Tenta tocar atraves do GerenciadorSfx global se ele existir
        if (GerenciadorSfx.Instancia != null)
        {
            GerenciadorSfx.Instancia.TocarSomErro();
        }
        else
        {
            Debug.LogWarning("GerenciadorSfx nao encontrado na cena para tocar o som de erro!");
        }
    }

    private void TocarSomDeAcerto()
    {
        // Tenta tocar atraves do GerenciadorSfx global se ele existir
        if (GerenciadorSfx.Instancia != null)
        {
            GerenciadorSfx.Instancia.TocarSomAcerto();
        }
        else
        {
            Debug.LogWarning("GerenciadorSfx nao encontrado na cena para tocar o som de acerto!");
        }
    }
}