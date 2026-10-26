using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Informações de uma peça requerida para a montagem, configurável no Inspector.
/// 
/// 24/10 17:58 "Esses códigos são a prova viva que não há deus entre nós que não nos ama"
/// </summary>
[System.Serializable]
public class StepPieceInfo
{
    [Tooltip("ID da peça cadastrado na SOPieceData ou myID do ComponenteBaseMontagem.")]
    public string pieceID;

    [Tooltip("Descrição opcional ou nome amigável para exibição em UI.")]
    public string pieceName;

    [Tooltip("Indica se esta peça é obrigatória para considerar a etapa/montagem concluída.")]
    public bool isRequired = true;

    [Tooltip("Mínimo de peças prontas (encaixadas + parafusadas) com este ID para concluir a etapa. " +
             "O máximo é limitado naturalmente pela quantidade de slots na cena.")]
    [Min(1)] public int quantidadeMinima = 1;
}

/// <summary>
/// Uma etapa da montagem. As etapas são cumpridas em sequência, mas dentro de uma
/// mesma etapa a ordem das peças é livre.
/// </summary>
[System.Serializable]
public class EtapaMontagem
{
    [Tooltip("Nome da etapa exibido na UI. Ex.: 'Instalar o processador'.")]
    public string nome;

    [TextArea(2, 4)]
    [Tooltip("Instrução mostrada ao jogador durante esta etapa.")]
    public string instrucao;

    [Tooltip("Peças desta etapa. Evite repetir o mesmo ID em etapas diferentes.")]
    public List<StepPieceInfo> pecas = new List<StepPieceInfo>();

    [Tooltip("IDs de ações exigidas nesta etapa (ex.: 'pasta_termica'). " +
             "Quem executa a ação chama StepChecker.Instance.RegistrarAcao(id).")]
    public List<string> acoes = new List<string>();
}

/// <summary>
/// Controla o progresso geral da montagem: registra peças colocadas e ações realizadas,
/// libera as etapas em sequência, valida se a montagem foi concluída (considerando também
/// os parafusos/conectores de cada peça) e dispara os eventos para a UI/lógica do jogo.
/// </summary>
public class StepChecker : MonoBehaviour
{
    #region Singleton

    public static StepChecker Instance { get; private set; }

    #endregion

    #region Campos Serializados

    [Header("Montagem em Etapas (Sequencial)")]
    [Tooltip("Etapas na ordem em que devem ser feitas. Se estiver vazia, usa o modo antigo (lista 'Pecas Requeridas').")]
    [SerializeField] private List<EtapaMontagem> etapas = new List<EtapaMontagem>();

    [Header("Modo Antigo (sem sequência)")]
    [Tooltip("Usado apenas quando a lista de etapas está vazia.")]
    [SerializeField] private List<StepPieceInfo> pecasRequeridas = new List<StepPieceInfo>();

    [Header("Registro de Progresso")]
    [Tooltip("Lista de componentes que já foram posicionados na bancada/gabinete.")]
    [SerializeField] private List<ComponenteBaseMontagem> pecasColocadas = new List<ComponenteBaseMontagem>();

    #endregion

    #region Eventos

    // Eventos para atualização de UI / Lógica de Jogo
    public event Action<ComponenteBaseMontagem> OnPiecePlaced;
    public static event Action OnTudoFeito;

    /// <summary>
    /// Disparado quando a etapa atual muda. Ao concluir a última etapa, o índice é igual
    /// ao total de etapas e a etapa recebida é null.
    /// </summary>
    public static event Action<int, EtapaMontagem> OnEtapaMudou;

    /// <summary>
    /// Disparado quando o jogador tenta algo fora de hora (peça ou ação). Recebe a mensagem para a UI.
    /// </summary>
    public static event Action<string> OnAcaoBloqueada;

    #endregion

    #region Estado Interno

    // Garante que OnTudoFeito dispare apenas uma vez por montagem.
    private bool montagemConcluida = false;

    // Índice da etapa atual (== etapas.Count quando todas foram concluídas).
    private int etapaAtual = 0;

    // IDs das ações já realizadas (ex.: "pasta_termica").
    private readonly HashSet<string> acoesConcluidas = new HashSet<string>();

