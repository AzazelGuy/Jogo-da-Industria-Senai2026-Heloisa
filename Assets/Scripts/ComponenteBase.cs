using UnityEngine;

/// <summary>
/// Classe base para componentes de hardware arrastáveis e encaixáveis na cena.
/// </summary>
public class ComponenteBase : MonoBehaviour, ISelectable
{
    public enum EstadoComponente
    {
        Normal,
        Selecionado
    }

    [Header("Configurações Principais")]
    [SerializeField, Tooltip("Animador opcional para disparar animações de seleção.")]
    private Animator animador;

    [SerializeField, Tooltip("Dados ScriptableObject com as informações da peça.")]
    private SOPieceData dadosDaPeca;

    [SerializeField, Tooltip("Estado atual de interação do componente.")]
    private EstadoComponente estadoAtual = EstadoComponente.Normal;

    [SerializeField, Tooltip("MeshFilter do modelo da peça.")]
    private MeshFilter modeloDaPeca;

    [Header("Configurações de Snapping (Encaixe)")]
    [SerializeField, Tooltip("Distância máxima para atrair a peça ao slot de destino.")]
    private float distanciaDeEncaixe = 1.0f;

    [Header("Configurações de Arraste 3D")]
    [SerializeField, Tooltip("Máscara de camadas para detectar o chão/bancada.")]
    private LayerMask mascaraDeSuperficie = ~0;

    [Header("Configurações de Destaque (Outline)")]
    [SerializeField, Tooltip("Material Custom/OutlineShader aplicado ao selecionar.")]
    private Material materialDeDestaque;

    // Controle Interno
    private Vector3 posicaoOriginal;
    private Camera cameraPrincipal;
    private IdentifiyerEncaixe slotDeDestino;

    private bool estaEncaixado = false;
    private bool estaInstalado = false;
    private bool recemSelecionado = false;

    // Arraste 3D
    private float profundidadeDeArraste;
    private Plane planoDeArraste;

    // Renderizadores e Materiais
    private Renderer renderizadorMesh;
    private Material[] materiaisOriginais;
    private Material[] materiaisComDestaque;

    public string myID => dadosDaPeca != null ? dadosDaPeca.ID : "";

    private void Awake()
    {
        if (animador == null)
        {
            animador = GetComponent<Animator>();
        }

        posicaoOriginal = transform.position;
        cameraPrincipal = Camera.main;

        ConfigurarMateriaisDeDestaque();
    }

    private void Start()
    {
        IdentifiyerEncaixe[] slotsExistentes = FindObjectsByType<IdentifiyerEncaixe>(FindObjectsSortMode.None);
        foreach (IdentifiyerEncaixe slot in slotsExistentes)
        {
            if (slot.GetID == dadosDaPeca.ID)
            {
                slotDeDestino = slot;
                break;
            }
        }
    }

    private void Update()
    {
        if (estaInstalado) return;

        if (estadoAtual == EstadoComponente.Selecionado)
        {
            if (recemSelecionado)
            {
                recemSelecionado = false;
                return;
            }

            ProcessarArrasteEEncaixe();

            if (Input.GetMouseButtonDown(0))
            {
                SoltarObjeto();
            }
        }
    }

    public void OnSelect()
    {
        SelecionarPeca();
    }

    protected virtual void SelecionarPeca()
    {
        if (estaInstalado || estadoAtual == EstadoComponente.Selecionado) return;

        estadoAtual = EstadoComponente.Selecionado;
        recemSelecionado = true;

        if (cameraPrincipal == null) cameraPrincipal = Camera.main;

        profundidadeDeArraste = cameraPrincipal.WorldToScreenPoint(transform.position).z;

        if (animador != null) animador.SetTrigger("OnSelect");

        AlternarDestaqueVisivel(true);

        GameManager.Instance.SelectObject(gameObject);

        if (slotDeDestino != null && modeloDaPeca != null)
        {
            slotDeDestino.UpdateModel(modeloDaPeca.mesh);
        }
    }

