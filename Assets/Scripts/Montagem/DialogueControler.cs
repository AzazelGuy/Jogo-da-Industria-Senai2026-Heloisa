using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

[Serializable]
public class DialoguePage
{
    [TextArea(2, 5)] public string text;

    [Tooltip("Image (UI) da cena mostrada nesta página. Pode ter Animator próprio. Vazio = nenhuma ilustração.")]
    public Image tutorialImage;
}

public class DialogueControler : MonoBehaviour
{
    public static DialogueControler instance;
    public static event Action OnDialogueEnd;

    [Header("UI")]
    [SerializeField] private GameObject dialogueGroup;
    [SerializeField] private TMP_Text dialogueText;

    [Header("Personagem (Image + Animator)")]
    [SerializeField] private Image portrait;
    [Tooltip("Parâmetro bool do Animator: true = falando, false = parado.")]
    [SerializeField] private string talkingParam = "IsTalking";
    [SerializeField] private Animator portraitAnimator;

    [Header("Digitação")]
    [SerializeField] private float typingSpeed = 0.04f;
    [Tooltip("Áudio de fala do personagem. Só toca de novo quando o anterior terminar.")]
    [SerializeField, FormerlySerializedAs("blip")] private AudioClip voiceClip;

    [Header("Input")]
    [SerializeField] private KeyCode confirmKey = KeyCode.E;

    [Header("Feedback de Erro")]
    [TextArea(2, 5)]
    [SerializeField] private string defaultMistakeMessage = "Essa ação não pode ser feita agora.";
    [SerializeField, Min(0f)] private float mistakeFadeDuration = 0.2f;

    private DialoguePage[] pages;
    private int currentPage;
    private bool isTyping;
    private float previousTimeScale = 1f;
    private bool currentDialogueIsMistake;
    private bool isClosingMistakeDialogue;
    private readonly Queue<string> pendingMistakes = new Queue<string>();


    private int talkingHash;
    private Coroutine typingRoutine;
    private Coroutine mistakeFadeRoutine;
    private CanvasGroup dialogueCanvasGroup;
    private Image currentIllustration;
    private bool IsActive => dialogueGroup != null && dialogueGroup.activeSelf;

