/*
 * ======================================================================================
 * SCRIPT: GerenciadorSfx.cs (Gerenciador Global de Efeitos Sonoros)
 * PROJETO: Hyper Bytes
 * ======================================================================================
 */

using UnityEngine;

public class GerenciadorSfx : MonoBehaviour
{
    public static GerenciadorSfx Instancia { get; private set; }

    [Header("--- CLIPES DE ÁUDIO (SFX) ---")]
    [Tooltip("Arraste aqui o som de clique de botão.")]
    public AudioClip somBotao;
    [Tooltip("Arraste aqui o som de acerto.")]
    public AudioClip somAcerto;
    [Tooltip("Arraste aqui o som de erro.")]
    public AudioClip somErro;

    [Header("--- CONFIGURAÇÃO ---")]
    [Range(0f, 1f)] public float volumeSfx = 1f;

    private AudioSource audioSource;

    private void Awake()
    {
        // Padrão Singleton para garantir um único gerenciador na cena/jogo
        if (Instancia == null)
        {
            Instancia = this;
            DontDestroyOnLoad(gameObject); // Opcional: mantém o som entre as fases se desejar
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    // Métodos públicos para tocar os sons de qualquer lugar do jogo
    public void TocarSomBotao()
    {
        TocarEfeito(somBotao);
    }

    public void TocarSomAcerto()
    {
        TocarEfeito(somAcerto);
    }

    public void TocarSomErro()
    {
        TocarEfeito(somErro);
    }

    private void TocarEfeito(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip, volumeSfx);
        }
    }
}