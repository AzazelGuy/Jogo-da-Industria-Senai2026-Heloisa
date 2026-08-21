using UnityEngine;

//Script responsavel por conferir as etapas das peças encaixadas
public enum StatesOfCompletion //Estados de montagem
{
    Empty,
    Gabinete,
    PlacaMaeEFonte,
    PecasBasicas,
    PecasFinais,
    Finalizado
}
public class StepChecker : MonoBehaviour
{
    public static StepChecker Instance { get; private set; }

    private StatesOfCompletion currState = StatesOfCompletion.Empty;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AtualizarEstado(StatesOfCompletion newState)
    {
        if (!currState.Equals(newState))
        {
            Debug.Log($"<color=green>[StepChecker]</color> Estado atalizado de {currState.ToString()} para {newState.ToString()}.");
            currState = newState;
            return;
        }else
        {
            Debug.LogWarning($"<color=green>[StepChecker]</color> Não é possivel mudar o estado para o mesmo estado atual!");
            return;
        }
    }
}
