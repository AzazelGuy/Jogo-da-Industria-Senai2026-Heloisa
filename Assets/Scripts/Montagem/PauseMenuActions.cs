using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine;

/// <summary>
/// Controla o menu de pausa: abrir/fechar (com ou sem animação), tela de configurações
/// e failsafes para evitar pausar/despausar durante uma transição de animação.
/// </summary>
public class PauseMenuActions : MonoBehaviour
{
    #region Singleton

    public static PauseMenuActions instance;

    #endregion

    #region Campos Serializados

    [SerializeField] private Animator animator;

    [SerializeField] private GameObject pauseMenuGroup;
    [SerializeField] private GameObject[] ObjectstoHide;
    [SerializeField] private GameObject SettingsMenu;

    #endregion

    #region Estado Interno

    private bool isPaused = false;
    private bool playingAnimation;

    #endregion

    #region Propriedades Públicas

    public bool Paused => isPaused;

    #endregion

    #region Ciclo de Vida (Unity)

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // Dica: passe o gameObject aqui para persistir a hierarquia
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (pauseMenuGroup)
        {
            foreach (GameObject b in ObjectstoHide)
            {
                b.SetActive(false);
            }
            pauseMenuGroup.SetActive(false);
        }
    }

    void Update()
    {
        // FAILSAFE: Se estiver rodando a animação de pause OU se o jogo estiver mudando de cena, NÃO DEIXA PAUSAR!
        if (Input.GetKeyDown(KeyCode.Escape) && PodePausar())
        {
            TogglePause();
        }
    }

    #endregion

    #region API Pública

    /// <summary>
    /// Chamado pelo botão "Voltar" da UI para retomar o jogo.
    /// </summary>
    public void Resume()
    {
        // FAILSAFE adicional para quando o jogador clica no botão "Voltar"
        if (!PodePausar()) return;
        TogglePause();
    }

    #endregion

    #region Validação de Estado

    // --- FUNÇÃO DE VALIDAÇÃO (NÃO PAUSE MEU AMIGO) ---
    /// <summary>
    /// Retorna true se é seguro pausar/despausar no momento
    /// (ou seja, se não há animação de transição do menu em andamento).
    /// </summary>
    public bool PodePausar()
    {
        // 1. Se estiver rodando a animação de abrir/fechar o pause
        if (playingAnimation) return false;

        return true;
    }

    /// <summary>
    /// Alterna o estado de pausa disparando o Animator (se houver) ou, como fallback,
    /// alternando o estado e o Time.timeScale diretamente.
    /// </summary>
    private void TogglePause()
    {
        string pauseTrigger = isPaused ? "Unpause" : "Pause";

        if (animator != null)
        {
            animator.SetTrigger(pauseTrigger);
        }
        else
        {
            // Fallback caso não tenha Animator no menu de pause
            isPaused = !isPaused;
            ChangeActive();
            Time.timeScale = isPaused ? 0f : 1f;
        }
    }

    #endregion

    #region Animation Methods

    /// <summary>
    /// Chamado (via Animator/eventos ou diretamente) para efetivar o estado pausado.
    /// </summary>
    public void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;
    }

    /// <summary>
    /// Chamado (via Animator/eventos ou diretamente) para efetivar o estado despausado,
    /// limpando seleção de UI e resetando animações de botão.
    /// </summary>
    public void UnPause()
    {
        foreach (GameObject b in ObjectstoHide)
        {
            b.SetActive(false);
        }

        isPaused = false;

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        // Reset button animations
        if (pauseMenuGroup != null)
        {
            Button[] buttons = pauseMenuGroup.GetComponentsInChildren<Button>();
            foreach (Button btn in buttons)
            {
                btn.OnDeselect(null); // Force deselect state
            }
        }

        Time.timeScale = 1f;
    }

    /// <summary>
    /// Marca que a animação de transição do menu começou, bloqueando novos pedidos de pausa
    /// e desativando o raycast do CanvasGroup (evita cliques durante a transição).
    /// </summary>
    public void StartAnimation()
    {
        playingAnimation = true;
        if (pauseMenuGroup != null && pauseMenuGroup.TryGetComponent<CanvasGroup>(out var canvasGroup))
        {
            canvasGroup.blocksRaycasts = false;
        }
    }

    /// <summary>
    /// Marca o fim da animação de transição do menu, reabilitando raycasts
    /// e sincronizando a visibilidade do grupo com o estado atual de pausa.
    /// </summary>
    public void EndAnimation()
    {
        playingAnimation = false;
        if (pauseMenuGroup != null && pauseMenuGroup.TryGetComponent<CanvasGroup>(out var canvasGroup))
        {
            canvasGroup.blocksRaycasts = true;
        }
        ChangeActive();
    }

    /// <summary>
    /// Sincroniza a ativação do GameObject do menu de pausa com o estado interno "isPaused".
    /// </summary>
    public void ChangeActive()
    {
        if (pauseMenuGroup != null)
        {
            pauseMenuGroup.SetActive(isPaused);
        }
    }

    #endregion

    #region Button Actions

    /// <summary>
    /// Abre/fecha a tela de configurações e força a desseleção dos botões do menu de pausa.
    /// </summary>
    public void ShowSettings(bool activate)
    {
        SettingsMenu.SetActive(activate);
        Button[] buttons = pauseMenuGroup.GetComponentsInChildren<Button>();
        foreach (Button btn in buttons)
        {
            btn.OnDeselect(null); // Force deselect state
        }
    }

    /// <summary>
    /// Prepara o retorno ao menu principal, desselecionando botões e limpando a instância estática.
    /// </summary>
    public void ReturntoMainMenu()
    {
        Button[] buttons = pauseMenuGroup.GetComponentsInChildren<Button>();
        foreach (Button btn in buttons)
        {
            btn.OnDeselect(null); // Force deselect state
        }
        instance = null;
    }

    #endregion
}
