using UnityEngine;
using UnityEngine.UI;

public class ToggleDislexia : MonoBehaviour
{
    [SerializeField] private Toggle toggle;

    void Start()
    {
        // sincroniza o visual com o estado salvo, sem disparar o evento
        toggle.SetIsOnWithoutNotify(AcessibilidadeFonte.Instance.Ativa);
        toggle.onValueChanged.AddListener(AcessibilidadeFonte.Instance.Definir);
    }
}