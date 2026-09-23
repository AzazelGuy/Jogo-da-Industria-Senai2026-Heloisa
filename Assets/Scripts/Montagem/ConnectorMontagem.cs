using UnityEngine;

/// <summary>
/// Tipos de interação e movimento para conectores, parafusos, alavancas e cabos.
/// </summary>
public enum ConnectorType
{
    LinearMove,       // Movimento linear (parafusos, cabos)
    Rotation,         // Rotação (travas, alavancas)
    AnimationTrigger, // Dispara uma animação específica no Animator
    InstantSnap       // Encaixe instantâneo
}

/// <summary>
/// Controla peças encaixáveis por clique contínuo (hold) ou ação direta,
/// movendo/rotacionando o objeto até a posição/rotação desejada e notificando
/// o IdentifiyerEncaixe / EncaixeBaseMontagem correspondente ao concluir.
/// </summary>
public class ConnectorMontagem : MonoBehaviour, ISelectable
{
    #region Campos Serializados - Configuração do Conector

    [Header("Configurações do Conector")]
    [SerializeField, Tooltip("Modo de encaixe do conector.")]
    private ConnectorType tipoDeConexao = ConnectorType.LinearMove;

    [SerializeField, Tooltip("Velocidade do movimento ou rotação.")]
    private float velocidade = 1.5f;

    [SerializeField, Tooltip("Tolerância de distância/ângulo para considerar o movimento concluído.")]
    private float limiteDeConclusao = 0.01f;

    #endregion

    #region Campos Serializados - Transformações de Destino

    [Header("Transformações de Destino")]
    [SerializeField] private Vector3 posicaoDesejada;
    [SerializeField] private Vector3 rotacaoDesejadaEuler;

    #endregion

    #region Campos Serializados - Animação

    [Header("Animação (Se Tipo == AnimationTrigger)")]
    [SerializeField] private Animator animador;
    [SerializeField] private string nomeDoGatilhoAnimacao = "Connect";

    #endregion

    #region Campos Serializados - Referências de Encaixe

    [Header("Referências de Encaixe")]
    [SerializeField] private IdentifiyerEncaixe encaixeIdentifiyer;
    [SerializeField] private EncaixeBaseMontagem encaixeBase;
    [SerializeField] private int indiceDoConector;

    [SerializeField] private AudioClip SFXPlaced;

    #endregion

    #region Estado Interno

    private bool estaConectado;

    #endregion

    #region Configuração Externa (Setters)

    /// <summary>
    /// Define o encaixe de destino (via IdentifiyerEncaixe) e o índice do conector nesse encaixe.
    /// </summary>
    public void SetPlacement(IdentifiyerEncaixe alvo, int indice = 0)
    {
        encaixeIdentifiyer = alvo;
        encaixeBase = null;
        indiceDoConector = indice;
    }

    /// <summary>
    /// Define o encaixe de destino (via EncaixeBaseMontagem) e o índice do conector nesse encaixe.
    /// </summary>
    public void SetPlacement(EncaixeBaseMontagem alvo, int indice = 0)
    {
        encaixeBase = alvo;
        encaixeIdentifiyer = null;
        indiceDoConector = indice;
    }

    /// <summary>
    /// Define apenas a posição de destino do conector.
    /// </summary>
    public void SetTarget(Vector3 posicao)
    {
        posicaoDesejada = posicao;
    }

    /// <summary>
    /// Define a posição e a rotação (em Euler) de destino do conector.
    /// </summary>
    public void SetTarget(Vector3 posicao, Vector3 rotacaoEuler)
    {
        posicaoDesejada = posicao;
        rotacaoDesejadaEuler = rotacaoEuler;
    }

    #endregion

    #region Lógica de Interação

    /// <summary>
    /// Chamado continuamente enquanto o jogador mantém o clique sobre o conector.
    /// Processa o tipo de conexão configurado até a conclusão.
    /// </summary>
    public void OnHold()
    {
        if (estaConectado) return;

        switch (tipoDeConexao)
        {
            case ConnectorType.LinearMove:
                ProcessarMovimentoLinear();
                break;

            case ConnectorType.Rotation:
                ProcessarRotacao();
                break;

            case ConnectorType.AnimationTrigger:
                ProcessarAnimacao();
                break;

            case ConnectorType.InstantSnap:
                ConcluirConexao();
                break;
        }
    }

    /// <summary>
    /// Move o conector linearmente em direção à posição desejada; conclui ao chegar perto o suficiente.
    /// </summary>
    private void ProcessarMovimentoLinear()
    {
        transform.position = Vector3.MoveTowards(
            transform.position,
            posicaoDesejada,
            velocidade * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, posicaoDesejada) <= limiteDeConclusao)
        {
            ConcluirConexao();
        }
    }

    /// <summary>
    /// Rotaciona o conector em direção à rotação desejada; conclui ao chegar perto o suficiente do ângulo alvo.
    /// </summary>
    private void ProcessarRotacao()
    {
        Quaternion rotacaoAlvo = Quaternion.Euler(rotacaoDesejadaEuler);
        transform.localRotation = Quaternion.RotateTowards(
            transform.localRotation,
            rotacaoAlvo,
            velocidade * 100f * Time.deltaTime
        );

        if (Quaternion.Angle(transform.localRotation, rotacaoAlvo) <= limiteDeConclusao)
        {
            ConcluirConexao();
        }
    }

    /// <summary>
    /// Dispara o gatilho de animação configurado e conclui a conexão imediatamente.
    /// </summary>
    private void ProcessarAnimacao()
    {
        if (animador != null && !string.IsNullOrEmpty(nomeDoGatilhoAnimacao))
        {
            animador.SetTrigger(nomeDoGatilhoAnimacao);
        }
        ConcluirConexao();
    }

    #endregion

    #region Conclusão da Conexão

    /// <summary>
    /// Finaliza a conexão: ajusta a transformação final, toca o SFX, notifica o encaixe
    /// (IdentifiyerEncaixe e/ou EncaixeBaseMontagem) e desativa o collider.
    /// </summary>
    public void ConcluirConexao()
    {
        if (estaConectado) return;

        estaConectado = true;
        AudioManager.Instance.PlaySFX(SFXPlaced);

        // Ajusta posição/rotação final no encerramento
        if (tipoDeConexao == ConnectorType.LinearMove)
        {
            transform.position = posicaoDesejada;
        }
        else if (tipoDeConexao == ConnectorType.Rotation)
        {
            transform.localRotation = Quaternion.Euler(rotacaoDesejadaEuler);
        }

        // 1. Notifica o IdentifiyerEncaixe (se atribuído)
        if (encaixeIdentifiyer != null)
        {
            encaixeIdentifiyer.NotifyConnectorPlaced(indiceDoConector);

            // Checa progresso no StepChecker
            if (StepChecker.Instance != null)
            {
                StepChecker.Instance.CheckConnectorProgress(encaixeIdentifiyer);
            }
        }

        // 2. Notifica o EncaixeBaseMontagem (caso utilize essa estrutura)
        if (encaixeBase != null)
        {
            encaixeBase.OnConectorPlaced(indiceDoConector);
        }

        // 3. Desativa o Collider para evitar interações pós-encaixe
        if (TryGetComponent<Collider>(out var colisor))
        {
            colisor.enabled = false;
        }
    }

    #endregion

    #region Implementação de ISelectable

    // Este conector não usa seleção/duplo clique - apenas OnHold()
    public void OnSelect() { }
    public void OnDeselect() { }
    public void OnDoubleClick() { }

    #endregion
}
