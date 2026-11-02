/*
 * ======================================================================================
 * SCRIPT: TooltipSlotHUD.cs
 * PROJETO: Hyper Bytes
 * ======================================================================================
 */

using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class TooltipSlotHUD : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("--- CONFIGURAÇÃO DO TOOLTIP ESPECÍFICO ---")]
    [Tooltip("Arraste o CanvasGroup da imagem de especificação correspondente a esta peça.")]
    public CanvasGroup canvasGroupTooltip;

    [Tooltip("Velocidade do Fade In / Fade Out.")]
    public float velocidadeFade = 10f;

    [Header("--- REFERÊNCIA DA HUD GLOBAL (SLOTS) ---")]
    [Tooltip("Arraste o CanvasGroup principal da sua HUD de slots.")]
    public CanvasGroup canvasGroupPainelSlots;

    private Coroutine corrotinaFade;

    private void Start()
    {
        ForçarFechamentoImediato();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Só ativa se a HUD de slots estiver totalmente visível (alpha > 0.5)
        if (canvasGroupTooltip != null && canvasGroupPainelSlots != null && canvasGroupPainelSlots.alpha > 0.5f)
        {
            if (corrotinaFade != null) StopCoroutine(corrotinaFade);
            corrotinaFade = StartCoroutine(FazerFade(1f, ativarNoInicio: true, desativarNoFim: false));
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ForçarFechamento();
    }

    // Método público para fechar suavemente
    public void ForçarFechamento()
    {
        if (canvasGroupTooltip != null && canvasGroupTooltip.gameObject.activeSelf)
        {
            if (corrotinaFade != null) StopCoroutine(corrotinaFade);
            corrotinaFade = StartCoroutine(FazerFade(0f, ativarNoInicio: false, desativarNoFim: true));
        }
    }

    // Método público para fechar instantaneamente (usado ao dar zoom ou sair)
    public void ForçarFechamentoImediato()
    {
        if (canvasGroupTooltip != null)
        {
            if (corrotinaFade != null) StopCoroutine(corrotinaFade);
            canvasGroupTooltip.alpha = 0f;
            canvasGroupTooltip.gameObject.SetActive(false);
        }
    }

    private IEnumerator FazerFade(float alphaAlvo, bool ativarNoInicio, bool desativarNoFim)
    {
        if (ativarNoInicio)
        {
            canvasGroupTooltip.gameObject.SetActive(true);
        }

        while (Mathf.Abs(canvasGroupTooltip.alpha - alphaAlvo) > 0.01f)
        {
            canvasGroupTooltip.alpha = Mathf.MoveTowards(canvasGroupTooltip.alpha, alphaAlvo, Time.unscaledDeltaTime * velocidadeFade);
            yield return null;
        }

        canvasGroupTooltip.alpha = alphaAlvo;

        if (desativarNoFim)
        {
            canvasGroupTooltip.gameObject.SetActive(false);
        }
    }
}