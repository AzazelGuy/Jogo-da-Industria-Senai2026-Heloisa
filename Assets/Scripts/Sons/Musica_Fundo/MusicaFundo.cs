/*
 * ======================================================================================
 * SCRIPT: MusicaFundo.cs (Com Loop Automático por Código)
 * PROJETO: Hyper Bytes
 * ======================================================================================
 */

using UnityEngine;

public class MusicaFundo : MonoBehaviour
{
    [Header("--- CONFIGURAÇÃO DE ÁUDIO ---")]
    [Tooltip("Arraste aqui o AudioClip da sua música.")]
    public AudioClip musicaFundo;
    [Tooltip("Volume da música (de 0 a 1).")]
    [Range(0f, 1f)] public float volumeMusica = 0.5f;

    [Header("--- PERSISTÊNCIA ---")]
    [Tooltip("Marque se quiser que a música continue tocando ao mudar de fase sem reiniciar.")]
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

        // Garante que a música vai repetir em loop por código
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