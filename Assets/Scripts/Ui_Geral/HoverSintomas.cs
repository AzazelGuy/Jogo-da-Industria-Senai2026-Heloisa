/*
 * ======================================================================================
 * SCRIPT: HoverSintomas.cs
 * PROJETO: Hyper Bytes
 * ======================================================================================
 */

using UnityEngine;
using UnityEngine.EventSystems;

public class HoverSintomas : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private CliqueComputador cliqueComputador;
    private RectTransform rectTransform;

    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        cliqueComputador = FindObjectOfType<CliqueComputador>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (cliqueComputador != null && rectTransform != null && gameObject.activeSelf)
        {
            StopAllCoroutines();
            StartCoroutine(cliqueComputador.DeslizarPainel(rectTransform, rectTransform.anchoredPosition.x, cliqueComputador.posXVisivelSintomas));
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (cliqueComputador != null && rectTransform != null && gameObject.activeSelf)
        {
            StopAllCoroutines();
            StartCoroutine(cliqueComputador.DeslizarPainel(rectTransform, rectTransform.anchoredPosition.x, cliqueComputador.posXParcialSintomas));
        }
    }
}