using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

    /// <summary>
    /// Gerencia a identificação do slot de encaixe e o controle de parafusamento das peças.
    /// </summary>
    public class IdentifiyerEncaixe : MonoBehaviour
    {
        [Header("Identificação do Encaixe")]
        [SerializeField, Tooltip("ID único da peça/slot.")]
        private SOPieceData Peca;
        [SerializeField, Tooltip("MeshFilter que recebe o modelo visual da peça encaixada.")]
        private MeshFilter modeloUtilizado;

        [Header("Configurações de Parafusos")]
        [Tooltip("Posições no espaço onde os parafusos devem ser gerados.")]
        [SerializeField] public List<Transform> posicoesDosParafusos = new List<Transform>();

        [Tooltip("Estado de conclusão de cada parafuso.")]
        [SerializeField] public List<bool> parafusosConcluidos = new List<bool>();

        [Header("Minigame / Interação")]
        [Tooltip("Ativa a etapa visual de parafusamento ao focar na peça.")]
        [SerializeField] private bool possuiEtapaExtra = true;
    [SerializeField] private bool updatesState = false;
    [SerializeField] private StatesOfCompletion StateToUpdateTo;


    public string GetID => Peca.ID;
        public bool HasExtraStep => possuiEtapaExtra;

        private void Start()
        {
        if (HasExtraStep)
        {
            // Inicializa a lista de controle dos parafusos como não concluídos
            parafusosConcluidos.Clear();
            foreach (Transform posicao in posicoesDosParafusos)
            {
                parafusosConcluidos.Add(false);
            }
        }
        }

        /// <summary>
        /// Marca um parafuso específico como concluído e verifica o estado do encaixe.
        /// </summary>
        /// <param name="indice">Índice do parafuso na lista.</param>
        public void MarkScrewCompleted(int indice)
        {
            if (indice >= 0 && indice < parafusosConcluidos.Count)
            {
                parafusosConcluidos[indice] = true;
                VerificarParafusamentoCompleto();
            }
        }

        private void VerificarParafusamentoCompleto()
        {
            if (!parafusosConcluidos.Contains(false))
            {
            AoConcluirTodosEncaixes();
            }
        }

        private void AoConcluirTodosEncaixes()
        {
            Debug.Log($"[IdentifiyerEncaixe] Todos os parafusos foram inseridos em: {Peca.Nome}");

            if (CameraController.instance != null)
            {
                CameraController.instance.ReturnToOverview();
            }

            if (updatesState)StepChecker.Instance.AtualizarEstado(StateToUpdateTo);
        }

    public void FinishitAll()
    {
        if (updatesState) StepChecker.Instance.AtualizarEstado(StateToUpdateTo);
    }

        /// <summary>
        /// Atualiza o modelo 3D (Mesh) exibido no slot.
        /// </summary>
        /// <param name="novoModelo">A nova malha 3D a ser aplicada.</param>
        public void UpdateModel(Mesh novoModelo)
        {
            if (modeloUtilizado != null)
            {
                modeloUtilizado.mesh = novoModelo;
            }
        }
    }