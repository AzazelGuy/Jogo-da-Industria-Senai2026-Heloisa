
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class BotaoPecaUI : MonoBehaviour
{
    [SerializeField] private SOPieceData pieceData;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void Start()
    {
        // Optional: If you want to hook up the click event via code automatically, 
        // otherwise you can keep your OnClick() assignment in the Unity Inspector.
        // button.onClick.AddListener(OnClickSpawn);
    }

    private void Update()
    {
        CheckLimitAndHide();
    }

    /// <summary>
    /// Checks if the piece has reached its limit and hides/disables the button.
    /// </summary>
    public void CheckLimitAndHide()
    {
        if (CaixaPecasUi.Instance == null || pieceData == null) return;

        // Check if we can still spawn this piece
        bool canSpawn = CaixaPecasUi.Instance.PodeSpawnar(pieceData, out _);

        // Option A: Completely hide the button GameObject
        if (gameObject.activeSelf != canSpawn)
        {
            gameObject.SetActive(canSpawn);
        }

        // Option B (Alternative): If you prefer to disable the button instead of hiding it completely,
        // comment out the activeSelf check above and use this instead:
        // button.interactable = canSpawn;
    }

    /// <summary>
    /// Hook this method up to your Button's OnClick event in the inspector.
    /// </summary>
    public void OnClickSpawn()
    {
        if (CaixaPecasUi.Instance != null && pieceData != null)
        {
            CaixaPecasUi.Instance.SpawnarPecaPeloSO(pieceData);

            // Immediately update state after spawning
            CheckLimitAndHide();
        }
    }
}