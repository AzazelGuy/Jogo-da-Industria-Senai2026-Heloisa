/*
 * ======================================================================================
 * SCRIPT: InspecaoEImaPC.cs
 * PROJETO: Hyper Bytes
 * ======================================================================================
 */

using System.Collections;
using UnityEngine;

public class InspecaoEImaPC : MonoBehaviour
{
    [Header("Sensibilidade e Suavizacao")]
    public float sensibilidadeMouse = 5f;
    public float velocidadeIma = 10f;

    [Header("Configuracoes de Zoom")]
    public float velocidadeZoom = 5f;
    public float fovMinimo = 25f;
    public float fovMaximo = 60f;
    public float velocidadeSuavizacaoZoom = 8f;

    [Header("--- CONTROLE DE HUD POR ZOOM (INVERTIDO) ---")]
    public float fovLimiteAbertura = 45f;
    public float tempoFade = 0.3f;

    [Header("--- INTERFACES DA HUD (SLOTS & SINTOMAS) ---")]
    public CanvasGroup canvasGroupSlots;
    public CanvasGroup canvasGroupSintomas;

    [Header("--- TOOLTIPS (SLOTS DE PECAS) ---")]
    [Tooltip("Arraste aqui todos os scripts TooltipSlotHUD presentes nos botoes.")]
    public TooltipSlotHUD[] arrayTooltips;

    [Header("Camadas de Interacao")]
    public LayerMask camadaPecas = ~0;

    private bool arrastando = false;
    private bool jaInteragiu = false;
    private bool hudVisivel = true;
    private Coroutine rotinaFadeAtual;

    private Camera camPrincipal;
    private float fovAlvo;

    private readonly Quaternion[] rotacoesDasFaces = new Quaternion[]
    {
        Quaternion.Euler(0, 0, 0),
        Quaternion.Euler(0, 180, 0),
        Quaternion.Euler(0, 90, 0),
        Quaternion.Euler(0, -90, 0),
        Quaternion.Euler(90, 0, 0),
        Quaternion.Euler(-90, 0, 0)
    };

    void Awake()
    {
        camPrincipal = Camera.main;
        if (camPrincipal != null)
        {
            fovAlvo = camPrincipal.fieldOfView;
        }
    }

    void OnEnable()
    {
        arrastando = false;
        jaInteragiu = false;
        hudVisivel = true;

        if (camPrincipal == null) camPrincipal = Camera.main;
        if (camPrincipal != null)
        {
            fovAlvo = camPrincipal.fieldOfView;
        }

        MostrarHUDImediatamente();
    }

    void OnDisable()
    {
        arrastando = false;
        jaInteragiu = false;
        hudVisivel = true;

        if (camPrincipal != null)
        {
            camPrincipal.fieldOfView = fovMaximo;
        }

        EsconderTodasTooltipsImediatamente();
        MostrarHUDImediatamente();
    }

    void Update()
    {
        if (camPrincipal == null) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ResetarZoom();
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            fovAlvo -= scroll * velocidadeZoom * 10f;
            fovAlvo = Mathf.Clamp(fovAlvo, fovMinimo, fovMaximo);
        }

        camPrincipal.fieldOfView = Mathf.Lerp(camPrincipal.fieldOfView, fovAlvo, Time.unscaledDeltaTime * velocidadeSuavizacaoZoom);

        if (camPrincipal.fieldOfView <= fovLimiteAbertura && hudVisivel)
        {
            hudVisivel = false;
            EsconderTodasTooltipsImediatamente(); // Forca o fechamento imediato ao dar zoom
            FazerFadeHUD(false);
        }
        else if (camPrincipal.fieldOfView > fovLimiteAbertura + 2f && !hudVisivel)
        {
            hudVisivel = true;
            FazerFadeHUD(true);
        }

        if (Input.GetMouseButtonDown(0))
        {
            arrastando = true;
            jaInteragiu = true;
        }

        if (Input.GetMouseButtonUp(0))
        {
            arrastando = false;
        }

