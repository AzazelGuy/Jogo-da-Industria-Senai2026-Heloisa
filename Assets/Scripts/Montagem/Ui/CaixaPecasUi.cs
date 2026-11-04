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
    [Tooltip("Ponto de onde as peças começam o voo até a mesa.")]
    [SerializeField] private Transform pontoPartidaVoo;
    [SerializeField, Min(0.1f)] private float duracaoVoo = 1f;
    [SerializeField, Min(0f)] private float alturaCurvaVoo = 0.75f;

    [Header("Limite de Peças")]
    [Tooltip("Quantas cópias de cada peça podem existir ao mesmo tempo (na mesa ou já encaixadas).")]
    [SerializeField, Min(1)] private int limitePadraoPorPeca = 1;

    [Tooltip("Exceções ao limite padrão (ex.: Memória RAM = 2).")]
    [SerializeField] private List<LimitePeca> limitesEspecificos = new List<LimitePeca>();

    [Tooltip("Limite de peças no total, somando todos os tipos. 0 = sem limite total.")]
    [SerializeField, Min(0)] private int limiteTotalNaMesa = 0;

    [Tooltip("Local para spawnar as peças.")]
    [SerializeField] private List<Transform> LocaideSpawn;

    #endregion

    #region Estado Interno

    private Coroutine fadeRoutine;
    private bool isOpen = false;

    // Peças que a caixa já spawnou (e ainda existem), agrupadas pelo SOPieceData de origem
    private readonly Dictionary<SOPieceData, List<GameObject>> pecasSpawnadas = new Dictionary<SOPieceData, List<GameObject>>();
    private readonly Dictionary<Transform, GameObject> pecasNosLocaisSpawn = new Dictionary<Transform, GameObject>();
    private readonly HashSet<GameObject> pecasEmVoo = new HashSet<GameObject>();

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
        if (infoPeca == null)
        {
            Debug.LogWarning("[CaixaPecasUI] Não é possível spawnar uma peça sem SOPieceData.", this);
            StepChecker.Instance.NotificarBloqueio("Ainda não está na hora para essa peça, termine a etapa atual primeiramente.");
            return;
        }

        if (StepChecker.Instance != null
            && !StepChecker.Instance.PodeSpawnarPeca(infoPeca.ID, out string motivoEtapa))
        {
            StepChecker.Instance.NotificarBloqueio(motivoEtapa);
            return;
        }

        if (infoPeca.prefab == null || infoPeca.prefab.Count == 0)
        {
            Debug.LogWarning("[CaixaPecasUI] SOPieceData inválido ou sem prefabs cadastrados na lista!");
            return;
        }

        List<GameObject> prefabsValidos = infoPeca.prefab.FindAll(prefab => prefab != null);
        if (prefabsValidos.Count == 0)
        {
            Debug.LogWarning("[CaixaPecasUI] Nenhum prefab válido está cadastrado na lista da peça.");
            return;
        }

        // Limite de peças: avisa o jogador e mantém a caixa aberta
        if (!PodeSpawnar(infoPeca, out string motivo))
        {
            Debug.Log($"[CaixaPecasUI] Spawn bloqueado: {motivo}");
            if (StepChecker.Instance != null) StepChecker.Instance.NotificarBloqueio(motivo);
            return;
        }

        List<Transform> locaisLivres = ObterLocaisSpawnLivres();
        if (locaisLivres.Count == 0)
        {
            const string mensagem = "Não há espaço livre na mesa para outra peça. Mova uma peça antes de tentar novamente.";
            Debug.LogWarning($"[CaixaPecasUI] {mensagem}", this);
            if (StepChecker.Instance != null) StepChecker.Instance.NotificarBloqueio(mensagem);
            return;
        }

        GameObject prefabPeca = prefabsValidos[UnityEngine.Random.Range(0, prefabsValidos.Count)];
        Transform localSpawn = locaisLivres[UnityEngine.Random.Range(0, locaisLivres.Count)];
        Vector3 posicaoDestino = localSpawn.position;
        Quaternion rotacaoDestino = localSpawn.rotation;
        Vector3 posicaoInicial = pontoPartidaVoo != null ? pontoPartidaVoo.position : posicaoDestino;
        Quaternion rotacaoInicial = pontoPartidaVoo != null ? pontoPartidaVoo.rotation : rotacaoDestino;

        GameObject novaPeca = Instantiate(prefabPeca, posicaoDestino, rotacaoDestino);
        RegistrarSpawn(infoPeca, novaPeca, localSpawn, pontoPartidaVoo != null);

        if (pontoPartidaVoo != null)
        {
            Collider[] colliders = novaPeca.GetComponentsInChildren<Collider>(true);
            bool[] collidersAtivos = new bool[colliders.Length];
            for (int i = 0; i < colliders.Length; i++)
            {
                collidersAtivos[i] = colliders[i].enabled;
                colliders[i].enabled = false;
            }

            novaPeca.transform.SetPositionAndRotation(posicaoInicial, rotacaoInicial);
            CameraControllerMontagem.instance?.FollowSpawn(novaPeca.transform);
            StartCoroutine(AnimarSpawn(novaPeca, localSpawn, posicaoInicial, rotacaoInicial, colliders, collidersAtivos));
        }
        else
        {
            Debug.LogWarning("[CaixaPecasUI] Ponto de partida do voo não configurado; a peça aparecerá diretamente na mesa.", this);
            novaPeca.transform.SetPositionAndRotation(posicaoDestino, rotacaoDestino);
            novaPeca.transform.SetParent(localSpawn, true);
        }

        Debug.Log($"[CaixaPecasUI] Peça '{infoPeca.Nome}' spawnada com sucesso na mesa!");

        // Fecha a UI da caixa após selecionar a peça
        FecharCaixa();
    }

    private List<Transform> ObterLocaisSpawnLivres()
    {
        var locaisLivres = new List<Transform>();
        if (LocaideSpawn == null) return locaisLivres;

        var locaisUnicos = new HashSet<Transform>();
        foreach (Transform local in LocaideSpawn)
        {
            if (local == null || !locaisUnicos.Add(local)) continue;

            if (pecasNosLocaisSpawn.TryGetValue(local, out GameObject peca))
            {
                if (peca == null || (!pecasEmVoo.Contains(peca)
                    && Vector3.Distance(peca.transform.position, local.position) > 0.25f))
                {
                    pecasNosLocaisSpawn.Remove(local);
                }
                else
                {
                    continue;
                }
            }

            locaisLivres.Add(local);
        }

        return locaisLivres;
    }

    private IEnumerator AnimarSpawn(
        GameObject peca,
        Transform localSpawn,
        Vector3 posicaoInicial,
        Quaternion rotacaoInicial,
        Collider[] colliders,
        bool[] collidersAtivos)
    {
        Vector3 posicaoFinal = localSpawn.position;
        float alturaApex = Mathf.Max(posicaoInicial.y, posicaoFinal.y) + alturaCurvaVoo;
        Vector3 pontoControle1 = Vector3.Lerp(posicaoInicial, posicaoFinal, 0.25f);
        Vector3 pontoControle2 = Vector3.Lerp(posicaoInicial, posicaoFinal, 0.75f);
        pontoControle1.y = alturaApex;
        pontoControle2.y = alturaApex;
        float tempo = 0f;
        float duracao = Mathf.Max(0.1f, duracaoVoo);

        while (tempo < duracao && peca != null)
        {
            tempo += Time.deltaTime;
            float t = Mathf.Clamp01(tempo / duracao);
            float inverso = 1f - t;
            peca.transform.position =
                inverso * inverso * inverso * posicaoInicial
                + 3f * inverso * inverso * t * pontoControle1
                + 3f * inverso * t * t * pontoControle2
                + t * t * t * posicaoFinal;
            peca.transform.rotation = Quaternion.Slerp(rotacaoInicial, localSpawn.rotation, t);
            yield return null;
        }

        if (peca != null)
        {
            peca.transform.SetPositionAndRotation(localSpawn.position, localSpawn.rotation);
            peca.transform.SetParent(localSpawn, true);

            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null) colliders[i].enabled = collidersAtivos[i];
            }

            CameraControllerMontagem.instance?.StopFollowingSpawn(peca.transform);
        }

        if (peca != null) pecasEmVoo.Remove(peca);
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
            motivo = $"Limite de {limiteTotalNaMesa} peças na mesa atingido.";
            return false;
        }

        int limite = ObterLimite(peca);
        if (ContarDaPeca(peca) >= limite)
        {
            motivo = $"Você já pegou o máximo de \"{peca.Nome}\" ({limite}).";
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

    private void RegistrarSpawn(SOPieceData peca, GameObject instancia, Transform localSpawn, bool emVoo)
    {
        if (!pecasSpawnadas.TryGetValue(peca, out List<GameObject> lista))
        {
            lista = new List<GameObject>();
            pecasSpawnadas[peca] = lista;
        }

        lista.Add(instancia);
        pecasNosLocaisSpawn[localSpawn] = instancia;
        if (emVoo) pecasEmVoo.Add(instancia);
    }
    #endregion

  
}
