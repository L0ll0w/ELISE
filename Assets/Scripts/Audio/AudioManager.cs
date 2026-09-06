using System.Collections;
using UnityEngine;

/// <summary>
/// Gestionnaire audio centralisé pour le jeu.
/// Gère la lecture des musiques de zone avec fondus croisés (crossfade) fluides entre les pistes,
/// la mise en pause/reprise lors des combats, et la lecture des effets sonores.
/// </summary>
[AddComponentMenu("2.5D RPG/Audio/Audio Manager")]
public class AudioManager : MonoBehaviour
{
    private static AudioManager instance;
    public static AudioManager Instance
    {
        get
        {
            if (instance == null)
            {
                // Recherche dans la scène
                instance = FindFirstObjectByType<AudioManager>();
                if (instance == null)
                {
                    // Instanciation automatique d'un GameObject AudioManager persistant
                    GameObject go = new GameObject("AudioManager");
                    instance = go.AddComponent<AudioManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return instance;
        }
    }

    [Header("Réglages Volume")]
    [Range(0f, 1f)]
    [SerializeField] private float masterVolume = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float musicVolume = 0.8f;

    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 1f;

    // Deux AudioSource pour le fondu croisé (Crossfade)
    private AudioSource audioSourceA;
    private AudioSource audioSourceB;
    private AudioSource sfxSource;

    private bool isSourceAPlaying = false;
    private Coroutine crossfadeCoroutine;

    // Sauvegarde pour l'interruption par le combat
    private AudioClip savedZoneMusic;
    private float savedZoneMusicTime = 0f;
    private bool isMusicPausedForCombat = false;

    public float MasterVolume
    {
        get => masterVolume;
        set
        {
            masterVolume = Mathf.Clamp01(value);
            UpdateVolumes();
        }
    }

    public float MusicVolume
    {
        get => musicVolume;
        set
        {
            musicVolume = Mathf.Clamp01(value);
            UpdateVolumes();
        }
    }

    public float SFXVolume
    {
        get => sfxVolume;
        set
        {
            sfxVolume = Mathf.Clamp01(value);
            if (sfxSource != null) sfxSource.volume = sfxVolume * masterVolume;
        }
    }

    public AudioClip CurrentZoneMusic { get; private set; }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudioSources();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void InitializeAudioSources()
    {
        audioSourceA = gameObject.AddComponent<AudioSource>();
        audioSourceA.loop = true;
        audioSourceA.playOnAwake = false;

        audioSourceB = gameObject.AddComponent<AudioSource>();
        audioSourceB.loop = true;
        audioSourceB.playOnAwake = false;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
    }

    /// <summary>
    /// Joue une musique de zone avec un fondu croisé (crossfade) fluide par rapport à la musique actuelle.
    /// Si la musique demandée est déjà en cours de lecture, l'appel est ignoré.
    /// </summary>
    public void PlayZoneMusic(AudioClip newClip, float fadeDuration = 1.5f)
    {
        if (newClip == null)
        {
            StopZoneMusic(fadeDuration);
            return;
        }

        // Si la musique demandée est déjà celle en cours de lecture, ne rien faire
        if (CurrentZoneMusic == newClip && ActiveAudioSource != null && ActiveAudioSource.isPlaying)
        {
            return;
        }

        CurrentZoneMusic = newClip;
        isMusicPausedForCombat = false;

        if (crossfadeCoroutine != null)
        {
            StopCoroutine(crossfadeCoroutine);
        }

        crossfadeCoroutine = StartCoroutine(CrossfadeRoutine(newClip, fadeDuration));
    }

    /// <summary>
    /// Arrête la musique de zone actuelle avec un fondu de sortie.
    /// </summary>
    public void StopZoneMusic(float fadeDuration = 1.0f)
    {
        CurrentZoneMusic = null;
        if (crossfadeCoroutine != null) StopCoroutine(crossfadeCoroutine);
        crossfadeCoroutine = StartCoroutine(FadeOutCurrentMusicRoutine(fadeDuration));
    }

    /// <summary>
    /// Met en pause la musique de zone active en prévision d'un combat, en sauvegardant le temps de lecture.
    /// </summary>
    public void PauseZoneMusicForCombat(float fadeDuration = 0.8f)
    {
        AudioSource active = ActiveAudioSource;
        if (active != null && active.isPlaying)
        {
            savedZoneMusic = active.clip;
            savedZoneMusicTime = active.time;
            isMusicPausedForCombat = true;
            Debug.Log($"[AudioManager] Musique de zone '{savedZoneMusic.name}' mise en pause pour le combat à {savedZoneMusicTime:F1}s.");

            if (crossfadeCoroutine != null) StopCoroutine(crossfadeCoroutine);
            crossfadeCoroutine = StartCoroutine(FadeOutCurrentMusicRoutine(fadeDuration, pauseInsteadOfStop: true));
        }
    }

    /// <summary>
    /// Restaure la musique de zone mise en pause après la fin du combat.
    /// </summary>
    public void ResumeZoneMusicAfterCombat(float fadeDuration = 0.8f)
    {
        if (isMusicPausedForCombat && savedZoneMusic != null)
        {
            Debug.Log($"[AudioManager] Reprise de la musique de zone '{savedZoneMusic.name}' après le combat.");
            CurrentZoneMusic = savedZoneMusic;
            isMusicPausedForCombat = false;

            if (crossfadeCoroutine != null) StopCoroutine(crossfadeCoroutine);
            crossfadeCoroutine = StartCoroutine(ResumeMusicRoutine(savedZoneMusic, savedZoneMusicTime, fadeDuration));
        }
    }

    /// <summary>
    /// Joue un effet sonore ponctuels (SFX).
    /// </summary>
    public void PlaySFX(AudioClip clip, float volumeScale = 1.0f)
    {
        if (clip == null || sfxSource == null) return;
        float finalVol = masterVolume * sfxVolume * Mathf.Clamp01(volumeScale);
        sfxSource.PlayOneShot(clip, finalVol);
    }

    private AudioSource ActiveAudioSource => isSourceAPlaying ? audioSourceA : audioSourceB;
    private AudioSource StandbyAudioSource => isSourceAPlaying ? audioSourceB : audioSourceA;

    private IEnumerator CrossfadeRoutine(AudioClip newClip, float fadeDuration)
    {
        AudioSource fromSource = ActiveAudioSource;
        AudioSource toSource = StandbyAudioSource;

        float targetMaxVol = masterVolume * musicVolume;

        // Préparer la nouvelle AudioSource
        toSource.clip = newClip;
        toSource.volume = 0f;
        toSource.time = 0f;
        toSource.Play();

        // Inverser les rôles d'affichage
        isSourceAPlaying = !isSourceAPlaying;

        float elapsed = 0f;
        float startFromVol = fromSource != null ? fromSource.volume : 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);

            if (fromSource != null && fromSource.isPlaying)
            {
                fromSource.volume = Mathf.Lerp(startFromVol, 0f, t);
            }

            toSource.volume = Mathf.Lerp(0f, targetMaxVol, t);
            yield return null;
        }

        if (fromSource != null)
        {
            fromSource.Stop();
            fromSource.volume = 0f;
        }

        toSource.volume = targetMaxVol;
    }