    private void ProcessarArrasteEEncaixe()
    {
        if (cameraPrincipal == null) cameraPrincipal = Camera.main;

        Ray raio = cameraPrincipal.ScreenPointToRay(Input.mousePosition);
        planoDeArraste = new Plane(-cameraPrincipal.transform.forward, transform.position);

        Vector3 posicaoMundoAlvo;

        if (planoDeArraste.Raycast(raio, out float distancia))
        {
            posicaoMundoAlvo = raio.GetPoint(distancia);
        }
        else
        {
            Vector3 posicaoMouseTela = Input.mousePosition;
            posicaoMouseTela.z = profundidadeDeArraste;
            posicaoMundoAlvo = cameraPrincipal.ScreenToWorldPoint(posicaoMouseTela);
        }

        posicaoMundoAlvo.y = transform.position.y;

        // Atração (Snapping) ao slot de destino
        if (slotDeDestino != null)
        {
            Vector3 posicaoDoSlot = slotDeDestino.transform.position;
            float distanciaAteSlot = Vector3.Distance(posicaoMundoAlvo, posicaoDoSlot);

            if (distanciaAteSlot <= distanciaDeEncaixe)
            {
                transform.position = posicaoDoSlot;
                estaEncaixado = true;
                return;
            }
        }

        transform.position = posicaoMundoAlvo;
        estaEncaixado = false;
    }

    private void SoltarObjeto()
    {
        if (estaEncaixado && slotDeDestino != null)
        {
            transform.position = slotDeDestino.transform.position;
            estadoAtual = EstadoComponente.Normal;
            estaInstalado = true;

            if (animador != null) animador.SetTrigger("OnDeselect");

            AlternarDestaqueVisivel(false);

            GameManager.Instance.ClearObject();
            slotDeDestino.UpdateModel(null);

            if (TryGetComponent<Collider>(out var colisor))
            {
                colisor.enabled = false;
            }

            if (slotDeDestino.HasExtraStep) DispararZoomDaCamera(); else slotDeDestino.FinishitAll();
        }
        else
        {
            OnDeselect();
        }
    }

    private void DispararZoomDaCamera()
    {
        FocusPoint pontoDeFoco = GetComponentInChildren<FocusPoint>();

        if (pontoDeFoco == null && slotDeDestino != null)
        {
            pontoDeFoco = slotDeDestino.GetComponentInChildren<FocusPoint>();

            if (slotDeDestino.GetComponentInParent<IdentifiyerEncaixe>().HasExtraStep)
            {
                slotDeDestino.GetComponentInParent<EncaixeBase>().MiniGame();
            }
        }

        if (CameraController.instance != null)
        {
            if (pontoDeFoco != null)
            {
                CameraController.instance.FocusOnPiece(pontoDeFoco);
            }
            else
            {
                Debug.LogWarning($"<color=blue>[ComponenteBase]</color> Peça '{gameObject.name}' foi encaixada, mas nenhum 'FocusPoint' foi encontrado!");
            }
        }
    }

    protected virtual void Deselecionar()
    {
        if (estaInstalado) return;

        estadoAtual = EstadoComponente.Normal;
        transform.position = posicaoOriginal;
        estaEncaixado = false;

        AlternarDestaqueVisivel(false);

        if (animador != null) animador.SetTrigger("OnDeselect");

        if (slotDeDestino != null)
        {
            slotDeDestino.UpdateModel(null);
        }

        GameManager.Instance.ClearObject();
    }

    public void OnDeselect()
    {
        Deselecionar();
    }

    #region Gestão de Materiais de Destaque (Outline)

    private void ConfigurarMateriaisDeDestaque()
    {
        renderizadorMesh = modeloDaPeca != null ? modeloDaPeca.GetComponent<Renderer>() : GetComponent<Renderer>();

        if (renderizadorMesh == null || materialDeDestaque == null)
        {
            if (materialDeDestaque == null)
            {
                Debug.LogWarning($"[ComponenteBase] O material de destaque não foi atribuído no Inspector em: {gameObject.name}");
            }
            return;
        }

        materiaisOriginais = renderizadorMesh.sharedMaterials;
        materiaisComDestaque = new Material[materiaisOriginais.Length + 1];

        for (int i = 0; i < materiaisOriginais.Length; i++)
        {
            materiaisComDestaque[i] = materiaisOriginais[i];
        }
        materiaisComDestaque[materiaisComDestaque.Length - 1] = materialDeDestaque;
    }

    private void AlternarDestaqueVisivel(bool visivel)
    {
        if (renderizadorMesh == null || materialDeDestaque == null) return;

        renderizadorMesh.materials = visivel ? materiaisComDestaque : materiaisOriginais;
    }

    #endregion

    public void OnDoubleClick() { }
    public void OnHold() { }
}