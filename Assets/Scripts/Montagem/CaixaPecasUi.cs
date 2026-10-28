using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controla a UI da Caixa de Peças e Ferramentas.
/// Gerencia abertura/fechamento com fade (CanvasGroup), o spawn das peças na mesa puxando direto
/// do SOPieceData, o LIMITE de peças que podem estar na mesa e o ato de GUARDAR peças de volta na caixa.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class CaixaPecasUi : MonoBehaviour
{
    #region Singleton

    public static CaixaPecasUi Instance { get; private set; }

    #endregion

    #region Tipos Auxiliares

    /// <summary>
    /// Limite específico de uma peça (ex.: Memória RAM = 2). Peças fora da lista usam o limite padrão.
    /// </summary>
    [Serializable]
    public class LimitePeca
    {
        public SOPieceData peca;
        [Min(1)] public int maximo = 1;
    }

    #endregion

    #region Campos Serializados

    [Header("Configurações de UI")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float fadeDuration = 0.3f;

    [Header("Spawn na Mesa")]
    [Tooltip("Ponto de referência na mesa onde a peça deve aparecer ao ser selecionada.")]
    [SerializeField] private Transform pontoSpawnMesa;

    [Header("Limite de Peças")]
    [Tooltip("Quantas cópias de cada peça podem existir ao mesmo tempo (na mesa ou já encaixadas).")]
    [SerializeField, Min(1)] private int limitePadraoPorPeca = 1;

    [Tooltip("Exceções ao limite padrão (ex.: Memória RAM = 2).")]
    [SerializeField] private List<LimitePeca> limitesEspecificos = new List<LimitePeca>();

    [Tooltip("Limite de peças no total, somando todos os tipos. 0 = sem limite total.")]
    [SerializeField, Min(0)] private int limiteTotalNaMesa = 0;

    #endregion

    #region Estado Interno

    private Coroutine fadeRoutine;
    private bool isOpen = false;

    // Peças que a caixa já spawnou (e ainda existem), agrupadas pelo SOPieceData de origem
    private readonly Dictionary<SOPieceData, List<GameObject>> pecasSpawnadas = new Dictionary<SOPieceData, List<GameObject>>();

    #endregion

    #region Propriedades Públicas

    /// <summary>
    /// Caixa realmente aberta. Se o painel for desativado por fora (SetActive(false) de outro script),
    /// isso vira false sozinho, em vez de ficar "aberta" para sempre e travar os cliques do mundo.
    /// </summary>
    public bool IsOpen => isOpen && canvasGroup != null && canvasGroup.gameObject.activeInHierarchy;

    #endregion

    #region Ciclo de Vida (Unity)

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[CaixaPecasUI] Já existe uma instância ativa. Removendo o componente duplicado.", this);
            Destroy(this);
            return;
        }

        Instance = this;

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        // Começa fechado e invisível
        FecharCaixaInstantaneo();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnDisable()
    {
        // Corrotinas morrem quando o objeto é desativado; volta ao estado fechado para não ficar inconsistente
        fadeRoutine = null;
        FecharCaixaInstantaneo();
    }

    #endregion

    #region Abrir e Fechar (com Fade)

    /// <summary>
    /// Alterna entre abrir e fechar a caixa de peças.
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
        if (IsOpen) return;

        if (!isActiveAndEnabled)
        {
            Debug.LogWarning("[CaixaPecasUI] O objeto da CaixaPecasUi está desativado, então não dá para abrir. " +
                             "Deixe este script num objeto sempre ativo (ex.: o Canvas) e arraste o CanvasGroup do painel no campo.", this);
            return;
        }

        isOpen = true;

        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeRoutine(1f, true));
    }

    public void FecharCaixa()
    {
        if (!isOpen) return;

        if (!isActiveAndEnabled)
        {
            FecharCaixaInstantaneo();
            return;
        }

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

        // Se o CanvasGroup estiver no MESMO objeto deste script, nunca desativamos o objeto:
        // senão a própria caixa se desliga e não consegue mais abrir (nem rodar corrotinas).
        bool podeDesativarObjeto = canvasGroup.gameObject != gameObject;

        if (ativarNoInicio && podeDesativarObjeto)
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
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            if (podeDesativarObjeto) canvasGroup.gameObject.SetActive(false);
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
    /// Respeita o limite de peças, puxa o prefab do ScriptableObject, spawna na mesa e fecha o menu.
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

        // Limite de peças: avisa o jogador e mantém a caixa aberta
        if (!PodeSpawnar(infoPeca, out string motivo))
        {
            Debug.Log($"[CaixaPecasUI] Spawn bloqueado: {motivo}");
            if (StepChecker.Instance != null) StepChecker.Instance.NotificarBloqueio(motivo);
            return;
        }

        Vector3 posicaoSpawn = pontoSpawnMesa != null ? pontoSpawnMesa.position : Vector3.zero;
        Quaternion rotacaoSpawn = pontoSpawnMesa != null ? pontoSpawnMesa.rotation : Quaternion.identity;

        // Instancia a peça na mesa
        GameObject novaPeca = Instantiate(prefabPeca, posicaoSpawn, rotacaoSpawn);
        RegistrarSpawn(infoPeca, novaPeca);

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

    #region Limite de Peças

    /// <summary>
    /// Diz se ainda dá para spawnar mais uma cópia desta peça. Em caso negativo, 'motivo' explica o porquê.
    /// </summary>
    public bool PodeSpawnar(SOPieceData peca, out string motivo)
    {
        motivo = null;
        if (peca == null) return false;

        if (limiteTotalNaMesa > 0 && ContarTotal() >= limiteTotalNaMesa)
        {
            motivo = $"Limite de {limiteTotalNaMesa} peças na mesa atingido. Guarde alguma (botão direito) para pegar outra.";
            return false;
        }

        int limite = ObterLimite(peca);
        if (ContarDaPeca(peca) >= limite)
        {
            motivo = $"Você já pegou o máximo de \"{peca.Nome}\" ({limite}). Guarde uma (botão direito) para pegar outra.";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Quantas cópias desta peça ainda podem ser spawnadas. Útil para mostrar "x2" no ícone da UI.
    /// </summary>
    public int QuantidadeRestante(SOPieceData peca)
    {
        if (peca == null) return 0;
        return Mathf.Max(0, ObterLimite(peca) - ContarDaPeca(peca));
    }

    private int ObterLimite(SOPieceData peca)
    {
        foreach (LimitePeca limite in limitesEspecificos)
        {
            if (limite != null && limite.peca == peca) return Mathf.Max(1, limite.maximo);
        }

        return limitePadraoPorPeca;
    }

    private int ContarDaPeca(SOPieceData peca)
    {
        if (!pecasSpawnadas.TryGetValue(peca, out List<GameObject> lista)) return 0;

        lista.RemoveAll(p => p == null); // limpa peças que foram destruídas por outros caminhos
        return lista.Count;
    }

    private int ContarTotal()
    {
        int total = 0;
        foreach (KeyValuePair<SOPieceData, List<GameObject>> par in pecasSpawnadas)
        {
            par.Value.RemoveAll(p => p == null);
            total += par.Value.Count;
        }

        return total;
    }

    private void RegistrarSpawn(SOPieceData peca, GameObject instancia)
    {
        if (!pecasSpawnadas.TryGetValue(peca, out List<GameObject> lista))
        {
            lista = new List<GameObject>();
            pecasSpawnadas[peca] = lista;
        }

        lista.Add(instancia);
    }

    private SOPieceData EncontrarOrigem(GameObject instancia)
    {
        foreach (KeyValuePair<SOPieceData, List<GameObject>> par in pecasSpawnadas)
        {
            if (par.Value.Contains(instancia)) return par.Key;
        }

        return null;
    }

    #endregion

    #region Guardar Peças

    /// <summary>
    /// Guarda a peça de volta na caixa: destrói a instância e libera uma vaga no limite.
    /// Só funciona para peças que vieram da caixa e que NÃO estão encaixadas.
    /// </summary>
    public bool GuardarPeca(ComponenteBaseMontagem peca)
    {
        if (peca == null || peca.IsPlaced) return false;

        SOPieceData origem = EncontrarOrigem(peca.gameObject);
        if (origem == null)
        {
            Debug.LogWarning($"[CaixaPecasUI] '{peca.name}' não foi spawnada pela caixa, então não pode ser guardada.");
            return false;
        }

        // Limpa o que a peça deixou espalhado e avisa o SelectionManager ANTES de destruir
        peca.PrepararParaGuardar();
        if (SelectionManager.Instance != null) SelectionManager.Instance.Esquecer(peca);

        pecasSpawnadas[origem].Remove(peca.gameObject);
        Destroy(peca.gameObject);

        Debug.Log($"[CaixaPecasUI] Peça '{origem.Nome}' guardada na caixa. Restantes para pegar: {QuantidadeRestante(origem)}");
        return true;
    }

    #endregion
}
