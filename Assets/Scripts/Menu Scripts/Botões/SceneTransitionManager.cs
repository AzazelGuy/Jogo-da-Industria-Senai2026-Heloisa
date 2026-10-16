using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    [SerializeField] AudioClip select;
    public void ModoMontagem()
    {
        PlayAudio();
        LoadingScreen.Carregar("ModoMontagem");
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