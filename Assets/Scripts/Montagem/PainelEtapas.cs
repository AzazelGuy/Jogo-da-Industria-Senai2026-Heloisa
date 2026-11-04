using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Versão mínima: o painel desliza para dentro da tela quando o mouse está por cima
/// e volta a se esconder (sobrando só uma abinha) quando o mouse sai.
/// A posição que o painel tem no editor é a "aberta".
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class PainelEtapas : MonoBehaviour
{
    [Tooltip("Quanto do painel continua aparecendo quando está escondido.")]
    [SerializeField] private Sprite[] Etapas;

    [SerializeField] private Image img;
    private int indicea = 0;
    private void Start()
    {
        if (img == null)img = GetComponent<Image>();
        img.sprite = Etapas[indicea];
    }

    private void OnEnable()
    {
        StepChecker.OnEtapaMudou += updateImage;
    }

    private void OnDisable()
    {
        StepChecker.OnEtapaMudou -= updateImage;
    }
    private void Update()
    {
        
    }
    private void updateImage(int indice, EtapaMontagem etapa)
    {
        indicea++;
        img.sprite = Etapas[indicea];
    }
}