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
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour
    {
        public static CameraController instance;

        [Header("Configurações de Movimento")]
        [SerializeField, Tooltip("Duração da transição da câmera em segundos.")]
        private float duracaoDaTransicao = 0.8f;

        [SerializeField, Tooltip("Curva de suavização da animação da câmera.")]
        private AnimationCurve curvaDeTransicao = AnimationCurve.EaseInOut(0, 0, 1, 1);

        // Referências e Estados Internos
        private Camera cameraAlvo;
        private CameraState estadoAtual = CameraState.Overview;

        // Transformações Iniciais (Bancada)
        private Vector3 posicaoInicial;
        private Quaternion rotacaoInicial;
        private float fovInicial;

        // Controle de Corrotina
        private Coroutine rotinaDeTransicaoAtiva;

        // Eventos C#
        public event Action OnFocusReached;
        public event Action OnReturnToOverview;

        public CameraState CurrentState => estadoAtual;

        private void Awake()
        {
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

        /// <summary>
        /// Salva a posição, rotação e campo de visão (FOV) atuais como o ponto inicial.
        /// </summary>
        public void SaveInitialTransform()
        {
            posicaoInicial = transform.position;
            rotacaoInicial = transform.rotation;
            fovInicial = cameraAlvo.fieldOfView;
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

        private void IniciarTransicao(Vector3 posicaoAlvo, Quaternion rotacaoAlvo, float fovAlvo, Action aoConcluir)
        {
            if (rotinaDeTransicaoAtiva != null)
            {
                StopCoroutine(rotinaDeTransicaoAtiva);
            }

            estadoAtual = CameraState.Moving;
            rotinaDeTransicaoAtiva = StartCoroutine(RotinaDeTransicao(posicaoAlvo, rotacaoAlvo, fovAlvo, aoConcluir));
        }

        private IEnumerator RotinaDeTransicao(Vector3 posicaoAlvo, Quaternion rotacaoAlvo, float fovAlvo, Action aoConcluir)
        {
            Vector3 posicaoInicio = transform.position;
            Quaternion rotacaoInicio = transform.rotation;
            float fovInicio = cameraAlvo.fieldOfView;

            float tempoDecorrido = 0f;

            while (tempoDecorrido < duracaoDaTransicao)
            {
                tempoDecorrido += Time.deltaTime;
                float tempoNormalizado = Mathf.Clamp01(tempoDecorrido / duracaoDaTransicao);
                float valorDaCurva = curvaDeTransicao.Evaluate(tempoNormalizado);

                transform.position = Vector3.Lerp(posicaoInicio, posicaoAlvo, valorDaCurva);
                transform.rotation = Quaternion.Slerp(rotacaoInicio, rotacaoAlvo, valorDaCurva);
                cameraAlvo.fieldOfView = Mathf.Lerp(fovInicio, fovAlvo, valorDaCurva);

                yield return null;
            }

            transform.position = posicaoAlvo;
            transform.rotation = rotacaoAlvo;
            cameraAlvo.fieldOfView = fovAlvo;

            rotinaDeTransicaoAtiva = null;
            aoConcluir?.Invoke();
        }

        private void Update()
        {
            // Atalho para cancelar o zoom: Tecla ESC ou Botão Direito do Mouse
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
            {
                if (estadoAtual == CameraState.Focused)
                {
                    ReturnToOverview();
                }
            }
        }
    }