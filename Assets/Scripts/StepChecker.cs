using System;
using UnityEngine;

public enum StepPhase //Define as diferentes fases da montagem dos computadores
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

    public StepPhase CurrentStep = StepPhase.Nada;
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
    public event Action MudancadePasso;

    public event Action TudoFeito;
    public StepPhase GetCurrStep => CurrentStep;
}
