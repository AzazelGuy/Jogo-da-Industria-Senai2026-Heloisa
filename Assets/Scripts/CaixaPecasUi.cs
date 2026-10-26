using System.Collections;
using UnityEngine;

/// <summary>
/// Controla a UI da Caixa de Peças e Ferramentas. 
/// Gerencia abertura/fechamento com fade (CanvasGroup) e o spawn das peças na mesa puxando direto do SOPieceData.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class CaixaPecasUi : MonoBehaviour
{
    #region Campos Serializados

    [Header("Configurações de UI")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float fadeDuration = 0.3f;

    [Header("Spawn na Mesa")]
    [Tooltip("Ponto de referência na mesa onde a peça deve aparecer ao ser selecionada.")]
    [SerializeField] private Transform pontoSpawnMesa;

    #endregion

    #region Estado Interno

    private Coroutine fadeRoutine;
    private bool isOpen = false;

    #endregion

    #region Ciclo de Vida (Unity)

    private void Awake()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        // Começa fechado e invisível
        FecharCaixaInstantaneo();
    }

    #endregion

    #region Abrir e Fechar (com Fade)

    /// <summary>
    /// Alterna entre abrir e fechar a caixa de peças.
    /// Pode ser ligado a um clique no GameObject da caixa no cenário.
    /// </summary>
    public void ToggleCaixa()
    {
        if (isOpen)
        {
            FecharCaixa();
        }
        else
        {
            AbrirCaixa();
        }
    }

    public void AbrirCaixa()
    {
        if (isOpen) return;
        isOpen = true;

        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeRoutine(1f, true));
    }

    public void FecharCaixa()
    {
        if (!isOpen) return;
        isOpen = false;

        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeRoutine(0f, false));
    }

    private void FecharCaixaInstantaneo()
    {
        isOpen = false;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    private IEnumerator FadeRoutine(float targetAlpha, bool ativarNoInicio)
    {
        if (canvasGroup == null) yield break;

        if (ativarNoInicio)
        {
            canvasGroup.gameObject.SetActive(true);
        }

        float startAlpha = canvasGroup.alpha;
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;

        if (targetAlpha <= 0f)
        {
            canvasGroup.gameObject.SetActive(false);
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        else
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        fadeRoutine = null;
    }

    #endregion

    #region Spawn de Peças (Integrado com SOPieceData)

    /// <summary>
    /// Chamado pelos botões dos ícones das peças na UI.
    /// Puxa o prefab direto do ScriptableObject, spawna na mesa e fecha o menu da caixa.
    /// </summary>
    public void SpawnarPecaPeloSO(SOPieceData infoPeca)
    {
        if (infoPeca == null || infoPeca.prefab == null || infoPeca.prefab.Count == 0)
        {
            Debug.LogWarning("[CaixaPecasUI] SOPieceData inválido ou sem prefabs cadastrados na lista!");
            return;
        }

        // Pega o primeiro prefab da lista do ScriptableObject
        GameObject prefabPeca = infoPeca.prefab[0];
        if (prefabPeca == null)
        {
            Debug.LogWarning("[CaixaPecasUI] O prefab na posição 0 da lista está nulo!");
            return;
        }

        Vector3 posicaoSpawn = pontoSpawnMesa != null ? pontoSpawnMesa.position : Vector3.zero;
        Quaternion rotacaoSpawn = pontoSpawnMesa != null ? pontoSpawnMesa.rotation : Quaternion.identity;

        // Instancia a peça na mesa
        GameObject novaPeca = Instantiate(prefabPeca, posicaoSpawn, rotacaoSpawn);

        // Garante que a física e gravidade estão ativas ao cair na mesa
        Rigidbody rb = novaPeca.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        Debug.Log($"[CaixaPecasUI] Peça '{infoPeca.Nome}' spawnada com sucesso na mesa!");

        // Fecha a UI da caixa após selecionar a peça
        FecharCaixa();
    }

    #endregion
}