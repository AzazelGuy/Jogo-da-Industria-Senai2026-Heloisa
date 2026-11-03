using UnityEngine;

[RequireComponent(typeof(Collider))]
public class UsoFerramentaMontagem : MonoBehaviour
{
    [Header("Configuração da Ação")]
    [Tooltip("ID da ação exigida pelo StepChecker (ex.: 'parafusar', 'pasta_termica').")]
    [SerializeField] private string acaoID;
    [Tooltip("Nome exato da ferramenta na Hotbar necessária para esta ação (ex.: 'Chave de Fenda').")]
    [SerializeField] private string ferramentaNecessaria;

    [Header("Feedback")]
    [SerializeField] private GameObject resultadoVisual;

    /// <summary>Executa a ação depois que o SelectionManager confirma o clique nesta área.</summary>
    public void TryPerformAction()
    {
        if (MistakeEvent.IsFeedbackActive) return;
        if (PauseMenuActions.instance != null && PauseMenuActions.instance.Paused) return;

        UiController uiController = UiController.Instance;
        if (uiController == null)
        {
            Debug.LogError("[UsoFerramenta] UiController não está ativo; não é possível validar a ferramenta selecionada.", this);
            return;
        }

        StepChecker stepChecker = StepChecker.Instance;
        if (stepChecker != null && !stepChecker.AcaoDisponivel(acaoID))
        {
            stepChecker.NotificarBloqueio("Essa ação ainda não está disponível nesta etapa.");
            return;
        }

        string ferramentaAtual = uiController.FerramentaSelecionadaAtual;
        if (ferramentaAtual != ferramentaNecessaria)
        {
            if (stepChecker != null)
                stepChecker.NotificarBloqueio($"Você precisa selecionar '{ferramentaNecessaria}' na Hotbar para fazer isso.");
            else
                Debug.LogWarning($"[UsoFerramenta] Selecione '{ferramentaNecessaria}' na Hotbar para fazer isso.", this);
            return;
        }

        if (stepChecker != null && !stepChecker.RegistrarAcao(acaoID)) return;

        if (resultadoVisual != null) resultadoVisual.SetActive(true);
        Debug.Log($"[UsoFerramenta] Ação {acaoID} executada com sucesso usando {ferramentaAtual}!");
        GetComponent<Collider>().enabled = false;
        enabled = false;

    }
}