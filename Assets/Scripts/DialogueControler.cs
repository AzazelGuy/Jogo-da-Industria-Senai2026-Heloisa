using System;
using System.Collections;
using TMPro;
using UnityEngine;
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

    private DialoguePage[] pages;
    private int currentPage;
    private bool isTyping;
    private float previousTimeScale = 1f;


    private int talkingHash;
    private Coroutine typingRoutine;
    private Image currentIllustration;
    private bool IsActive => dialogueGroup.activeSelf;

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
    }

    private void Start() => dialogueGroup.SetActive(false);

    private void OnDestroy()
    {
        // Só limpa se for a instância "oficial" (o duplicado destruído não pode zerar a real)
        if (instance != this) return;

        instance = null;
        if (IsActive) Time.timeScale = previousTimeScale; // não deixa o jogo congelado
    }

    private void Update()
    {
        if (!IsActive || !Input.GetKeyDown(confirmKey)) return;

        if (isTyping) FinishTyping();
        else NextPage();
    }

    // =========================
    // Fluxo do diálogo
    // =========================
    public void StartDialogue(DialoguePage[] dialoguePages)
    {
        if (dialoguePages == null || dialoguePages.Length == 0 || IsActive) return;

        pages = dialoguePages;
        currentPage = 0;

        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        dialogueGroup.SetActive(true);
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
        StopRoutine(ref typingRoutine);
        ShowIllustration(null);

        isTyping = false;
        SetTalking(false);
        StopVoice();
        dialogueText.text = string.Empty;

        Time.timeScale = previousTimeScale;
        dialogueGroup.SetActive(false);

        OnDialogueEnd?.Invoke();
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