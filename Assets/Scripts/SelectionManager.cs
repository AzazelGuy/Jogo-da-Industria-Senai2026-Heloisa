using UnityEngine;

/// <summary>
/// Gerencia as interações de seleção, duplo clique e clique contínuo (hold) via Raycast.
/// </summary>
public class SelectionManager : MonoBehaviour
{
    public static SelectionManager Instance { get; private set; }

    [Header("Configurações do Raio (Raycast)")]
    [SerializeField, Tooltip("Camada contendo os objetos que podem ser selecionados.")]
    private LayerMask camadaSelecionavel;

    [SerializeField, Tooltip("Distância máxima de alcance do raio de seleção.")]
    private float distanciaMaxima = 100f;

    [Header("Tempo de Interação")]
    [SerializeField, Tooltip("Intervalo máximo de tempo entre cliques para registrar um duplo clique.")]
    private float limiteDuploClique = 0.3f;

    [SerializeField, Tooltip("Tempo necessário pressionando o botão para registrar como clique contínuo (hold).")]
    private float limiteCliqueContinuo = 0.5f;

    // Referências e Estados Internos
    private Camera cameraPrincipal;
    private ISelectable objetoSelecionadoAtual;
    private ISelectable objetoPressionadoAtual;

    private float tempoDoClique;
    private float tempoDoUltimoClique;
    private bool estaPressionando;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        AtualizarReferenciaDaCamera();
    }

    private void Update()
    {
        ProcessarEntradasDeUsuario();
    }

    private void ProcessarEntradasDeUsuario()
    {
        // 1. Clique inicial do mouse
        if (Input.GetMouseButtonDown(0))
        {
            tempoDoClique = Time.time;
            estaPressionando = false;
            objetoPressionadoAtual = ObterSelecionavelSobOCursor();
        }

        // 2. Botão do mouse mantido pressionado
        if (Input.GetMouseButton(0))
        {
            if (!estaPressionando && (Time.time - tempoDoClique >= limiteCliqueContinuo))
            {
                estaPressionando = true;
            }

            if (estaPressionando && objetoPressionadoAtual != null)
            {
                objetoPressionadoAtual.OnHold();
            }
        }

        // 3. Botão do mouse solto
        if (Input.GetMouseButtonUp(0))
        {
            if (!estaPressionando && objetoPressionadoAtual != null)
            {
                SelecionarObjeto(objetoPressionadoAtual);

                if (Time.time - tempoDoUltimoClique <= limiteDuploClique)
                {
                    objetoPressionadoAtual.OnDoubleClick();
                }

                tempoDoUltimoClique = Time.time;
            }
            else if (!estaPressionando)
            {
                DeselecionarAtual();
            }

            estaPressionando = false;
            objetoPressionadoAtual = null;
        }
    }

    /// <summary>
    /// Executa um Raycast para encontrar o objeto interativo mais próximo sob o cursor
    /// </summary>
    private ISelectable ObterSelecionavelSobOCursor()
    {
        if (cameraPrincipal == null)
        {
            AtualizarReferenciaDaCamera();
            if (cameraPrincipal == null) return null;
        }

        Ray raio = cameraPrincipal.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] colisoes = Physics.RaycastAll(raio, distanciaMaxima, camadaSelecionavel);

        // Ordena as colisões pela distância do objeto em relação à câmera
        System.Array.Sort(colisoes, (x, y) => x.distance.CompareTo(y.distance));

        foreach (RaycastHit colisao in colisoes)
        {
            ISelectable selecionavel = colisao.collider.GetComponentInParent<ISelectable>();
            if (selecionavel != null)
            {
                return selecionavel;
            }
        }

        return null;
    }

    private void SelecionarObjeto(ISelectable selecionavel)
    {
        if (objetoSelecionadoAtual != null && objetoSelecionadoAtual != selecionavel)
        {
            objetoSelecionadoAtual.OnDeselect();
        }

        objetoSelecionadoAtual = selecionavel;
        objetoSelecionadoAtual.OnSelect();
    }

    private void DeselecionarAtual()
    {
        if (objetoSelecionadoAtual != null)
        {
            objetoSelecionadoAtual.OnDeselect();
            objetoSelecionadoAtual = null;
        }
    }

    private void AtualizarReferenciaDaCamera()
    {
        cameraPrincipal = Camera.main;
        if (cameraPrincipal == null)
        {
            Debug.LogWarning("[SelectionManager] Nenhuma câmera com a tag 'MainCamera' foi encontrada na cena.");
        }
    }
}