using Unity.VisualScripting;
using UnityEngine;

public class ControleMenu : MonoBehaviour
{
    [Header("Pain�is de UI")]
    public RectTransform menuInicial;
    public RectTransform menuModos;

    [Header("Configura��es da Transi��o")]
    public float velocidadeTransicao = 8f;

    [Header("Posi��o Escondida do Menu de Modos")]
    [Tooltip("Altura em Y onde o Menu de Modos fica escondido antes de aparecer")]
    public float alturaEscondidoY = 1080f;

    [SerializeField] AudioClip MenuOST;
    private Vector2 posAlvoInicial;
    private Vector2 posAlvoModos;
    private bool emTransicao = false;
    private bool estaNoMenuModos = false; // Controla qual menu est� ativo

    void Start()
    {
        // Garante que o Menu Inicial come�a no centro (Y = 0)
        if (menuInicial != null)
        {
            menuInicial.anchoredPosition = Vector2.zero;
            posAlvoInicial = menuInicial.anchoredPosition;
        }

        // Garante que o Menu de Modos come�a no topo escondido (Y = 1080)
        if (menuModos != null)
        {
            menuModos.anchoredPosition = new Vector2(0, alturaEscondidoY);
            posAlvoModos = menuModos.anchoredPosition;
        }

        if (AudioManager.Instance != null) AudioManager.Instance.PlayMusic(MenuOST);
    }

    void Update()
    {
        if (!emTransicao) return;

        // Move suavemente os dois menus para as posi��es alvo
        menuInicial.anchoredPosition = Vector2.Lerp(menuInicial.anchoredPosition, posAlvoInicial, Time.deltaTime * velocidadeTransicao);
        menuModos.anchoredPosition = Vector2.Lerp(menuModos.anchoredPosition, posAlvoModos, Time.deltaTime * velocidadeTransicao);

        // Finaliza a transi��o quando estiver bem pr�ximo
        if (Vector2.Distance(menuModos.anchoredPosition, posAlvoModos) < 0.1f)
        {
            menuInicial.anchoredPosition = posAlvoInicial;
            menuModos.anchoredPosition = posAlvoModos;
            emTransicao = false;
        }
    }

    // Fun��o chamada no clique do bot�o JOGAR
    public void AbrirMenuModos()
    {
        posAlvoInicial = new Vector2(0, -alturaEscondidoY); // Faz o Menu Inicial descer para -1080
        posAlvoModos = Vector2.zero;                       // Faz o Menu de Modos vir para o centro (0)
        emTransicao = true;
        estaNoMenuModos = true;
    }

    // Fun��o para bot�o Voltar
    public void VoltarParaMenuInicial()
    {
        posAlvoInicial = Vector2.zero;                      // Menu Inicial volta para o centro (0)
        posAlvoModos = new Vector2(0, alturaEscondidoY);    // Menu de Modos volta para o topo (1080)
        emTransicao = true;
        estaNoMenuModos = false;
    }

    // M�todo de leitura utilizado pelo script AtalhosTeclado
    public bool EstaNoMenuModos()
    {
        return estaNoMenuModos;
    }

    // Fun��o para o bot�o SAIR
    public void SairDoJogo()
    {
        Debug.Log("Saindo do Jogo...");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}