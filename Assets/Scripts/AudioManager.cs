using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    // ─── Singleton ───────────────────────────────────────────────
    public static AudioManager Instance { get; private set; }

    // ─── Referências de AudioSource ──────────────────────────────
    [Header("Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    // ─── Volumes (0-1) ───────────────────────────────────────────
    [Header("Volumes iniciais")]
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.5f;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;

    [Header("Configuração de ajuste")]
    [SerializeField] private float volumeStep = 0.1f; // quanto aumenta/diminui


    // ─── Estado ──────────────────────────────────────────────────
    private bool musicMuted = false;
    private bool sfxMuted = false;
    private Coroutine fadeCoroutine;
    private AudioClip _savedClip;
    private bool _savedWasPlaying;
    private Coroutine introCoroutine;


    // ─── Propriedades públicas ────────────────────────────────────
    public float MusicVolume
    {
        get => musicVolume;
        set
        {
            musicVolume = Mathf.Clamp01(value);
            if (!musicMuted) musicSource.volume = musicVolume;
        }
    }

    public float SFXVolume
    {
        get => sfxVolume;
        set
        {
            sfxVolume = Mathf.Clamp01(value);
            if (!sfxMuted) sfxSource.volume = sfxVolume;
        }
    }

    public AudioClip CrrentMusicClip => musicSource.clip;
    public bool IsMusicPlaying => musicSource.isPlaying;
    // ─────────────────────────────────────────────────────────────
    #region Unity Lifecycle

    private void Awake()
    {
        // Singleton com DontDestroyOnLoad
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Cria AudioSources automaticamente se não forem atribuídos no Inspector
        if (musicSource == null) musicSource = CreateSource("MusicSource", true);
        if (sfxSource == null) sfxSource = CreateSource("SFXSource", false);

        // Aplica volumes iniciais
        musicSource.volume = musicVolume;
        sfxSource.volume = sfxVolume;
    }

    #endregion

    // ─────────────────────────────────────────────────────────────
    #region Música

    /// <summary>Toca uma música imediatamente (corta a atual).</summary>
    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (clip == null) return;

        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.volume = musicMuted ? 0f : musicVolume;
        musicSource.Play();
    }

    /// <summary>Toca a intro e a musica imediatamente (corta a atual).</summary>
    public void PlayMusicWithIntro(AudioClip intro, AudioClip loopClip, bool useFade = false, float fadeDuration = 1f)
    {
        if (introCoroutine != null)
            StopCoroutine(introCoroutine);

        introCoroutine = StartCoroutine(PlayIntroRoutine(intro, loopClip, useFade, fadeDuration));
    }

    /// <summary>Faz crossfade entre a música atual e a nova.</summary>
    public void PlayMusicWithFade(AudioClip clip, float fadeDuration = 1f, bool loop = true)
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeMusic(clip, fadeDuration, loop));
    }

    /// <summary> Toca Intro e depois a música (com ou sem fade) </summary>

    private IEnumerator PlayIntroRoutine(AudioClip intro, AudioClip loopClip, bool useFade, float fadeDuration)
    {
        // Toca a intro
        if (useFade)
            yield return FadeToClip(intro, fadeDuration, false);
        else
        {
            musicSource.Stop();
            musicSource.clip = intro;
            musicSource.loop = false;
            musicSource.volume = musicMuted ? 0f : musicVolume;
            musicSource.Play();
        }

        yield return new WaitForSeconds(intro.length);

        // Entra no loop
        musicSource.clip = loopClip;
        musicSource.loop = true;
        musicSource.Play();

        introCoroutine = null;
    }

    private IEnumerator FadeToClip(AudioClip clip, float duration, bool loop)
    {
        float targetVolume = musicMuted ? 0f : musicVolume;

        float startVolume = musicSource.volume;
        float elapsed = 0f;

        while (elapsed < duration / 2f)
        {
            elapsed += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0, elapsed / (duration / 2f));
            yield return null;
        }

        musicSource.Stop();
        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.Play();

        elapsed = 0f;

        while (elapsed < duration / 2f)
        {
            elapsed += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(0, targetVolume, elapsed / (duration / 2f));
            yield return null;
        }

        musicSource.volume = targetVolume;
    }


    public void PauseMusic() => musicSource.Pause();
    public void ResumeMusic() => musicSource.UnPause();
    public void StopMusic() => musicSource.Stop();

    // Salva a música atual e toca uma nova
    public void SaveAndPlayMusic(AudioClip clip, float fadeDuration = 1f, bool useFade = true)
    {
        _savedClip = musicSource.clip;
        _savedWasPlaying = musicSource.isPlaying;

        if (useFade)
            PlayMusicWithFade(clip, fadeDuration);
        else
            PlayMusic(clip);
    }

    public void RestoreSavedMusic(float fadeDuration = 1f, bool useFade = true)
    {
        if (!_savedWasPlaying || _savedClip == null)
        {
            StopMusic();
            _savedClip = null;
            return;
        }

        if (useFade)
            PlayMusicWithFade(_savedClip, fadeDuration);
        else
            PlayMusic(_savedClip);

        _savedClip = null;
    }
    #endregion

    // ─────────────────────────────────────────────────────────────
    #region SFX

    /// <summary>Toca um efeito sonoro com volume e pitch opcionais.</summary>
    public void PlaySFX(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (clip == null || sfxMuted) return;

        sfxSource.pitch = pitch;
        sfxSource.PlayOneShot(clip, volume * sfxVolume);
    }

    /// <summary>Toca um SFX com pitch aleatório dentro de um range (útil para variação).</summary>
    public void PlaySFXRandomPitch(AudioClip clip, float minPitch = 0.9f, float maxPitch = 1.1f)
    {
        PlaySFX(clip, pitch: Random.Range(minPitch, maxPitch));
    }

    /// <summary>Toca um SFX aleatório de um array de clips.</summary>
    public void PlayRandomSFX(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0) return;
        PlaySFX(clips[Random.Range(0, clips.Length)]);
    }

    #endregion

    // ─────────────────────────────────────────────────────────────
    #region Mute

    public void SetMusicMute(bool mute)
    {
        musicMuted = mute;
        musicSource.volume = mute ? 0f : musicVolume;
    }

    public void SetSFXMute(bool mute)
    {
        sfxMuted = mute;
        sfxSource.volume = mute ? 0f : sfxVolume;
    }

    public void ToggleMusicMute() => SetMusicMute(!musicMuted);
    public void ToggleSFXMute() => SetSFXMute(!sfxMuted);

    #endregion

    // ─────────────────────────────────────────────────────────────
    #region Utilitários privados

    private AudioSource CreateSource(string sourceName, bool loop)
    {
        var go = new GameObject(sourceName);
        go.transform.SetParent(transform);
        var src = go.AddComponent<AudioSource>();
        src.loop = loop;
        src.playOnAwake = false;
        return src;
    }

    private IEnumerator FadeMusic(AudioClip newClip, float duration, bool loop)
    {
        float targetVolume = musicMuted ? 0f : musicVolume;

        // Fade out
        float startVolume = musicSource.volume;
        float elapsed = 0f;

        while (elapsed < duration / 2f)
        {
            elapsed += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / (duration / 2f));
            yield return null;
        }

        musicSource.Stop();
        musicSource.clip = newClip;
        musicSource.loop = loop;
        musicSource.Play();

        // Fade in
        elapsed = 0f;
        while (elapsed < duration / 2f)
        {
            elapsed += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(0f, targetVolume, elapsed / (duration / 2f));
            yield return null;
        }

        musicSource.volume = targetVolume;
        fadeCoroutine = null;
    }

    #endregion

    // ─────────────────────────────────────────────
    // Controle de Volume (Incremento)
    #region
    // Música
    public void IncreaseMusicVolume()
    {
        MusicVolume += volumeStep;
    }

    public void DecreaseMusicVolume()
    {
        MusicVolume -= volumeStep;
    }

    // SFX
    public void IncreaseSFXVolume()
    {
        SFXVolume += volumeStep;
    }

    public void DecreaseSFXVolume()
    {
        SFXVolume -= volumeStep;
    }

    // Mute geral (opcional)
    public void ToggleAllMute()
    {
        bool mute = !musicMuted || !sfxMuted;
        SetMusicMute(mute);
        SetSFXMute(mute);
    }
    #endregion
}
