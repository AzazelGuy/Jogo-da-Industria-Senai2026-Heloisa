using UnityEngine;

/// <summary>
/// Tipos de intera��o e movimento para conectores, parafusos, alavancas e cabos.
/// </summary>
public enum ConnectorType
{
    LinearMove,       // Movimento linear (parafusos, cabos)
    Rotation,         // Rota��o (travas, alavancas)
    AnimationTrigger, // Dispara uma anima��o espec�fica no Animator
    InstantSnap       // Encaixe instant�neo
}

/// <summary>
/// Controla pe�as encaix�veis por clique cont�nuo ou a��o direta.
/// </summary>
public class ConnectorMontagem : MonoBehaviour, ISelectable
{
    [Header("Configura��es do Conector")]
    [SerializeField, Tooltip("Modo de encaixe do conector.")]
    private ConnectorType tipoDeConexao = ConnectorType.LinearMove;

    [SerializeField, Tooltip("Velocidade do movimento ou rota��o.")]
    private float velocidade = 1.5f;

    [SerializeField, Tooltip("Toler�ncia de dist�ncia/�ngulo para considerar o movimento conclu�do.")]
    private float limiteDeConclusao = 0.01f;

    [Header("Transforma��es de Destino")]
    [SerializeField] private Vector3 posicaoDesejada;
    [SerializeField] private Vector3 rotacaoDesejadaEuler;

    [Header("Anima��o (Se Tipo == AnimationTrigger)")]
    [SerializeField] private Animator animador;
    [SerializeField] private string nomeDoGatilhoAnimacao = "Connect";

    [Header("Refer�ncias de Encaixe")]
    [SerializeField] private IdentifiyerEncaixe encaixeIdentifiyer;
    [SerializeField] private EncaixeBaseMontagem encaixeBase;
    [SerializeField] private int indiceDoConector;

    [SerializeField] private AudioClip SFXPlaced;

    private bool estaConectado;

    // --- M�todos de Configura��o Externa ---

    public void SetPlacement(IdentifiyerEncaixe alvo, int indice = 0)
    {
        encaixeIdentifiyer = alvo;
        encaixeBase = null;
        indiceDoConector = indice;
    }

    public void SetPlacement(EncaixeBaseMontagem alvo, int indice = 0)
    {
        encaixeBase = alvo;
        encaixeIdentifiyer = null;
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

    // --- L�gica de Intera��o ---

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
        AudioManager.Instance.PlaySFX(SFXPlaced);
        // Ajusta posi��o/rota��o final no encerramento
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

        // 2. Notifica o EncaixeBaseMontagem (caso utilize essa estrutura)
        if (encaixeBase != null)
        {
            encaixeBase.OnConectorPlaced(indiceDoConector);
        }

        // 3. Desativa o Collider para evitar intera��es p�s-encaixe
        if (TryGetComponent<Collider>(out var colisor))
        {
            colisor.enabled = false;
        }
    }

    // --- M�todos da Interface ISelectable ---
    public void OnSelect() { }
    public void OnDeselect() { }
    public void OnDoubleClick() { }
}