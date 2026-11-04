using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CanvasGroup))]
public class HoverFade : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private float alphaVisivel = 1f;
    [SerializeField] private float alphaEscondido = 0.05f; // 0 = invisível total
    [SerializeField] private float velocidade = 10f;       // 0 = troca instantânea

    private CanvasGroup grupo;
    private float alvo;

    private void Awake()
    {
        grupo = GetComponent<CanvasGroup>();
        alvo = alphaEscondido;
        grupo.alpha = alphaEscondido;
    }

    private void Update()
    {
        if (velocidade <= 0f)
            grupo.alpha = alvo;
        else
            grupo.alpha = Mathf.MoveTowards(grupo.alpha, alvo, velocidade * Time.unscaledDeltaTime);
    }

    public void OnPointerEnter(PointerEventData eventData) => alvo = alphaVisivel;
    public void OnPointerExit(PointerEventData eventData) => alvo = alphaEscondido;
}