    private IEnumerator FadeOutCurrentMusicRoutine(float fadeDuration, bool pauseInsteadOfStop = false)
    {
        AudioSource active = ActiveAudioSource;
        if (active == null || !active.isPlaying) yield break;

        float startVol = active.volume;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            active.volume = Mathf.Lerp(startVol, 0f, t);
            yield return null;
        }

        if (pauseInsteadOfStop)
        {
            active.Pause();
        }
        else
        {
            active.Stop();
        }
        active.volume = 0f;
    }

    private IEnumerator ResumeMusicRoutine(AudioClip clip, float startTime, float fadeDuration)
    {
        AudioSource active = ActiveAudioSource;
        active.clip = clip;
        active.time = Mathf.Clamp(startTime, 0f, clip.length - 0.1f);
        active.volume = 0f;
        active.Play();

        float targetMaxVol = masterVolume * musicVolume;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            active.volume = Mathf.Lerp(0f, targetMaxVol, t);
            yield return null;
        }

        active.volume = targetMaxVol;
    }

    private void UpdateVolumes()
    {
        float targetMusicVol = masterVolume * musicVolume;
        if (ActiveAudioSource != null && ActiveAudioSource.isPlaying)
        {
            ActiveAudioSource.volume = targetMusicVol;
        }
        if (sfxSource != null)
        {
            sfxSource.volume = masterVolume * sfxVolume;
        }
    }
}
