using UnityEngine;

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
