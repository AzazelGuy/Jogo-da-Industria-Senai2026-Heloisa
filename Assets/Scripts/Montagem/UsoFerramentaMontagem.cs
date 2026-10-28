using UnityEngine;

[RequireComponent(typeof(Collider))]
public class UsoFerramentaMontagem: MonoBehaviour
{
    [Header("Configuração da Ação")]
    [Tooltip("ID da ação exigida pelo StepChecker (ex.: 'parafusar', 'pasta_termica').")]
    [SerializeField] private string acaoID;

    [Tooltip("Nome exato da ferramenta na Hotbar necessária para esta ação (ex.: 'Chave de Fenda').")]
    [SerializeField] private string ferramentaNecessaria;

    [Header("Feedback")]
    [SerializeField] private GameObject resultadoVisual;

    private void OnMouseDown()
{
    // Trava se o jogo estiver pausado
    if (PauseMenuActions.instance != null && PauseMenuActions.instance.Paused) return;
    if (StepChecker.Instance == null || UiController.Instance == null) return;

    // 1. Verifica se a ação está liberada pela sequência do StepChecker
    if (!StepChecker.Instance.AcaoDisponivel(acaoID))
    {
        Debug.Log("[UsoFerramenta] Ação não disponível no momento (fora da etapa).");
        return;
    }

    // 2. Verifica se o jogador está com a ferramenta certa selecionada na Hotbar
    string ferramentaAtual = UiController.Instance.FerramentaSelecionadaAtual;
    if (ferramentaAtual != ferramentaNecessaria)
    {
        StepChecker.Instance.NotificarBloqueio($"Você precisa selecionar '{ferramentaNecessaria}' na Hotbar para fazer isso.");
        return;
    }

    // 3. Executa a ação com sucesso!
    if (StepChecker.Instance.RegistrarAcao(acaoID))
    {
        if (resultadoVisual != null) resultadoVisual.SetActive(true);
        Debug.Log($"[UsoFerramenta] Ação {acaoID} executada com sucesso usando {ferramentaAtual}!");

        // Opcional: destruir este script ou desativar o collider se for ação única
        enabled = false;
    }
}
}