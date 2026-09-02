using System;
using UnityEngine;

public enum StepPhase
{
    Nada,
    Gabinete,
    PlacaMae,
    Processador,
    Memoria,
    extras,
    Fonte,
    finalizado
}

public class StepChecker : MonoBehaviour
{
    public static StepChecker Instance { get; private set; }

    [Header("Progresso Atual")]
    public StepPhase CurrentStep = StepPhase.Nada;

    [Header("Configuração de Avanço")]
    [Tooltip("Define se a etapa atual deve avançar automaticamente ao concluir os conectores.")]
    [SerializeField] private bool autoAdvanceStep = true;

    [Tooltip("Define qual será a próxima etapa caso o avanço automático esteja ativo.")]
    [SerializeField] private StepPhase nextStep = StepPhase.Gabinete;

    public event Action MudancadePasso;
    public event Action TudoFeito;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Verifica o progresso dos conectores de uma base e avança de etapa se tudo estiver concluído.
    /// </summary>
    public void CheckConnectorProgress(IdentifiyerEncaixe encaixe)
    {
        if (encaixe == null) return;

        int total = encaixe.CompletedScrews.Count;
        int concluidos = encaixe.GetCompletedCount();

        Debug.Log($"[StepChecker] Progresso de {encaixe.getID}: {concluidos}/{total} conectores colocados.");

        if (encaixe.IsFullyAssembled())
        {
            Debug.Log($"[StepChecker] Todos os conectores da peça '{encaixe.getID}' foram instalados!");
            
            if (autoAdvanceStep)
            {
                AdvanceToNextStep();
            }
        }
    }

    /// <summary>
    /// Avança o estado para a próxima etapa configurada no Inspector.
    /// </summary>
    public void AdvanceToNextStep()
    {
        RegisterStepCompletion(nextStep);
    }

    public void RegisterStepCompletion(StepPhase step)
    {
        CurrentStep = step;
        MudancadePasso?.Invoke();
        ValidateBoothConditions();
    }

    private void ValidateBoothConditions()
    {
        if (CurrentStep == StepPhase.finalizado)
        {
            TudoFeito?.Invoke();
        }
    }

    public StepPhase GetCurrStep => CurrentStep;
}