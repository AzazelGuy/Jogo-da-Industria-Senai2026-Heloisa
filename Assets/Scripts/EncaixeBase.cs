using UnityEngine;

    /// <summary>
    /// Gerencia a criação dinâmica de conectores/parafusos no slot de encaixe.
    /// </summary>
    public class EncaixeBase : MonoBehaviour
    {
        [SerializeField, Tooltip("Identificador do slot de encaixe associado.")]
        private IdentifiyerEncaixe identificador;

        [SerializeField, Tooltip("Prefab do parafuso/conector a ser instanciado.")]
        private GameObject prefabDoConector;

        private void Start()
        {
            if (identificador == null)
            {
                identificador = GetComponent<IdentifiyerEncaixe>();
            }
        }

        /// <summary>
        /// Instancia os parafusos nas posições configuradas para iniciar a etapa de parafusamento.
        /// </summary>
        public void MiniGame()
        {
            if (identificador == null || prefabDoConector == null) return;

            for (int i = 0; i < identificador.posicoesDosParafusos.Count; i++)
            {
                Transform posicao = identificador.posicoesDosParafusos[i];
                GameObject objetoParafuso = Instantiate(prefabDoConector);

                // Instancia o parafuso um pouco acima da posição final para deslizar
                objetoParafuso.transform.position = posicao.position + new Vector3(0f, 1.5f, 0f);

                Conector conector = objetoParafuso.GetComponent<Conector>();
                if (conector != null)
                {
                    conector.SetTarget(posicao.position);
                    conector.SetPlacement(this, i);
                }
            }
        }

        /// <summary>
        /// Método chamado pelo conector/parafuso assim que a conexão é concluída.
        /// </summary>
        /// <param name="indice">Índice do parafuso encaixado.</param>
        public void OnConectorPlaced(int indice)
        {
            if (identificador != null)
            {
                identificador.MarkScrewCompleted(indice);
            }
        }
    }