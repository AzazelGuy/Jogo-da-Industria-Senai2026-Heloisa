using UnityEngine;
using UnityEngine.SceneManagement;

public class RetornaraoMenuTemp : MonoBehaviour
{
    private void Update()
    {
        if (MistakeEvent.IsFeedbackActive) return;

        if (Input.GetKeyDown(KeyCode.X))
        {
            LoadingScreen.Carregar("MenuInicial");
        }
    }
}
