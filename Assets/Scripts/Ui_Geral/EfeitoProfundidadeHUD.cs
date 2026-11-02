/*
 * ======================================================================================
 * SCRIPT: EfeitoProfundidadeHUD.cs
 * PROJETO: Hyper Bytes
 * ======================================================================================
 */

using UnityEngine;

public class EfeitoProfundidadeHUD : MonoBehaviour
{
    [Header("Configuração de Flutuação (Hover)")]
    [Tooltip("Velocidade do movimento de flutuação.")]
    public float velocidadeFlutuacao = 2f;
    [Tooltip("Altura máxima que o painel sobe e desce.")]
    public float amplitudeFlutuacao = 10f;

    private RectTransform rectTransform;
    private Vector2 posicaoBase;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        // Salva a posição inicial em que o painel apareceu na tela
        if (rectTransform != null)
        {
            posicaoBase = rectTransform.anchoredPosition;
        }
    }

    private void Update()
    {
        if (rectTransform == null) return;

        // Calcula o novo deslocamento vertical usando seno
        float novoY = posicaoBase.y + (Mathf.Sin(Time.unscaledTime * velocidadeFlutuacao) * amplitudeFlutuacao);

        // Aplica apenas na altura (Y), mantendo o X fixo onde o deslize o colocou
        Vector2 posAtual = rectTransform.anchoredPosition;
        posAtual.y = novoY;
        rectTransform.anchoredPosition = posAtual;
    }
}