    #endregion

    #region Propriedades Públicas

    public IReadOnlyList<ComponenteBaseMontagem> PecasColocadas => pecasColocadas;
    public bool MontagemConcluida => montagemConcluida;

    /// <summary>
    /// True se a montagem está configurada em etapas sequenciais.
    /// </summary>
    public bool UsaEtapas => etapas != null && etapas.Count > 0;

    public int TotalEtapas => etapas != null ? etapas.Count : 0;
    public int IndiceEtapaAtual => etapaAtual;

    /// <summary>
    /// Etapa atual, ou null se não há etapas configuradas / todas já foram concluídas.
    /// </summary>
    public EtapaMontagem EtapaAtual =>
        (UsaEtapas && etapaAtual >= 0 && etapaAtual < etapas.Count) ? etapas[etapaAtual] : null;

    #endregion

    #region Ciclo de Vida (Unity)

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        // Só limpa se for a instância "oficial". Antes, o duplicado sendo destruído
        // zerava o Instance do StepChecker verdadeiro.
        if (Instance == this)
        {
            Instance = null;
        }
    }

    #endregion

    #region Registro de Peças e Ações

    /// <summary>
    /// Registra que uma peça foi encaixada e reavalia o progresso.
    /// </summary>
    public void RegisterPiecePlaced(ComponenteBaseMontagem peca)
    {
        if (peca == null || pecasColocadas.Contains(peca)) return;

        pecasColocadas.Add(peca);
        Debug.Log($"[StepChecker] Peça '{peca.myID}' encaixada ({pecasColocadas.Count} peças no total).");

        OnPiecePlaced?.Invoke(peca);

        AvaliarProgresso();
    }

    /// <summary>
    /// Registra que uma ação (ex.: aplicar pasta térmica) foi realizada.
    /// No modo em etapas, só é aceita se a ação pertencer à etapa atual (ou a uma anterior).
    /// Retorna true se a ação foi aceita.
    /// </summary>
    public bool RegistrarAcao(string acaoID)
    {
        if (string.IsNullOrEmpty(acaoID)) return false;
        if (acoesConcluidas.Contains(acaoID)) return false;

        if (UsaEtapas)
        {
            int indice = IndiceDaEtapaDaAcao(acaoID);
            if (indice < 0)
            {
                Debug.LogWarning($"[StepChecker] A ação '{acaoID}' não existe em nenhuma etapa.");
                return false;
            }

            if (indice > etapaAtual)
            {
                NotificarBloqueio($"Ainda não é hora dessa ação. Etapa atual: {NomeDaEtapa(etapaAtual)}.");
                return false;
            }
        }

        acoesConcluidas.Add(acaoID);
        Debug.Log($"[StepChecker] Ação '{acaoID}' realizada.");

        AvaliarProgresso();
        return true;
    }

    /// <summary>
    /// Retorna true se a ação já foi realizada.
    /// </summary>
    public bool AcaoConcluida(string acaoID)
    {
        return acoesConcluidas.Contains(acaoID);
    }

    /// <summary>
    /// Retorna true se a ação pode ser executada agora (pertence à etapa atual ou anterior e ainda não foi feita).
    /// </summary>
    public bool AcaoDisponivel(string acaoID)
    {
        if (string.IsNullOrEmpty(acaoID) || acoesConcluidas.Contains(acaoID)) return false;
        if (!UsaEtapas) return true;

        int indice = IndiceDaEtapaDaAcao(acaoID);
        return indice >= 0 && indice <= etapaAtual;
    }

    /// <summary>
    /// Limpa o progresso para permitir uma nova montagem (útil ao reiniciar a fase,
    /// já que este objeto persiste entre cenas).
    /// </summary>
    public void ResetarProgresso()
    {
        pecasColocadas.Clear();
        acoesConcluidas.Clear();
        montagemConcluida = false;
        etapaAtual = 0;

        if (UsaEtapas)
        {
            OnEtapaMudou?.Invoke(etapaAtual, EtapaAtual);
        }
    }

    #endregion

    #region Sequência (Autorização de Peças)

    /// <summary>
    /// Verifica se a peça pode ser colocada agora. No modo em etapas, a peça só é liberada
    /// se pertencer à etapa atual ou a uma anterior (ex.: uma 3ª RAM depois da etapa de memória).
    /// Peças cujo ID não aparece em nenhuma etapa são consideradas livres.
    /// </summary>
    public bool PodeColocarPeca(ComponenteBaseMontagem peca, out string motivo)
    {
        motivo = null;

        if (!UsaEtapas || peca == null) return true;
        if (etapaAtual >= etapas.Count) return true;

        int indice = IndiceDaEtapaDaPeca(peca.myID);
        if (indice < 0 || indice <= etapaAtual) return true;

        motivo = $"Ainda não é hora de instalar '{peca.PieceName}'. Etapa atual: {NomeDaEtapa(etapaAtual)}.";
        return false;
    }

    /// <summary>
    /// Avisa a UI/lógica de jogo que algo foi bloqueado por estar fora da sequência.
    /// </summary>
    public void NotificarBloqueio(string motivo)
    {
        if (string.IsNullOrEmpty(motivo)) return;

        Debug.Log($"[StepChecker] Bloqueado: {motivo}");
        OnAcaoBloqueada?.Invoke(motivo);
    }

    /// <summary>
    /// Verifica se um slot já foi liberado pela sequência: ele só aparece/colide quando a etapa
    /// da peça que ele aceita (por ID) já é a atual ou uma anterior. Slots cujos IDs não aparecem
    /// em nenhuma etapa são considerados livres.
    /// </summary>
    public bool SlotLiberado(IdentifiyerEncaixe slot)
    {
        if (!UsaEtapas || slot == null) return true;
        if (etapaAtual >= etapas.Count) return true;

        int menorIndice = IndiceDaEtapaDaPeca(slot.getID);

        foreach (string id in slot.AcceptedPieceIDs)
        {
            int indice = IndiceDaEtapaDaPeca(id);
            if (indice >= 0 && (menorIndice < 0 || indice < menorIndice))
            {
                menorIndice = indice;
            }
        }

        if (menorIndice < 0) return true;
        return menorIndice <= etapaAtual;
    }

    private int IndiceDaEtapaDaPeca(string pieceID)
    {
        if (string.IsNullOrEmpty(pieceID)) return -1;

        for (int i = 0; i < etapas.Count; i++)
        {
            if (etapas[i] == null || etapas[i].pecas == null) continue;

            foreach (var req in etapas[i].pecas)
            {
                if (req != null && req.pieceID == pieceID) return i;
            }
        }
        return -1;
    }

    private int IndiceDaEtapaDaAcao(string acaoID)
    {
        for (int i = 0; i < etapas.Count; i++)
        {
            if (etapas[i] != null && etapas[i].acoes != null && etapas[i].acoes.Contains(acaoID)) return i;
        }
        return -1;
    }

    private string NomeDaEtapa(int indice)
    {
        if (indice < 0 || indice >= etapas.Count) return "-";

        string nome = etapas[indice] != null ? etapas[indice].nome : null;
        return string.IsNullOrEmpty(nome) ? $"Etapa {indice + 1}" : nome;
    }

    #endregion

    #region Validação de Montagem

    /// <summary>
    /// Verifica se toda a montagem está concluída: todas as etapas (ou, no modo antigo, todas as
    /// peças exigidas) encaixadas na quantidade mínima E com seus parafusos concluídos.
    /// </summary>
    public bool IsComputerFullyAssembled()
    {
        // 1. Modo em etapas: todas as etapas precisam estar completas
        if (UsaEtapas)
        {
            foreach (var etapa in etapas)
            {
                if (!EtapaCompleta(etapa)) return false;
            }
            return true;
        }

        // 2. Modo antigo: lista de peças requeridas configurada no Inspector
        if (pecasRequeridas != null && pecasRequeridas.Count > 0)
        {
            return RequisitosDePecasAtendidos(pecasRequeridas);
        }

        // 3. Fallback: Se não há nada configurado, verifica se TODAS as peças na cena foram colocadas e parafusadas
        ComponenteBaseMontagem[] todasAsPecas = FindObjectsByType<ComponenteBaseMontagem>(FindObjectsSortMode.None);
        if (todasAsPecas.Length == 0) return false;

        foreach (ComponenteBaseMontagem peca in todasAsPecas)
        {
            if (!peca.IsFullyAssembledWithScrews)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Uma etapa está completa quando suas peças atingem a quantidade mínima (prontas e parafusadas)
    /// e todas as suas ações foram realizadas.
    /// </summary>
    private bool EtapaCompleta(EtapaMontagem etapa)
    {
        if (etapa == null) return true;

        if (!RequisitosDePecasAtendidos(etapa.pecas)) return false;

        if (etapa.acoes != null)
        {
            foreach (string acao in etapa.acoes)
            {
                if (!string.IsNullOrEmpty(acao) && !acoesConcluidas.Contains(acao)) return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Soma quantas peças de cada ID são exigidas e confere se a quantidade pronta
    /// (encaixada + parafusada) é suficiente.
    /// </summary>
    private bool RequisitosDePecasAtendidos(List<StepPieceInfo> lista)
    {
        if (lista == null) return true;

        var necessarias = new Dictionary<string, int>();
        foreach (var req in lista)
        {
            if (req == null || !req.isRequired) continue;

            string id = req.pieceID ?? "";
            necessarias.TryGetValue(id, out int atual);
            necessarias[id] = atual + Mathf.Max(1, req.quantidadeMinima);
        }

        foreach (var par in necessarias)
        {
            if (ContarPecasProntas(par.Key) < par.Value) return false;
        }

        return true;
    }

    private int ContarPecasProntas(string pieceID)
    {
        int prontas = 0;
        foreach (var p in pecasColocadas)
        {
            if (p != null && p.myID == pieceID && p.IsFullyAssembledWithScrews)
            {
                prontas++;
            }
        }
        return prontas;
    }

    #endregion

    #region Avaliação de Progresso

    /// <summary>
    /// Reavalia a etapa atual e, se tudo estiver completo, conclui a montagem.
    /// </summary>
    private void AvaliarProgresso()
    {
        AtualizarEtapa();
        TentarConcluirMontagem();
    }

    /// <summary>
    /// Recalcula a etapa atual (primeira etapa incompleta) e avisa a UI se ela mudou.
    /// </summary>
    private void AtualizarEtapa()
    {
        if (!UsaEtapas) return;

        int nova = etapas.Count;
        for (int i = 0; i < etapas.Count; i++)
        {
            if (!EtapaCompleta(etapas[i]))
            {
                nova = i;
                break;
            }
        }

        if (nova == etapaAtual) return;

        etapaAtual = nova;
        Debug.Log(nova < etapas.Count
            ? $"[StepChecker] Nova etapa: {NomeDaEtapa(nova)} ({nova + 1}/{etapas.Count})."
            : "[StepChecker] Todas as etapas foram concluídas.");

        OnEtapaMudou?.Invoke(etapaAtual, EtapaAtual);
    }

    /// <summary>
    /// Dispara OnTudoFeito uma única vez, quando a montagem estiver completa.
    /// </summary>
    private void TentarConcluirMontagem()
    {
        if (montagemConcluida) return;
        if (!IsComputerFullyAssembled()) return;

        montagemConcluida = true;
        Debug.Log("[StepChecker] O Computador está totalmente montado!");
        OnTudoFeito?.Invoke();
    }

    #endregion

    #region Progresso de Conectores

    /// <summary>
    /// Avalia o progresso de parafusos/conectores de um encaixe específico e,
    /// caso todos estejam concluídos, reavalia etapas e a montagem inteira.
    /// </summary>
    public void CheckConnectorProgress(IdentifiyerEncaixe encaixe)
    {
        if (encaixe == null) return;

        int total = encaixe.CompletedScrews.Count;
        int concluidos = encaixe.GetCompletedCount();

        Debug.Log($"[StepChecker] Progresso de parafusos em '{encaixe.getID}': {concluidos}/{total}.");

        if (encaixe.IsFullyAssembled())
        {
            Debug.Log($"[StepChecker] Todos os parafusos/conectores de '{encaixe.getID}' foram instalados!");
            AvaliarProgresso();
        }
    }

    #endregion
}
