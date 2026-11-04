using UnityEngine;
using UnityEngine.InputSystem;

public class CursorUI : MonoBehaviour
{

    public GameObject cursorGO;
    private void Start()
    {
    }

    void Update()
    {
        cursorGO.transform.position = Input.mousePosition;
    }

}
