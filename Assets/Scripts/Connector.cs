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
/// Controla peças encaixáveis por clique contínuo ou ação direta.
/// </summary>
public class Connector : MonoBehaviour, ISelectable
{
    [Header("Configurações do Conector")]
    [SerializeField, Tooltip("Modo de encaixe do conector.")]
    private ConnectorType tipoDeConexao = ConnectorType.LinearMove;

    [SerializeField, Tooltip("Velocidade do movimento ou rotação.")]
    private float velocidade = 1.5f;

    [SerializeField, Tooltip("Tolerância de distância/ângulo para considerar o movimento concluído.")]
    private float limiteDeConclusao = 0.01f;

    [Header("Transformações de Destino")]
    [SerializeField] private Vector3 posicaoDesejada;
    [SerializeField] private Vector3 rotacaoDesejadaEuler;

    [Header("Animação (Se Tipo == AnimationTrigger)")]
    [SerializeField] private Animator animador;
    [SerializeField] private string nomeDoGatilhoAnimacao = "Connect";

    [Header("Referências de Encaixe")]
    [SerializeField] private IdentifiyerEncaixe encaixeIdentifiyer;
    [SerializeField] private EncaixeBase encaixeBase;
    [SerializeField] private int indiceDoConector;

    private bool estaConectado;

    // --- Métodos de Configuração Externa ---

    public void SetPlacement(IdentifiyerEncaixe alvo, int indice = 0)
    {
        encaixeIdentifiyer = alvo;
        indiceDoConector = indice;
    }

    public void SetPlacement(EncaixeBase alvo, int indice = 0)
    {
        encaixeBase = alvo;
        indiceDoConector = indice;
    }

    public void SetTarget(Vector3 posicao)
    {
        posicaoDesejada = posicao;
    }

    public void SetTarget(Vector3 posicao, Vector3 rotacaoEuler)
    {
        posicaoDesejada = posicao;
        rotacaoDesejadaEuler = rotacaoEuler;
    }

    // --- Lógica de Interação ---

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

    private void ProcessarAnimacao()
    {
        if (animador != null && !string.IsNullOrEmpty(nomeDoGatilhoAnimacao))
        {
            animador.SetTrigger(nomeDoGatilhoAnimacao);
        }
        ConcluirConexao();
    }

    public void ConcluirConexao()
    {
        if (estaConectado) return;

        estaConectado = true;

        // Ajusta posição/rotação final no encerramento
        if (tipoDeConexao == ConnectorType.LinearMove)
        {
            transform.position = posicaoDesejada;
        }
        else if (tipoDeConexao == ConnectorType.Rotation)
        {
            transform.localRotation = Quaternion.Euler(rotacaoDesejadaEuler);
        }

        // 1. Notifica o IdentifiyerEncaixe (se assinalado)
        if (encaixeIdentifiyer != null)
        {
            encaixeIdentifiyer.NotifyConnectorPlaced(indiceDoConector);

            // Checa progresso no StepChecker
            if (StepChecker.Instance != null)
            {
                StepChecker.Instance.CheckConnectorProgress(encaixeIdentifiyer);
            }
        }

        // 2. Notifica o EncaixeBase (caso utilize essa estrutura)
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

    // --- Métodos da Interface ISelectable ---
    public void OnSelect() { }
    public void OnDeselect() { }
    public void OnDoubleClick() { }
}