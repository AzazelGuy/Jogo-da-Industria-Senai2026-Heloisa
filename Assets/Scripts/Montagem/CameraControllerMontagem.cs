using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Estados possíveis da câmera.
/// </summary>
public enum CameraState
{
    Overview,   // Visão geral da bancada (olhar livre com o mouse)
    Moving,     // Em transição suave entre dois pontos
    Focused     // Focada em uma peça/parafuso
}

/// <summary>
/// Controlador único da câmera principal. Junta três comportamentos:
///
/// 1) VISÃO GERAL: duas "estações" (esquerda/direita). Mouse na borda direita leva a câmera
///    para a estação direita; na borda esquerda volta pra principal olhando um pouco pra esquerda.
///    Mouse no topo/rodapé inclina a câmera para cima/baixo.
/// 2) FOCO: transições suaves (corrotina + AnimationCurve) até um FocusPoint, e de volta.
///    Quando focada, aplica um leve parallax (edge pan) conforme a posição do mouse.
/// 3) MOVIMENTO NATURAL: balanço sutil (bob/sway) para a câmera parecer "viva".
///
/// COMO FUNCIONA: a câmera tem uma "pose base" (posição, rotação, FOV) e, por cima dela, camadas
/// de offset (olhar, pan, balanço). A pose final é base + offsets, aplicada uma vez por frame no LateUpdate.
/// Essa separação evita os "pulos" que acontecem quando duas lógicas brigam pelo mesmo transform.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraControllerMontagem : MonoBehaviour
{
    #region Singleton

    public static CameraControllerMontagem instance;

    #endregion

    #region Constantes

    // Quanto do bob vertical (em unidades) vira inclinação (em graus) da câmera.
    private const float FATOR_BOB_ROTACAO = 10f;

    // Proporção entre a velocidade do sway (rotação lateral) e a do bob.
    private const float FATOR_VELOCIDADE_SWAY = 0.7f;

    #endregion

    #region Campos Serializados - Estações (Visão Geral)

    [Header("Estações da Visão Geral (opcionais)")]
    [SerializeField, Tooltip("Pose principal da bancada. Se vazio, usa a pose da câmera ao iniciar.")]
    private Transform estacaoPrincipal;

    [SerializeField, Tooltip("Pose alternativa ao levar o mouse para a borda direita. Se vazio, a troca de estação é desativada.")]
    private Transform estacaoDireita;

    #endregion

    #region Campos Serializados - Transição

    [Header("Transição de Foco")]
    [SerializeField, Min(0.01f), Tooltip("Duração da transição da câmera em segundos.")]
    private float duracaoDaTransicao = 0.8f;

    [SerializeField, Tooltip("Curva de suavização da transição.")]
    private AnimationCurve curvaDeTransicao = AnimationCurve.EaseInOut(0, 0, 1, 1);

    #endregion

    #region Campos Serializados - Olhar com o Mouse

    [Header("Olhar com o Mouse (Visão Geral)")]
    [SerializeField, Range(0f, 0.2f), Tooltip("Faixa da borda horizontal (0 a 1 da tela) que dispara a troca de estação.")]
    private float limiteBordaHorizontal = 0.05f;

    [SerializeField, Tooltip("Ângulo extra (graus) ao olhar para a esquerda.")]
    private float anguloOlharEsquerda = 15f;

    [SerializeField, Range(0f, 0.2f), Tooltip("Faixa da borda vertical (0 a 1 da tela) que dispara inclinação.")]
    private float limiteBordaVertical = 0.08f;

    [SerializeField, Tooltip("Ângulo máximo (graus) ao olhar para cima.")]
    private float anguloOlharCima = 12f;

    [SerializeField, Tooltip("Ângulo máximo (graus) ao olhar para baixo.")]
    private float anguloOlharBaixo = 12f;

    [SerializeField, Tooltip("Velocidade de suavização da posição entre estações.")]
    private float velocidadePosicao = 5f;

    [SerializeField, Tooltip("Velocidade de suavização da rotação (troca de estação e olhar).")]
    private float velocidadeRotacao = 4f;

    #endregion

    #region Campos Serializados - Movimento Natural

    [Header("Movimento Natural (Bob / Sway)")]
    [SerializeField, Tooltip("Amplitude do balanço vertical (unidades).")]
    private float amplitudeBob = 0.03f;

    [SerializeField, Tooltip("Velocidade do balanço.")]
    private float velocidadeBob = 0.8f;

    [SerializeField, Tooltip("Amplitude da rotação lateral (graus).")]
    private float amplitudeSway = 1f;

    [SerializeField, Range(0f, 1f), Tooltip("Intensidade do balanço quando focada em uma peça (0 = parado, 1 = igual à visão geral).")]
    private float intensidadeBalancoFocado = 0.3f;

    [SerializeField, Min(0.01f), Tooltip("Tempo (s) para ligar/desligar o balanço ao mudar de estado.")]
    private float duracaoFadeBalanco = 0.25f;

    #endregion

    #region Campos Serializados - Edge Pan

    [Header("Edge Pan (Parallax do Mouse)")]
    [SerializeField, Tooltip("Habilita o movimento leve conforme o mouse se afasta do centro da tela.")]
    private bool usarEdgePan = true;

    [SerializeField, Tooltip("Aplicar o pan também na visão geral? (Normalmente só faz sentido focada.)")]
    private bool panNaVisaoGeral = false;

    [SerializeField, Tooltip("Deslocamento máximo em X e Y (eixos locais da câmera).")]
    private Vector2 limitePan = new Vector2(0.3f, 0.2f);

    [SerializeField, Tooltip("Velocidade de suavização do pan.")]
    private float velocidadeSuavizacaoPan = 5f;

    #endregion

    #region Estado Interno

    private Camera cameraAlvo;
    private CameraState estadoAtual = CameraState.Overview;
    private bool inicializado;

    // Pose de reserva da visão geral (usada quando não há estacaoPrincipal)
    private Vector3 posicaoInicial;
    private Quaternion rotacaoInicial;
    private float fovInicial;

    // Pose BASE: é o que as transições e a troca de estação controlam.
    private Vector3 posicaoBase;
    private Quaternion rotacaoBase;
    private float fovBase;

    // Camadas de offset aplicadas por cima da pose base
    private float pitchAtual;           // inclinação vertical atual (graus)
    private float yawAtual;             // giro horizontal extra atual (graus)
    private Vector3 offsetPanLocal;     // offset do parallax, em espaço LOCAL da câmera
    private float pesoBalanco = 1f;     // 0..1: quanto do bob/sway está ativo

    // Alvos do olhar (calculados a partir do mouse na visão geral)
    private float pitchAlvo;
    private float yawAlvo;

    private bool naEstacaoDireita;
    private bool retornandoParaVisaoGeral;
    private Coroutine rotinaDeTransicaoAtiva;

    #endregion

    #region Eventos

    /// <summary>Disparado quando a câmera termina de chegar ao ponto de foco.</summary>
    public event Action OnFocusReached;

    /// <summary>Disparado quando a câmera termina de voltar para a visão geral.</summary>
    public event Action OnReturnToOverview;

    #endregion

    #region Propriedades Públicas

    public CameraState CurrentState => estadoAtual;

    #endregion

    #region Ciclo de Vida (Unity)

    private void Awake()
    {
        // Garante uma única instância. Destruímos só o componente (não o GameObject inteiro),
        // porque o objeto duplicado pode ter outras coisas importantes penduradas nele.
        if (instance != null && instance != this)
        {
            Debug.LogWarning("[CameraControllerMontagem] Já existe uma instância ativa. Removendo o componente duplicado.", this);
            Destroy(this);
            return;
        }

        instance = this;
        cameraAlvo = GetComponent<Camera>();
        SaveInitialTransform();
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private void LateUpdate()
    {
        // Ordem importa: primeiro decide os alvos, depois suaviza, por fim aplica no transform.
        AtualizarAlvosDeOlhar();
        AtualizarPoseBaseNaVisaoGeral();
        AtualizarCamadas();
        AplicarPoseFinal();
    }

    private void OnValidate()
    {
        // Evita valores inválidos digitados no Inspector
        duracaoDaTransicao = Mathf.Max(0.01f, duracaoDaTransicao);
        duracaoFadeBalanco = Mathf.Max(0.01f, duracaoFadeBalanco);
    }

    #endregion

    #region API Pública

    /// <summary>
    /// Salva a pose atual como pose inicial (reserva) e reposiciona a base na estação principal.
    /// Chame com a câmera em repouso.
    /// </summary>
    public void SaveInitialTransform()
    {
        // Se já estamos rodando, usa a pose BASE (sem bob/pan) para não gravar um offset por engano.
        posicaoInicial = inicializado ? posicaoBase : transform.position;
        rotacaoInicial = inicializado ? rotacaoBase : transform.rotation;
        fovInicial = inicializado ? fovBase : cameraAlvo.fieldOfView;

        if (rotinaDeTransicaoAtiva != null)
        {
            StopCoroutine(rotinaDeTransicaoAtiva);
            rotinaDeTransicaoAtiva = null;
        }

        naEstacaoDireita = false;
        retornandoParaVisaoGeral = false;

        Pose estacao = ObterPoseDaEstacao(false);
        posicaoBase = estacao.position;
        rotacaoBase = estacao.rotation;
        fovBase = fovInicial;

        estadoAtual = CameraState.Overview;
        inicializado = true;
    }

    /// <summary>
    /// Move a câmera suavemente até o ponto de foco informado.
    /// </summary>
    public void FocusOnPiece(FocusPoint pontoDeFoco)
    {
        if (pontoDeFoco == null) return;

        retornandoParaVisaoGeral = false;

        IniciarTransicao(
            pontoDeFoco.GetWorldTargetPosition(),
            pontoDeFoco.GetWorldTargetRotation(),
            pontoDeFoco.focusedFOV,
            () =>
            {
                estadoAtual = CameraState.Focused;
                OnFocusReached?.Invoke();
            });
    }

    /// <summary>
    /// Retorna a câmera para a visão geral da bancada (na estação em que ela estava).
    /// </summary>
    public void ReturnToOverview()
    {
        // Já está na visão geral, ou já está voltando: não faz nada
        if (estadoAtual == CameraState.Overview) return;
        if (estadoAtual == CameraState.Moving && retornandoParaVisaoGeral) return;

        retornandoParaVisaoGeral = true;
        Pose estacao = ObterPoseDaEstacao(naEstacaoDireita);

        IniciarTransicao(estacao.position, estacao.rotation, fovInicial, () =>
        {
            retornandoParaVisaoGeral = false;
            estadoAtual = CameraState.Overview;
            OnReturnToOverview?.Invoke();
        });
    }

    #endregion

    #region Transição de Câmera

    /// <summary>
    /// Interrompe qualquer transição em andamento e inicia uma nova.
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
    /// Interpola a pose BASE (posição, rotação, FOV) usando a curva configurada.
    /// Os offsets (olhar, pan, balanço) somem suavemente sozinhos durante o estado Moving.
    /// </summary>
    private IEnumerator RotinaDeTransicao(Vector3 posicaoAlvo, Quaternion rotacaoAlvo, float fovAlvo, Action aoConcluir)
    {
        Vector3 posicaoInicio = posicaoBase;
        Quaternion rotacaoInicio = rotacaoBase;
        float fovInicio = fovBase;

        float duracao = Mathf.Max(0.01f, duracaoDaTransicao);
        float tempoDecorrido = 0f;

        while (tempoDecorrido < duracao)
        {
            tempoDecorrido += Time.deltaTime;
            float t = curvaDeTransicao.Evaluate(Mathf.Clamp01(tempoDecorrido / duracao));

            posicaoBase = Vector3.Lerp(posicaoInicio, posicaoAlvo, t);
            rotacaoBase = Quaternion.Slerp(rotacaoInicio, rotacaoAlvo, t);
            fovBase = Mathf.Lerp(fovInicio, fovAlvo, t);

            yield return null;
        }

        // Crava nos valores finais para não sobrar erro de arredondamento
        posicaoBase = posicaoAlvo;
        rotacaoBase = rotacaoAlvo;
        fovBase = fovAlvo;

        rotinaDeTransicaoAtiva = null;
        aoConcluir?.Invoke();
    }

    #endregion

    #region Visão Geral (Estações + Olhar)

    /// <summary>
    /// Lê o mouse e define: em qual estação estamos e quais são os ângulos-alvo de olhar.
    /// Fora da visão geral, os alvos voltam a zero (a suavização cuida do fade).
    /// </summary>
    private void AtualizarAlvosDeOlhar()
    {
        if (estadoAtual != CameraState.Overview)
        {
            yawAlvo = 0f;
            pitchAlvo = 0f;
            return;
        }

        Vector2 mouse = ObterMouseNaTela01();

        // ---------- HORIZONTAL ----------
        yawAlvo = 0f;

        if (mouse.x > 1f - limiteBordaHorizontal && estacaoDireita != null)
        {
            // Borda direita: vai (e fica) na estação direita
            naEstacaoDireita = true;
        }
        else if (mouse.x < limiteBordaHorizontal)
        {
            // Borda esquerda: volta para a principal olhando um pouco pra esquerda
            naEstacaoDireita = false;
            yawAlvo = -anguloOlharEsquerda;
        }
        // Meio da tela: mantém a estação atual

        // ---------- VERTICAL ----------
        pitchAlvo = 0f;

        if (mouse.y > 1f - limiteBordaVertical)
        {
            // Topo: inclina para cima (pitch negativo no Unity)
            float quantidade = Mathf.InverseLerp(1f - limiteBordaVertical, 1f, mouse.y);
            pitchAlvo = -anguloOlharCima * quantidade;
        }
        else if (mouse.y < limiteBordaVertical)
        {
            // Rodapé: inclina para baixo (pitch positivo)
            float quantidade = Mathf.InverseLerp(limiteBordaVertical, 0f, mouse.y);
            pitchAlvo = anguloOlharBaixo * quantidade;
        }
    }

    /// <summary>
    /// Na visão geral, a pose base persegue suavemente a estação escolhida.
    /// </summary>
    private void AtualizarPoseBaseNaVisaoGeral()
    {
        if (estadoAtual != CameraState.Overview) return;

        Pose estacao = ObterPoseDaEstacao(naEstacaoDireita);

        posicaoBase = Vector3.Lerp(posicaoBase, estacao.position, SuavizacaoExp(velocidadePosicao));
        rotacaoBase = Quaternion.Slerp(rotacaoBase, estacao.rotation, SuavizacaoExp(velocidadeRotacao));
        fovBase = Mathf.Lerp(fovBase, fovInicial, SuavizacaoExp(velocidadePosicao));
    }

    /// <summary>
    /// Retorna a pose (posição + rotação) de uma estação, com fallback para a pose salva no início.
    /// </summary>
    private Pose ObterPoseDaEstacao(bool direita)
    {
        Transform t = direita ? estacaoDireita : estacaoPrincipal;

        return t != null
            ? new Pose(t.position, t.rotation)
            : new Pose(posicaoInicial, rotacaoInicial);
    }

    #endregion

    #region Camadas de Offset (Olhar, Pan, Balanço)

    /// <summary>
    /// Suaviza todas as camadas que ficam por cima da pose base.
    /// </summary>
    private void AtualizarCamadas()
    {
        // ---- Olhar (pitch/yaw) ----
        float tRotacao = SuavizacaoExp(velocidadeRotacao);
        pitchAtual = Mathf.Lerp(pitchAtual, pitchAlvo, tRotacao);
        yawAtual = Mathf.Lerp(yawAtual, yawAlvo, tRotacao);

        // ---- Edge Pan ----
        bool panAtivo = usarEdgePan &&
                        (estadoAtual == CameraState.Focused ||
                         (estadoAtual == CameraState.Overview && panNaVisaoGeral));

        Vector3 panAlvo = Vector3.zero;
        if (panAtivo)
        {
            Vector2 mouse = ObterMouseNaTela01();

            // Converte 0..1 para -1..1 (0,0 = centro da tela)
            float x = (mouse.x - 0.5f) * 2f;
            float y = (mouse.y - 0.5f) * 2f;
            panAlvo = new Vector3(x * limitePan.x, y * limitePan.y, 0f);
        }
        offsetPanLocal = Vector3.Lerp(offsetPanLocal, panAlvo, SuavizacaoExp(velocidadeSuavizacaoPan));

        // ---- Peso do balanço (bob/sway) ----
        float pesoAlvo = estadoAtual switch
        {
            CameraState.Overview => 1f,
            CameraState.Focused => intensidadeBalancoFocado,
            _ => 0f // Moving: balanço desligado para não atrapalhar a transição
        };
        pesoBalanco = Mathf.MoveTowards(pesoBalanco, pesoAlvo, Time.deltaTime / duracaoFadeBalanco);
    }

    /// <summary>
    /// Junta pose base + camadas e aplica no transform/câmera. É o ÚNICO lugar que escreve no transform.
    /// </summary>
    private void AplicarPoseFinal()
    {
        // Movimento natural: duas senoides em velocidades diferentes para não parecer repetitivo
        float bob = Mathf.Sin(Time.time * velocidadeBob) * amplitudeBob * pesoBalanco;
        float sway = Mathf.Sin(Time.time * velocidadeBob * FATOR_VELOCIDADE_SWAY) * amplitudeSway * pesoBalanco;

        // Rotação: base -> olhar (pitch/yaw) -> balanço
        Quaternion rotacaoFinal = rotacaoBase
                                  * Quaternion.Euler(pitchAtual, yawAtual, 0f)
                                  * Quaternion.Euler(bob * FATOR_BOB_ROTACAO, sway, sway * 0.3f);

        // Posição: base + bob vertical + pan (convertido do espaço local da base para o mundo)
        Vector3 posicaoFinal = posicaoBase
                               + Vector3.up * bob
                               + rotacaoBase * offsetPanLocal;

        transform.SetPositionAndRotation(posicaoFinal, rotacaoFinal);
        cameraAlvo.fieldOfView = fovBase;
    }

    #endregion

    #region Utilitários

    /// <summary>
    /// Posição do mouse normalizada (0..1). Se o cursor estiver fora da janela,
    /// retorna o centro, evitando que a câmera "dispare" por valores absurdos.
    /// </summary>
    private Vector2 ObterMouseNaTela01()
    {
        Vector3 p = Input.mousePosition;

        bool dentroDaTela = Screen.width > 0 && Screen.height > 0 &&
                            p.x >= 0f && p.x <= Screen.width &&
                            p.y >= 0f && p.y <= Screen.height;

        if (!dentroDaTela) return new Vector2(0.5f, 0.5f);

        return new Vector2(p.x / Screen.width, p.y / Screen.height);
    }

    /// <summary>
    /// Fator de suavização independente de FPS. Substitui o clássico Lerp(a, b, velocidade * deltaTime),
    /// que muda de comportamento conforme o framerate.
    /// </summary>
    private static float SuavizacaoExp(float velocidade)
    {
        return 1f - Mathf.Exp(-velocidade * Time.deltaTime);
    }

    #endregion
}
