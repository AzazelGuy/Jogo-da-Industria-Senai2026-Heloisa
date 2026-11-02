/*
 * ======================================================================================
 * SCRIPT: SlotPecaHUD.cs
 * PROJETO: Hyper Bytes
 * ======================================================================================
 */

using UnityEngine;

public class SlotPecaHUD : MonoBehaviour
{
    [Header("--- CONFIGURAÇÃO DA PEÇA DO SLOT ---")]
    public string nomeDaPeca = "Peça";
    [Tooltip("Marque TRUE apenas se ESTE slot for a peça com defeito da fase atual.")]
    public bool ehAPecaComDefeito = false;

    [Header("--- REFERÊNCIA DO BOOT (UI) ---")]
    [Tooltip("Arraste o objeto 'Botao_Boot_Verde' da sua UI para cá.")]
    public GameObject botaoBootVerde;

    public void ClicarNoSlot()
    {
        Debug.Log(">>> Clicou no slot da peça: " + nomeDaPeca);

        // Se o Gerenciador não existir, aborta para evitar erros
        if (GerenciadorBootEMascote.Instancia == null) return;

        // Validação do Boot: Se o botão verde estiver ativo, o PC está ligado
        if (botaoBootVerde != null && botaoBootVerde.activeSelf)
        {
            Debug.LogWarning(">>> AÇÃO BLOQUEADA: O computador está LIGADO!");
            GerenciadorBootEMascote.Instancia.TentarInteragirComPeca();
            return;
        }

        // Se o PC estiver desligado, valida se é a peça com defeito ou não
        if (ehAPecaComDefeito)
        {
            Debug.Log(">>> ACERTOU! Esta é a peça defeituosa.");
            GerenciadorBootEMascote.Instancia.VencerFase();
        }
        else
        {
            Debug.Log(">>> ERRO! Esta peça está boa.");
            GerenciadorBootEMascote.Instancia.ValidarPecaRemovida(false);
        }
    }
}