using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Versão mínima: o painel desliza para dentro da tela quando o mouse está por cima
/// e volta a se esconder (sobrando só uma abinha) quando o mouse sai.
/// A posição que o painel tem no editor é a "aberta".
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class PainelEtapasDeslizante : MonoBehaviour
{
    [Tooltip("Quanto do painel continua aparecendo quando está escondido.")]
    [SerializeField] private float larguraAba = 40f;
    [SerializeField] private bool noLadoDireito = false;
    [SerializeField] private float velocidade = 12f;
    [SerializeField] private bool logDeDiagnostico = true;

    private RectTransform rect;
    private Camera cameraDoCanvas;
    private Vector2 posicaoAberta;
    private bool aberto;

    private void Start()
    {
        rect = (RectTransform)transform;
        posicaoAberta = rect.anchoredPosition;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null) canvas = canvas.rootCanvas;
        cameraDoCanvas = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;

        if (logDeDiagnostico)
        {
            Debug.Log($"[Painel] Start: posAberta={posicaoAberta}, largura={rect.rect.width}, aba={larguraAba}, " +
                      $"canvas={(canvas != null ? canvas.renderMode.ToString() : "NENHUM")}", this);
        }
    }

    private void Update()
    {
        bool dentro = RectTransformUtility.RectangleContainsScreenPoint(rect, PosicaoDoMouse(), cameraDoCanvas);

        if (dentro != aberto && logDeDiagnostico)
        {
            Debug.Log(dentro ? "[Painel] Mouse ENTROU" : "[Painel] Mouse SAIU", this);
        }
        aberto = dentro;

        float deslocamento = Mathf.Max(0f, rect.rect.width - larguraAba);
        Vector2 posicaoFechada = posicaoAberta + new Vector2(noLadoDireito ? deslocamento : -deslocamento, 0f);
        Vector2 alvo = aberto ? posicaoAberta : posicaoFechada;

        float t = 1f - Mathf.Exp(-velocidade * Time.unscaledDeltaTime);
        rect.anchoredPosition = Vector2.Lerp(rect.anchoredPosition, alvo, t);
    }

    private static Vector2 PosicaoDoMouse()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null ? Mouse.current.position.ReadValue() : new Vector2(-1f, -1f);
#else
        return Input.mousePosition;
#endif
    }
}