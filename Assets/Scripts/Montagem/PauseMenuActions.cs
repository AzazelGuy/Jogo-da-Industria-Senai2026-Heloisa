using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PauseMenuActions : MonoBehaviour
{
    public static PauseMenuActions instance;

    [SerializeField] private GameObject pauseMenuGroup;
    [SerializeField] private GameObject[] ObjectstoHide;
    [SerializeField] private GameObject SettingsMenu;
    [SerializeField] private float fadeDuration = 0.15f;

    private CanvasGroup _group;
    private Coroutine _fadeRoutine;
    private bool isPaused = false;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (pauseMenuGroup)
        {
            if (!pauseMenuGroup.TryGetComponent(out _group))
                _group = pauseMenuGroup.AddComponent<CanvasGroup>();

            foreach (GameObject b in ObjectstoHide)
                b.SetActive(false);

            _group.alpha = 0f;
            pauseMenuGroup.SetActive(false);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            TogglePause();
    }

    public void Resume() => SetPaused(false);

    private void TogglePause() => SetPaused(!isPaused);

    private void SetPaused(bool paused)
    {
        if (pauseMenuGroup == null || isPaused == paused) return;

        isPaused = paused;
        Time.timeScale = paused ? 0f : 1f;

        if (!paused)
        {
            foreach (GameObject b in ObjectstoHide)
                b.SetActive(false);
            ResetButtons();
        }

        if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
        _fadeRoutine = StartCoroutine(Fade(paused));
    }

    private IEnumerator Fade(bool show)
    {
        float target = show ? 1f : 0f;
        float start = _group.alpha;

        // durante o fade ninguém clica em nada
        _group.interactable = false;
        _group.blocksRaycasts = false;

        if (show) pauseMenuGroup.SetActive(true);

        // se interromper no meio, o tempo é proporcional ao que falta
        float duration = Mathf.Max(0.01f, fadeDuration * Mathf.Abs(target - start));
        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            _group.alpha = Mathf.Lerp(start, target, t / duration);
            yield return null;
        }

        _group.alpha = target;

        if (show)
        {
            _group.interactable = true;
            _group.blocksRaycasts = true;
        }
        else
        {
            pauseMenuGroup.SetActive(false);
        }

        _fadeRoutine = null;
    }

    private void ResetButtons()
    {
        if (pauseMenuGroup == null) return;
        foreach (Button btn in pauseMenuGroup.GetComponentsInChildren<Button>())
            btn.OnDeselect(null);
    }

    #region Button Actions
    public void ShowSettings(bool activate)
    {
        SettingsMenu.SetActive(activate);
        ResetButtons();
    }

    public void ReturntoMainMenu()
    {
        Time.timeScale = 1f; // senão o menu principal carrega congelado
        instance = null;
        Destroy(gameObject);
        LoadingScreen.Carregar("MenuInicial");
    }
    #endregion

    public bool Paused => isPaused;
}