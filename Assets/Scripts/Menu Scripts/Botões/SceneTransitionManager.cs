using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    public void ModoMontagem()
    {
        SceneManager.LoadScene("ModoMontagem");
    }

    public void ModoReparo()
    {
        SceneManager.LoadScene("Fase1");
    }
}