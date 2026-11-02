/*
 * ======================================================================================
 * SCRIPT: SlotPecaHUD.cs
 * PROJETO: Hyper Bytes
 * ======================================================================================
 */

using UnityEngine;

public class SlotPecaHUD : MonoBehaviour
{
    [Header("--- CONFIGURACAO DA PECA DO SLOT ---")]
    public string nomeDaPeca = "Peca";
    [Tooltip("Marque TRUE apenas se ESTE slot for a peca com defeito da fase atual.")]
    public bool ehAPecaComDefeito = false;

    [Header("--- REFERENCIA DO BOOT (UI) ---")]
    [Tooltip("Arraste o objeto 'Botao_Boot_Verde' da sua UI para ca.")]
    public GameObject botaoBootVerde;

    public void ClicarNoSlot()
    {
        Debug.Log(">>> Clicou no slot da peca: " + nomeDaPeca);

        // Se o Gerenciador nao existir, aborta para evitar erros
        if (GerenciadorBootEMascote.Instancia == null) return;

        // Validacao do Boot: Se o botao verde estiver ativo, o PC esta ligado
        if (botaoBootVerde != null && botaoBootVerde.activeSelf)
        {
            Debug.LogWarning(">>> ACAO BLOQUEADA: O computador esta LIGADO!");
            GerenciadorBootEMascote.Instancia.TentarInteragirComPeca();
            return;
        }

        // Se o PC estiver desligado, valida se e a peca com defeito ou nao
        if (ehAPecaComDefeito)
        {
            Debug.Log(">>> ACERTOU! Esta e a peca defeituosa.");
            GerenciadorBootEMascote.Instancia.VencerFase();
        }
        else
        {
            Debug.Log(">>> ERRO! Esta peca esta boa.");
            GerenciadorBootEMascote.Instancia.ValidarPecaRemovida(false);
        }
    }
}