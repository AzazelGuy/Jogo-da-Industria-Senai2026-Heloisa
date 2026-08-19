using System;
using System.Collections;
using UnityEngine;

namespace SeriousGame.Hardware
{
    /// <summary>
    /// Estado atual da câmera de montagem.
    /// </summary>
    public enum CameraState
    {
        Overview,   // Visão geral da bancada
        Moving,     // Em transição suave
        Focused     // Focada em uma peça/parafuso específico
    }

    /// <summary>
    /// Gerenciador principal do movimento e zoom da câmera.
    /// Anexe este script EXCLUSIVAMENTE na sua Main Camera.
    /// Crie um arquivo chamado "FocusCameraController.cs".
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour
    {

        public static CameraController instance;
        [Header("Configurações de Movimento")]
        [SerializeField, Tooltip("Duração da transição em segundos.")]
        private float transitionDuration = 0.8f;

        [SerializeField, Tooltip("Curva de suavização da animação (Ease In Out recomendado).")]
        private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        // Referências internas de estado
        private Camera targetCamera;
        private CameraState currentState = CameraState.Overview;

        // Dados da Posição Inicial (Bancada)
        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private float initialFOV;

        // Controle de Corrotina ativa
        private Coroutine activeTransitionRoutine;

        // Eventos C# para notificar outros sistemas
        public event Action OnFocusReached;
        public event Action OnReturnToOverview;

        public CameraState CurrentState => currentState;

        private void Awake()
        {
            if (instance == null) { instance = this; } else { Destroy(gameObject); }
                targetCamera = GetComponent<Camera>();
            SaveInitialTransform();
        }

        /// <summary>
        /// Grava a posição, rotação e FOV atuais como o estado inicial (Overview).
        /// </summary>
        public void SaveInitialTransform()
        {
            initialPosition = transform.position;
            initialRotation = transform.rotation;
            initialFOV = targetCamera.fieldOfView;
            currentState = CameraState.Overview;
        }

        /// <summary>
        /// Move a câmera suavemente para o ponto de foco de uma peça/parafuso.
        /// </summary>
        public void FocusOnPiece(FocusPoint focusPoint)
        {
            if (focusPoint == null) return;

            Vector3 targetPos = focusPoint.GetWorldTargetPosition();
            Quaternion targetRot = focusPoint.GetWorldTargetRotation();
            float targetFOV = focusPoint.focusedFOV;

            StartTransition(targetPos, targetRot, targetFOV, () =>
            {
                currentState = CameraState.Focused;
                OnFocusReached?.Invoke();
            });
        }

        /// <summary>
        /// Move a câmera de volta para a visão geral da bancada.
        /// </summary>
        public void ReturnToOverview()
        {
            if (currentState == CameraState.Overview) return;

            StartTransition(initialPosition, initialRotation, initialFOV, () =>
            {
                currentState = CameraState.Overview;
                OnReturnToOverview?.Invoke();
            });
        }

        private void StartTransition(Vector3 targetPos, Quaternion targetRot, float targetFOV, Action onComplete)
        {
            if (activeTransitionRoutine != null)
            {
                StopCoroutine(activeTransitionRoutine);
            }

            currentState = CameraState.Moving;
            activeTransitionRoutine = StartCoroutine(TransitionRoutine(targetPos, targetRot, targetFOV, onComplete));
        }

        /// <summary>
        /// Corrotina de interpolação suave (Lerp/Slerp) de Posição, Rotação e FOV.
        /// </summary>
        private IEnumerator TransitionRoutine(Vector3 targetPos, Quaternion targetRot, float targetFOV, Action onComplete)
        {
            Vector3 startPos = transform.position;
            Quaternion startRot = transform.rotation;
            float startFOV = targetCamera.fieldOfView;

            float elapsedTime = 0f;

            while (elapsedTime < transitionDuration)
            {
                elapsedTime += Time.deltaTime;
                float normalizedTime = Mathf.Clamp01(elapsedTime / transitionDuration);
                float curveValue = transitionCurve.Evaluate(normalizedTime);

                transform.position = Vector3.Lerp(startPos, targetPos, curveValue);
                transform.rotation = Quaternion.Slerp(startRot, targetRot, curveValue);
                targetCamera.fieldOfView = Mathf.Lerp(startFOV, targetFOV, curveValue);

                yield return null;
            }

            transform.position = targetPos;
            transform.rotation = targetRot;
            targetCamera.fieldOfView = targetFOV;

            activeTransitionRoutine = null;
            onComplete?.Invoke();
        }

        private void Update()
        {
            // Atalho: Tecla ESC ou Botão Direito do Mouse desfaz o zoom
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
            {
                if (currentState == CameraState.Focused)
                {
                    ReturnToOverview();
                }
            }
        }
    }
}