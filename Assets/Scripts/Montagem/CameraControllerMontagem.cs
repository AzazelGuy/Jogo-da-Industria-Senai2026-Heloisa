using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Estados possíveis para o movimento da câmera.
/// </summary>
public enum CameraState
{
    Overview,   // Visão geral da bancada
    Moving,     // Em transição suave
    Focused     // Focada em uma peça/parafuso
}

/// <summary>
/// Gerenciador do movimento, rotação e aproximação (zoom) da câmera principal.
/// Controla transições suaves entre a visão geral da bancada e o foco em peças,
/// além de aplicar um leve "edge pan" (parallax) baseado na posição do mouse.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraControllerMontagem : MonoBehaviour
{
    #region Singleton

    public static CameraControllerMontagem instance;

    #endregion

    #region Campos Serializados - Movimento

    [Header("Configurações de Movimento")]
    [SerializeField, Tooltip("Duração da transição da câmera em segundos.")]
    private float duracaoDaTransicao = 0.8f;

    [SerializeField, Tooltip("Curva de suavização da animação da câmera.")]
    private AnimationCurve curvaDeTransicao = AnimationCurve.EaseInOut(0, 0, 1, 1);

    #endregion

    #region Campos Serializados - Edge Pan (Parallax do Mouse)

    [Header("Configurações de Edge Pan (Parallax do Mouse)")]
    [SerializeField, Tooltip("Habilita/Desabilita o movimento leve quando o mouse vai para as bordas.")]
    private bool usarEdgePan = true;

    [SerializeField, Tooltip("Intensidade máxima de deslocamento em X e Y.")]
    private Vector2 limitePan = new Vector2(0.3f, 0.2f);

    [SerializeField, Tooltip("Velocidade de suavização do movimento do pan.")]
    private float velocidadeSuavizacaoPan = 5f;

    #endregion

    #region Estado Interno

    // Referências e Estados Internos
    private Camera cameraAlvo;
    private CameraState estadoAtual = CameraState.Overview;

    // Transformações Iniciais (Bancada)
    private Vector3 posicaoInicial;
    private Quaternion rotacaoInicial;
    private float fovInicial;

    // Posições Internas para o cálculo do Pan
    private Vector3 posicaoBaseTarget;
    private Vector3 offsetPanAtual;

    // Controle de Corrotina
    private Coroutine rotinaDeTransicaoAtiva;

    #endregion

    #region Eventos

    // Eventos C# disparados quando a câmera termina de focar/voltar
    public event Action OnFocusReached;
    public event Action OnReturnToOverview;

    #endregion

    #region Propriedades Públicas

    public CameraState CurrentState => estadoAtual;

    #endregion

    #region Ciclo de Vida (Unity)

    private void Awake()
    {
        // Garante que exista apenas uma instância ativa do controlador de câmera
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        cameraAlvo = GetComponent<Camera>();
        SaveInitialTransform();
    }

    private void LateUpdate()
    {
        AplicarEdgePan();
    }

    #endregion

    #region API Pública

    /// <summary>
    /// Salva a posição, rotação e campo de visão (FOV) atuais como o ponto inicial.
    /// </summary>
    public void SaveInitialTransform()
    {
        posicaoInicial = transform.position;
        rotacaoInicial = transform.rotation;
        fovInicial = cameraAlvo.fieldOfView;
        posicaoBaseTarget = posicaoInicial;
        estadoAtual = CameraState.Overview;
    }

    /// <summary>
    /// Move a câmera suavemente para o ponto de foco informado.
    /// </summary>
    public void FocusOnPiece(FocusPoint pontoDeFoco)
    {
        if (pontoDeFoco == null) return;

        Vector3 posicaoAlvo = pontoDeFoco.GetWorldTargetPosition();
        Quaternion rotacaoAlvo = pontoDeFoco.GetWorldTargetRotation();
        float fovAlvo = pontoDeFoco.focusedFOV;

        IniciarTransicao(posicaoAlvo, rotacaoAlvo, fovAlvo, () =>
        {
            estadoAtual = CameraState.Focused;
            OnFocusReached?.Invoke();
        });
    }

    /// <summary>
    /// Retorna a câmera para a visão geral da bancada.
    /// </summary>
    public void ReturnToOverview()
    {
        if (estadoAtual == CameraState.Overview) return;

        IniciarTransicao(posicaoInicial, rotacaoInicial, fovInicial, () =>
        {
            estadoAtual = CameraState.Overview;
            OnReturnToOverview?.Invoke();
        });
    }

    #endregion

    #region Transição de Câmera (Interno)

    /// <summary>
    /// Interrompe uma transição em andamento (se houver) e inicia uma nova.
    /// </summary>
    private void IniciarTransicao(Vector3 posicaoAlvo, Quaternion rotacaoAlvo, float fovAlvo, Action aoConcluir)
    {
        if (rotinaDeTransicaoAtiva != null)
        {
            StopCoroutine(rotinaDeTransicaoAtiva);
        }

        estadoAtual = CameraState.Moving;
        rotinaDeTransicaoAtiva = StartCoroutine(RotinaDeTransicao(posicaoAlvo, rotacaoAlvo, fovAlvo, aoConcluir));
    }

    /// <summary>
    /// Corrotina que interpola posição, rotação e FOV ao longo do tempo,
    /// usando a curva de suavização configurada.
    /// </summary>
    private IEnumerator RotinaDeTransicao(Vector3 posicaoAlvo, Quaternion rotacaoAlvo, float fovAlvo, Action aoConcluir)
    {
        Vector3 posicaoInicio = posicaoBaseTarget;
        Quaternion rotacaoInicio = transform.rotation;
        float fovInicio = cameraAlvo.fieldOfView;

        float tempoDecorrido = 0f;

        while (tempoDecorrido < duracaoDaTransicao)
        {
            tempoDecorrido += Time.deltaTime;
            float tempoNormalizado = Mathf.Clamp01(tempoDecorrido / duracaoDaTransicao);
            float valorDaCurva = curvaDeTransicao.Evaluate(tempoNormalizado);

            posicaoBaseTarget = Vector3.Lerp(posicaoInicio, posicaoAlvo, valorDaCurva);
            transform.rotation = Quaternion.Slerp(rotacaoInicio, rotacaoAlvo, valorDaCurva);
            cameraAlvo.fieldOfView = Mathf.Lerp(fovInicio, fovAlvo, valorDaCurva);

            yield return null;
        }

        // Garante que os valores finais fiquem exatamente nos alvos (evita erro de arredondamento)
        posicaoBaseTarget = posicaoAlvo;
        transform.rotation = rotacaoAlvo;
        cameraAlvo.fieldOfView = fovAlvo;

        rotinaDeTransicaoAtiva = null;
        aoConcluir?.Invoke();
    }

    #endregion

    #region Edge Pan (Interno)

    /// <summary>
    /// Aplica o deslocamento sutil de câmera baseado na posição do mouse relativa ao centro da tela.
    /// Não é aplicado durante transições (estado Moving).
    /// </summary>
    private void AplicarEdgePan()
    {
        if (!usarEdgePan || estadoAtual == CameraState.Moving)
        {
            transform.position = posicaoBaseTarget;
            return;
        }

        // Normaliza a posição do mouse na tela de -1 até 1 (onde (0,0) é o centro)
        float mouseXNormalizado = Mathf.Clamp((Input.mousePosition.x / Screen.width - 0.5f) * 2f, -1f, 1f);
        float mouseYNormalizado = Mathf.Clamp((Input.mousePosition.y / Screen.height - 0.5f) * 2f, -1f, 1f);

        // Converte para offset nos eixos locais da câmera
        Vector3 offsetDesejado = (transform.right * mouseXNormalizado * limitePan.x) +
                                 (transform.up * mouseYNormalizado * limitePan.y);

        // Interpola o offset suavemente
        offsetPanAtual = Vector3.Lerp(offsetPanAtual, offsetDesejado, Time.deltaTime * velocidadeSuavizacaoPan);

        // Aplica a posição final mantendo a base segura
        transform.position = posicaoBaseTarget + offsetPanAtual;
    }

    #endregion
}
