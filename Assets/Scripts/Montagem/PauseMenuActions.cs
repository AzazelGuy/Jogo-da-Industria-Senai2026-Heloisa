using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine;

public class PauseMenuActions : MonoBehaviour
{
    public static PauseMenuActions instance;
    [SerializeField] private Animator animator;

    [SerializeField] private GameObject pauseMenuGroup;
    [SerializeField] private GameObject[] ObjectstoHide;
    [SerializeField] private GameObject SettingsMenu;

    private bool isPaused = false;
    private bool playingAnimation;

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
        
        if (pauseMenuGroup) {
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

    public void Resume()
    {
        // FAILSAFE adicional para quando o jogador clica no botão "Voltar"
        if (!PodePausar()) return;
        TogglePause();
    }

    // --- FUNÇÃO DE VALIDAÇÃO (NÃO PAUSE MEU AMIGO) ---
    public bool PodePausar()
    {
        // 1. Se estiver rodando a animação de abrir/fechar o pause
        if (playingAnimation) return false;

        return true;
    }

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

    #region Animation Methods
    public void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;
    }

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

    public void StartAnimation()
    {
        playingAnimation = true;
        if (pauseMenuGroup != null && pauseMenuGroup.TryGetComponent<CanvasGroup>(out var canvasGroup))
        {
            canvasGroup.blocksRaycasts = false;
        }
    }

    public void EndAnimation()
    {
        playingAnimation = false;
        if (pauseMenuGroup != null && pauseMenuGroup.TryGetComponent<CanvasGroup>(out var canvasGroup))
        {
            canvasGroup.blocksRaycasts = true;
        }
        ChangeActive();
    }

    public void ChangeActive()
    {
        if (pauseMenuGroup != null)
        {
            pauseMenuGroup.SetActive(isPaused);
        }
    }
    #endregion

    #region Button Actions
    public void ShowSettings(bool activate)
    {
        SettingsMenu.SetActive(activate);
        Button[] buttons = pauseMenuGroup.GetComponentsInChildren<Button>();
            foreach (Button btn in buttons)
            {
                btn.OnDeselect(null); // Force deselect state
            }
    }

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
    public bool Paused => isPaused;
}