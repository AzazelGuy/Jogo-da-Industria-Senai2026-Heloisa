using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    [SerializeField] AudioClip select;
    public void ModoMontagem()
    {
        PlayAudio();
        LoadingScreen.Carregar("Tutorial");
    }

    public void PlayAudio()
    {
        AudioManager.Instance.PlaySFX(select);
    }
    public void ModoReparo()
    {
        PlayAudio();
        LoadingScreen.Carregar("Fase1");
    }
}