        if (arrastando)
        {
            float mouseX = -Input.GetAxis("Mouse X") * sensibilidadeMouse;
            float mouseY = Input.GetAxis("Mouse Y") * sensibilidadeMouse;

            Transform camTransform = camPrincipal.transform;
            transform.Rotate(camTransform.up, mouseX, Space.World);
            transform.Rotate(camTransform.right, mouseY, Space.World);
        }
        else if (jaInteragiu)
        {
            AplicarImaGlobal();
        }

        if (Input.GetMouseButtonDown(1))
        {
            TentarRemoverPeca();
        }
    }

    public void ResetarZoom()
    {
        fovAlvo = fovMaximo;
    }

    private void FazerFadeHUD(bool mostrar)
    {
        if (rotinaFadeAtual != null)
        {
            StopCoroutine(rotinaFadeAtual);
        }
        rotinaFadeAtual = StartCoroutine(ExecutarFadeGlobal(mostrar));
    }

    private IEnumerator ExecutarFadeGlobal(bool mostrar)
    {
        float tempo = 0f;
        float alphaInicialSlots = canvasGroupSlots != null ? canvasGroupSlots.alpha : (mostrar ? 0f : 1f);
        float alphaInicialSintomas = canvasGroupSintomas != null ? canvasGroupSintomas.alpha : (mostrar ? 0f : 1f);
        float alphaAlvo = mostrar ? 1f : 0f;

        if (mostrar)
        {
            if (canvasGroupSlots != null) canvasGroupSlots.gameObject.SetActive(true);
            if (canvasGroupSintomas != null) canvasGroupSintomas.gameObject.SetActive(true);
        }

        while (tempo < tempoFade)
        {
            tempo += Time.unscaledDeltaTime;
            float t = tempo / tempoFade;

            if (canvasGroupSlots != null)
                canvasGroupSlots.alpha = Mathf.Lerp(alphaInicialSlots, alphaAlvo, t);

            if (canvasGroupSintomas != null)
                canvasGroupSintomas.alpha = Mathf.Lerp(alphaInicialSintomas, alphaAlvo, t);

            yield return null;
        }

        if (canvasGroupSlots != null) canvasGroupSlots.alpha = alphaAlvo;
        if (canvasGroupSintomas != null) canvasGroupSintomas.alpha = alphaAlvo;

        if (!mostrar)
        {
            if (canvasGroupSlots != null) canvasGroupSlots.gameObject.SetActive(false);
            if (canvasGroupSintomas != null) canvasGroupSintomas.gameObject.SetActive(false);
        }
    }

    private void MostrarHUDImediatamente()
    {
        if (canvasGroupSlots != null)
        {
            canvasGroupSlots.alpha = 1f;
            canvasGroupSlots.gameObject.SetActive(true);
        }

        if (canvasGroupSintomas != null)
        {
            canvasGroupSintomas.alpha = 1f;
            canvasGroupSintomas.gameObject.SetActive(true);
        }
    }

    private void EsconderTodasTooltipsImediatamente()
    {
        if (arrayTooltips != null)
        {
            foreach (var tooltipScript in arrayTooltips)
            {
                if (tooltipScript != null)
                {
                    tooltipScript.ForcarFechamentoImediato();
                }
            }
        }
    }

    void AplicarImaGlobal()
    {
        Quaternion rotAtual = transform.localRotation;
        Quaternion faceMaisProxima = Quaternion.identity;
        float menorAngulo = float.MaxValue;

        foreach (Quaternion rotFace in rotacoesDasFaces)
        {
            float angulo = Quaternion.Angle(rotAtual, rotFace);

            if (angulo < menorAngulo)
            {
                menorAngulo = angulo;
                faceMaisProxima = rotFace;
            }
        }

        transform.localRotation = Quaternion.Slerp(transform.localRotation, faceMaisProxima, Time.unscaledDeltaTime * velocidadeIma);
    }

    private void TentarRemoverPeca()
    {
        Ray ray = camPrincipal.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 100f, camadaPecas);

        foreach (RaycastHit hit in hits)
        {
            PecaInterativa peca = hit.collider.GetComponentInParent<PecaInterativa>();
            if (peca != null)
            {
                peca.DesmontarPeca();
                break;
            }
        }
    }
}