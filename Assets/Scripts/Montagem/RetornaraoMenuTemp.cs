using UnityEngine;
using UnityEngine.SceneManagement;

public class RetornaraoMenuTemp : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.X))
        {
            SceneManager.LoadScene("MenuInicial");
        }
    }
}
