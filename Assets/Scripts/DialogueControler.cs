using System.Collections;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class DialogueOption
{
    public string optionText; // Texto da escolha
    public int nextPage;      // Índice da próxima página (-1 = fecha)
    public bool IsGameRestart;
    public bool IsGameClose;
}

[System.Serializable]
public class DialoguePage
{
    [TextArea] public string text;      // Texto da fala
    public Sprite portrait;             // Retrato opcional
    public DialogueOption[] options;    // Null ou vazio = diálogo linear
    public bool isStop;                 //Flag de parar o dialogo
    public bool ChangeSong = false;                 //Flag de Trocar a Musica
    public AudioClip music;
}


public class DialogueControler : MonoBehaviour
{
    public static DialogueControler instance;
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(instance);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [Header("UI")]
    public GameObject dialoguePanel;
    public GameObject dialogueGroup; // <- Arrasta o PAI HUD aqui no Inspector
    public TMP_Text dialogueText;
    public Image portraitImage;

    [Header("Typing")] //Efeito de digitação + audio
    public float typingSpeed = 0.04f;
    public AudioSource blip;

    [Header("Choices")] //Botões de escolha
    public GameObject choicesPanel;
    public RectTransform[] choicePositions; // posição das caixas de escolha
    public GameObject cursor;              // imagem do cursor que se move

    private DialoguePage[] pages; //Paginas
    private int currentPage = 0; //Atual, baseado em array

    private bool isTyping = false; //Se ele está adicionando ou não texto
    private string fullText; //Referencia ao texto completo da pagina
    private Coroutine typingCoroutine; //Corotina que controla o efeito de digtação

    private DialogueOption[] currentOptions; //Referencia as opções
    private int selectedIndex = 0; //qual esta selecionada
    private bool inChoices = false; //O jogador está em perguna

    private KeyCode confirmKey = KeyCode.E; // tecla para confirmar

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
        dialogueGroup.SetActive(true); // <- era dialoguePanel
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

        // Retrato
        if (page.portrait != null)
        {
            portraitImage.gameObject.SetActive(true);
            portraitImage.sprite = page.portrait;
        }
        else
        {
            portraitImage.gameObject.SetActive(false);
        }
        if (page.ChangeSong == true)
        {
            AudioManager.Instance.PlayMusic(page.music);
        }
        // Resetar escolhas
        choicesPanel.SetActive(false);
        inChoices = false;

        // Digitar texto
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeText(page));

        // Preparar opções
        currentOptions = page.options;
    }

    // =========================
    // Digitar letra por letra
    // =========================
    IEnumerator TypeText(DialoguePage page)
    {
        isTyping = true;
        dialogueText.text = "";

        foreach (char c in fullText) //Adicionar charactere AO texto atual da tela.
        {
            dialogueText.text += c;

            if (blip != null && c != ' ')
                blip.Play();

            yield return new WaitForSecondsRealtime(typingSpeed);
        }

        isTyping = false;

        // Se a página tem opções, ativa-as
        if (page.options != null && page.options.Length > 0)
        {
            SetupChoices(page.options);
        }
        else if (page.isStop)
        {
            // Aqui a página é "final", mas não tem opções
            // Então apenas esperamos o jogador apertar a tecla para acabar
            inChoices = false; // só pra garantir
        }

    }

    // =========================
    // Configura escolhas e cursor
    // =========================
    void SetupChoices(DialogueOption[] options)
    {
        currentOptions = options;
        choicesPanel.SetActive(true);
        selectedIndex = 0;
        inChoices = true;
        UpdateCursor();
    }

    // =========================
    // Atualiza posição do cursor
    // =========================
    void UpdateCursor()
    {
        if (cursor != null && choicePositions.Length > 0)
        {
            int index = Mathf.Clamp(selectedIndex, 0, choicePositions.Length - 1);
            cursor.transform.position = choicePositions[index].position;
        }
    }

    // =========================
    // Lógica de update (input)
    // =========================
    void Update()
    {
        if (!dialoguePanel.activeSelf) return;

        // Pular animação letra por letra
        if (isTyping && Input.GetKeyDown(confirmKey))
        {
            SkipTyping();
            return;
        }

        // Se está em escolhas
        if (inChoices)
        {
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                selectedIndex = (selectedIndex - 1 + currentOptions.Length) % currentOptions.Length;
                UpdateCursor();
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                selectedIndex = (selectedIndex + 1) % currentOptions.Length;
                UpdateCursor();
            }
            else if (Input.GetKeyDown(confirmKey))
            {
                OnOptionSelected(currentOptions[selectedIndex]);
            }

        }
        else
        {
            // Avançar página linear
            if (!isTyping && Input.GetKeyDown(confirmKey))
            {
                if (pages[currentPage].isStop) //Essa é a 219
                {
                    EndDialogue(); // termina diálogo se essa página for "stop"
                }
                else
                {
                    NextPage(); // senão vai para a próxima
                }
            }
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

        // Ativa escolhas se houver
        if (currentOptions != null && currentOptions.Length > 0)
        {
            SetupChoices(currentOptions);
        }
    }

    // =========================
    // Próxima página linear
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
    // Quando o jogador escolhe uma opção
    // =========================
    void OnOptionSelected(DialogueOption option)
    {
        inChoices = false;
        choicesPanel.SetActive(false);

        if (option.nextPage < 0)
        {
            EndDialogue();
        }
        else
        {
            if (option.IsGameRestart)
            {
                //GetComponent<GameRestart>().Restart();
            }
            else if (option.IsGameClose)
            {
                // Fecha o Jogo
                Application.Quit();

                // Para Ele no editor
#if UNITY_EDITOR
                EditorApplication.isPlaying = false;
#endif
            }
            currentPage = option.nextPage;
            ShowPage(currentPage);
        }
    }


    // =========================
    // Fecha diálogo
    // =========================
    void EndDialogue()
    {
        dialogueText.text = "";
        currentOptions = null;
        inChoices = false;
        Time.timeScale = 1f;

        dialoguePanel.SetActive(false); // Esconde o grupo todo incluindo cursor
    }
}