    // =========================
    // Ciclo de vida
    // =========================
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        talkingHash = Animator.StringToHash(talkingParam);
        if (portrait != null && portraitAnimator == null) portraitAnimator = portrait.GetComponent<Animator>();
        CreateDefaultDialogueUIIfNeeded();
        if (dialogueGroup != null)
        {
            dialogueCanvasGroup = dialogueGroup.GetComponent<CanvasGroup>();
            if (dialogueCanvasGroup == null) dialogueCanvasGroup = dialogueGroup.AddComponent<CanvasGroup>();
        }
    }

    private void Start()
    {
        if (dialogueGroup == null)
        {
            Debug.LogError("[DialogueControler] Nenhum painel de diálogo foi configurado ou criado.", this);
            return;
        }

        dialogueGroup.SetActive(false);
    }

    private void OnDestroy()
    {
        // Só limpa se for a instância "oficial" (o duplicado destruído não pode zerar a real)
        if (instance != this) return;

        instance = null;
        StepChecker.OnMistakeRaised -= QueueMistakeDialogue;
        if (currentDialogueIsMistake || isClosingMistakeDialogue)
            StepChecker.SetMistakeFeedbackActive(false);
        if (IsActive) Time.timeScale = previousTimeScale; // não deixa o jogo congelado
    }

    private void OnEnable()
    {
        StepChecker.OnMistakeRaised += QueueMistakeDialogue;
    }

    private void OnDisable()
    {
        StepChecker.OnMistakeRaised -= QueueMistakeDialogue;
    }

    private void Update()
    {
        if (IsActive)
        {
            if (isClosingMistakeDialogue) return;
            if (!Input.GetKeyDown(confirmKey)) return;

            if (isTyping) FinishTyping();
            else NextPage();
            return;
        }

        if (pendingMistakes.Count > 0)
            StartMistakeDialogue(pendingMistakes.Dequeue());
    }

    private void QueueMistakeDialogue(string message)
    {
        if (instance != this || SceneManager.GetActiveScene().name != "ModoMontagem") return;
        pendingMistakes.Enqueue(string.IsNullOrWhiteSpace(message) ? defaultMistakeMessage : message);
    }

    private void StartMistakeDialogue(string message)
    {
        if (dialogueGroup == null || dialogueText == null)
        {
            Debug.LogError("[DialogueControler] Não foi possível mostrar o erro: painel ou texto não configurado.", this);
            return;
        }

        currentDialogueIsMistake = true;
        if (dialogueCanvasGroup != null) dialogueCanvasGroup.alpha = 0f;
        StepChecker.SetMistakeFeedbackActive(true);
        StartDialogue(new[] { new DialoguePage { text = $"{message}\n\nAperte {confirmKey} para continuar." } });
    }

    private void CreateDefaultDialogueUIIfNeeded()
    {
        if (dialogueGroup != null && dialogueText != null) return;
        if (dialogueGroup != null || dialogueText != null)
        {
            Debug.LogError("[DialogueControler] Configure dialogueGroup e dialogueText juntos.", this);
            return;
        }

        GameObject canvasObject = new GameObject("DialogueCanvas");
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 10000;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasObject.AddComponent<GraphicRaycaster>();

        TMP_FontAsset fallbackFont = TMP_Settings.defaultFontAsset;
        TMP_Text existingText = FindFirstObjectByType<TMP_Text>();
        if (existingText != null) fallbackFont = existingText.font;

        dialogueGroup = new GameObject("DialogueGroup", typeof(RectTransform), typeof(CanvasGroup));
        dialogueGroup.transform.SetParent(canvasObject.transform, false);
        RectTransform groupRect = (RectTransform)dialogueGroup.transform;
        groupRect.anchorMin = Vector2.zero;
        groupRect.anchorMax = Vector2.one;
        groupRect.offsetMin = Vector2.zero;
        groupRect.offsetMax = Vector2.zero;

        GameObject panel = new GameObject("DialoguePanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(dialogueGroup.transform, false);
        RectTransform panelRect = (RectTransform)panel.transform;
        panelRect.anchorMin = new Vector2(0.06f, 0.06f);
        panelRect.anchorMax = new Vector2(0.94f, 0.3f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(0.03f, 0.04f, 0.08f, 0.94f);

        GameObject textObject = new GameObject("DialogueText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(panel.transform, false);
        RectTransform textRect = (RectTransform)textObject.transform;
        textRect.anchorMin = new Vector2(0.06f, 0.12f);
        textRect.anchorMax = new Vector2(0.94f, 0.88f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        dialogueText = textObject.GetComponent<TextMeshProUGUI>();
        dialogueText.font = fallbackFont;
        dialogueText.fontSize = 36f;
        dialogueText.enableAutoSizing = true;
        dialogueText.fontSizeMin = 22f;
        dialogueText.fontSizeMax = 36f;
        dialogueText.alignment = TextAlignmentOptions.MidlineLeft;
        dialogueText.color = Color.white;
        dialogueText.raycastTarget = false;
        dialogueGroup.SetActive(false);
    }

    // =========================
    // Fluxo do diálogo
    // =========================
    public void StartDialogue(DialoguePage[] dialoguePages)
    {
        if (dialoguePages == null || dialoguePages.Length == 0 || dialogueGroup == null || dialogueText == null || IsActive) return;

        pages = dialoguePages;
        currentPage = 0;

        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        dialogueGroup.SetActive(true);
        if (currentDialogueIsMistake && dialogueCanvasGroup != null)
            mistakeFadeRoutine = StartCoroutine(FadeDialogueGroup(0f, 1f));
        ShowPage(currentPage);
    }

    private void NextPage()
    {
        currentPage++;

        if (currentPage >= pages.Length) EndDialogue();
        else ShowPage(currentPage);
    }

    private void EndDialogue()
    {
        bool wasMistakeDialogue = currentDialogueIsMistake;
        currentDialogueIsMistake = false;
        StopRoutine(ref typingRoutine);
        ShowIllustration(null);

        isTyping = false;
        SetTalking(false);
        StopVoice();
        dialogueText.text = string.Empty;

        if (wasMistakeDialogue)
        {
            StopRoutine(ref mistakeFadeRoutine);
            isClosingMistakeDialogue = true;
            mistakeFadeRoutine = StartCoroutine(FadeOutAndCloseMistakeDialogue());
        }
        else
        {
            CloseDialogueGroup();
            OnDialogueEnd?.Invoke();
        }
    }

    private IEnumerator FadeOutAndCloseMistakeDialogue()
    {
        yield return FadeDialogueGroup(1f, 0f);
        mistakeFadeRoutine = null;
        isClosingMistakeDialogue = false;
        CloseDialogueGroup();

        if (pendingMistakes.Count > 0)
            StartMistakeDialogue(pendingMistakes.Dequeue());
        else
            StepChecker.SetMistakeFeedbackActive(false);
    }

    private IEnumerator FadeDialogueGroup(float from, float to)
    {
        if (dialogueCanvasGroup == null || mistakeFadeDuration <= 0f)
        {
            if (dialogueCanvasGroup != null) dialogueCanvasGroup.alpha = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < mistakeFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            dialogueCanvasGroup.alpha = Mathf.Lerp(from, to, elapsed / mistakeFadeDuration);
            yield return null;
        }

        dialogueCanvasGroup.alpha = to;
    }

    private void CloseDialogueGroup()
    {
        Time.timeScale = previousTimeScale;
        dialogueGroup.SetActive(false);
        if (dialogueCanvasGroup != null) dialogueCanvasGroup.alpha = 1f;
    }

    // =========================
    // Página
    // =========================
    private void ShowPage(int index)
    {
        DialoguePage page = pages[index];

        // Texto completo de uma vez; a "digitação" só revela os caracteres (respeita rich text e não gera lixo de string)
        dialogueText.text = page.text;
        dialogueText.maxVisibleCharacters = 0;
        dialogueText.ForceMeshUpdate();

        ShowIllustration(page.tutorialImage);

        StopRoutine(ref typingRoutine);
        typingRoutine = StartCoroutine(TypeText());
    }

    private IEnumerator TypeText()
    {
        isTyping = true;
        SetTalking(true);

        var wait = new WaitForSecondsRealtime(typingSpeed);
        TMP_TextInfo info = dialogueText.textInfo;

        for (int i = 0; i < info.characterCount; i++)
        {
            dialogueText.maxVisibleCharacters = i + 1;

            // Só (re)toca quando o áudio anterior terminou: sem sobreposição e sem tocar 1000x
            if (voiceClip != null && AudioManager.Instance != null && !AudioManager.Instance.IsVoicePlaying)
                AudioManager.Instance.PlayVoice(voiceClip);

            yield return wait;
        }

        FinishTyping();
    }

    // Serve tanto pro fim natural da digitação quanto pro "pular" com o botão
    private void FinishTyping()
    {
        StopRoutine(ref typingRoutine);

        dialogueText.maxVisibleCharacters = int.MaxValue;
        isTyping = false;
        SetTalking(false);
        StopVoice();
    }

    private void StopVoice()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.StopVoice();
    }

    // =========================
    // Animações
    // =========================
    private void SetTalking(bool talking)
    {
        if (portraitAnimator != null) portraitAnimator.SetBool(talkingHash, talking);
    }

    // Liga a Image da página atual e desliga a anterior.
    // Ao reativar o GameObject, o Animator dela recomeça a animação do zero.
    private void ShowIllustration(Image next)
    {
        if (currentIllustration != null && currentIllustration != next)
            currentIllustration.gameObject.SetActive(false);

        currentIllustration = next;

        if (currentIllustration != null)
            currentIllustration.gameObject.SetActive(true);
    }

    private void StopRoutine(ref Coroutine routine)
    {
        if (routine == null) return;
        StopCoroutine(routine);
        routine = null;
    }
}