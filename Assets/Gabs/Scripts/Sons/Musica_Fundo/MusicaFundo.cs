/*
 * ======================================================================================
 * SCRIPT: MusicaFundo.cs (Com Loop Automatico por Codigo)
 * PROJETO: Hyper Bytes
 * ======================================================================================
 */

using UnityEngine;

public class MusicaFundo : MonoBehaviour
{
    [Header("--- CONFIGURACAO DE AUDIO ---")]
    [Tooltip("Arraste aqui o AudioClip da sua musica.")]
    public AudioClip musicaFundo;
    [Tooltip("Volume da musica (de 0 a 1).")]
    [Range(0f, 1f)] public float volumeMusica = 0.5f;

    [Header("--- PERSISTENCIA ---")]
    [Tooltip("Marque se quiser que a musica continue tocando ao mudar de fase sem reiniciar.")]
    public bool persistirEntreCenas = false;

    private static MusicaFundo instancia;
    private AudioSource audioSource;

    private void Awake()
    {
        if (persistirEntreCenas)
        {
            if (instancia == null)
            {
                instancia = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        // Configura o AudioSource automaticamente
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.clip = musicaFundo;
        audioSource.volume = volumeMusica;

        // Garante que a musica vai repetir em loop por codigo
        audioSource.loop = true;
    }

    private void Start()
    {
        if (audioSource != null && !audioSource.isPlaying && musicaFundo != null)
        {
            audioSource.Play();
        }
    }
}