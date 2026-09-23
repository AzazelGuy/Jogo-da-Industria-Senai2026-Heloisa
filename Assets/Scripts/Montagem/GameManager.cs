using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Gerenciador global do jogo. Persiste entre cenas (DontDestroyOnLoad) e mantém
/// a referência do objeto atualmente selecionado, além de tratar o atalho de reinício (R).
/// </summary>
public class GameManager : MonoBehaviour
{
    #region Singleton

    public static GameManager Instance;

    #endregion

    #region Estado Interno

    private GameObject selectedObject = null;

    #endregion

    #region Ciclo de Vida (Unity)

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        // Atalho de reinício: recarrega a cena atual e limpa o estado da UI/StepChecker
        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            UiController.Instance.ChangeActive(false);
            Destroy(StepChecker.Instance.gameObject);
        }
    }

    #endregion

    #region API Pública - Seleção de Objeto

    /// <summary>
    /// Define o objeto atualmente selecionado pelo jogador.
    /// </summary>
    public void SelectObject(GameObject TargetObject)
    {
        selectedObject = TargetObject;
    }

    /// <summary>
    /// Limpa a referência do objeto atualmente selecionado.
    /// </summary>
    public void ClearObject()
    {
        selectedObject = null;
    }

    public GameObject GetSelectedObject => selectedObject;

    #endregion
}
