using System.Collections;
using System;
using TMPro;
using UnityEngine;

[System.Serializable]
public class DialoguePage
{
    [TextArea] public string text;   // Texto da fala
    public Sprite portrait;          // Retrato opcional
    public bool isStop;              // Se true, o diálogo termina nesta página
    public bool ChangeSong = false;  // Se true, troca a música ao exibir a página
    public AudioClip music;
}

public class DialogueControler : MonoBehaviour
{
    public static DialogueControler instance;

    public static event Action OnDialogueEnd;

    [Header("UI")]
    public GameObject dialogueGroup; // PAI do HUD de diálogo (arrasta aqui no Inspector)
    public TMP_Text dialogueText;

    [Header("Typing")]
    public float typingSpeed = 0.04f;
    public AudioClip blip;

    private DialoguePage[] pages;
    private int currentPage = 0;

    private bool isTyping = false;
    private string fullText;
    private Coroutine typingCoroutine;

    private KeyCode confirmKey = KeyCode.E;

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
        }
    }

    private void Start()
    {
        dialogueGroup.SetActive(false);
    }

    // =========================
    // Iniciar diálogo
    // =========================
    public void StartDialogue(DialoguePage[] dialoguePages)
    {
        pages = dialoguePages;
        currentPage = 0;
        dialogueGroup.SetActive(true);
        ShowPage(currentPage);
        Time.timeScale = 0f;
    }

    // =========================
    // Mostrar página
    // =========================
    void ShowPage(int pageIndex)
    {
        DialoguePage page = pages[pageIndex];
        fullText = page.text;

        if (page.ChangeSong)
        {
            AudioManager.Instance.PlayMusic(page.music);
        }

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeText());
    }

    // =========================
    // Digitar letra por letra
    // =========================
    IEnumerator TypeText()
    {
        isTyping = true;
        dialogueText.text = "";

        foreach (char c in fullText)
        {
            dialogueText.text += c;

            if (blip != null && c != ' ')
                AudioManager.Instance.PlaySFXRandomPitch(blip);

            yield return new WaitForSecondsRealtime(typingSpeed);
        }

        isTyping = false;
    }

    // =========================
    // Input
    // =========================
    void Update()
    {
        if (!dialogueGroup.activeSelf) return;
        if (!Input.GetKeyDown(confirmKey)) return;

        if (isTyping)
        {
            SkipTyping();
        }
        else if (pages[currentPage].isStop)
        {
            EndDialogue();
        }
        else
        {
            NextPage();
        }
    }

    // =========================
    // Pular digitação
    // =========================
    void SkipTyping()
    {
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        dialogueText.text = fullText;
        isTyping = false;
    }

    // =========================
    // Próxima página
    // =========================
    void NextPage()
    {
        currentPage++;
        if (currentPage >= pages.Length)
        {
            EndDialogue();
        }
        else
        {
            ShowPage(currentPage);
        }
    }

    // =========================
    // Fecha diálogo
    // =========================
    void EndDialogue()
{
    if (typingCoroutine != null) StopCoroutine(typingCoroutine);
    isTyping = false;
    dialogueText.text = "";
    Time.timeScale = 1f;
    dialogueGroup.SetActive(false);

    OnDialogueEnd?.Invoke(); // avisa quem estiver ouvindo
}

private void OnDestroy() => instance = null;
}