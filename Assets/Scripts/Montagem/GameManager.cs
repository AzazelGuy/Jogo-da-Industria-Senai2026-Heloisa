using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    private GameObject selectedObject = null;

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
        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            UiController.Instance.ChangeActive(false);
            Destroy(StepChecker.Instance.gameObject);
        }
    }
    public void SelectObject(GameObject TargetObject)
    {
        selectedObject = TargetObject;
    }
    public void ClearObject()
    {
        selectedObject = null;
    }

    public GameObject GetSelectedObject => selectedObject;
}
