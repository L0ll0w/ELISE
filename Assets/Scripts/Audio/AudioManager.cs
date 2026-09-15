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

    [System.Serializable]
    private struct PausedAudioSourceData
    {
        public AudioSource source;
        public float savedTime;
        public float savedVolume;
        public AudioClip clip;
    }

    private readonly System.Collections.Generic.List<PausedAudioSourceData> pausedExternalSources = new System.Collections.Generic.List<PausedAudioSourceData>();
    private readonly System.Collections.Generic.List<Coroutine> externalFadeCoroutines = new System.Collections.Generic.List<Coroutine>();

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
        // 1. Pause de la musique de zone gérée par l'AudioManager
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

        // 2. Pause de toutes les sources audio environnementales / de musique actives dans la scène (ex: Player P, ambiances de scène)
        PauseExternalAudioSources(fadeDuration);
    }

    /// <summary>
    /// Restaure la musique de zone mise en pause après la fin du combat.
    /// Restaure également toutes les sources audio environnementales externes mises en pause.
    /// </summary>
    public void ResumeZoneMusicAfterCombat(float fadeDuration = 0.8f)
    {
        // 1. Reprise de la musique de zone AudioManager
        if (isMusicPausedForCombat && savedZoneMusic != null)
        {
            Debug.Log($"[AudioManager] Reprise de la musique de zone '{savedZoneMusic.name}' après le combat.");
            CurrentZoneMusic = savedZoneMusic;
            isMusicPausedForCombat = false;

            if (crossfadeCoroutine != null) StopCoroutine(crossfadeCoroutine);
            crossfadeCoroutine = StartCoroutine(ResumeMusicRoutine(savedZoneMusic, savedZoneMusicTime, fadeDuration));
        }

        // 2. Reprise des sources environnementales externes
        ResumeExternalAudioSources(fadeDuration);
    }

    private void PauseExternalAudioSources(float fadeDuration)
    {
        // Stopper les coroutines de fondu externe en cours
        foreach (var c in externalFadeCoroutines)
        {
            if (c != null) StopCoroutine(c);
        }
        externalFadeCoroutines.Clear();
        pausedExternalSources.Clear();

        AudioSource[] allSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
        AudioSource beatManagerSource = BeatManager.Instance != null ? BeatManager.Instance.AudioSource : null;
        GameObject beatManagerObj = BeatManager.Instance != null ? BeatManager.Instance.gameObject : null;

        foreach (var src in allSources)
        {
            if (src == null) continue;

            // Ne pas toucher aux AudioSources internes de l'AudioManager
            if (src == audioSourceA || src == audioSourceB || src == sfxSource) continue;

            // Ne pas toucher à l'AudioSource du BeatManager (musique du combat en rythme)
            if (src == beatManagerSource || (beatManagerObj != null && src.gameObject == beatManagerObj)) continue;

            // Vérifier si cette source est active et joue un son (musique ou boucle d'ambiance)
            if (src.isPlaying && src.clip != null)
            {
                PausedAudioSourceData data = new PausedAudioSourceData
                {
                    source = src,
                    savedTime = src.time,
                    savedVolume = src.volume > 0f ? src.volume : 1f,
                    clip = src.clip
                };
                pausedExternalSources.Add(data);

                Debug.Log($"[AudioManager] Source audio environnementale '{src.gameObject.name}' ({src.clip.name}) mise en pause pour le combat à {src.time:F1}s.");
                Coroutine coroutine = StartCoroutine(FadeOutAndPauseExternalSource(src, fadeDuration));
                externalFadeCoroutines.Add(coroutine);
            }
        }
    }

    private IEnumerator FadeOutAndPauseExternalSource(AudioSource src, float duration)
    {
        if (src == null) yield break;
        float startVol = src.volume;
        float elapsed = 0f;

        while (elapsed < duration && src != null)
        {
            elapsed += Time.deltaTime;
            src.volume = Mathf.Lerp(startVol, 0f, elapsed / duration);
            yield return null;
        }

        if (src != null)
        {
            src.Pause();
            src.volume = 0f;
        }
    }

    private void ResumeExternalAudioSources(float fadeDuration)
    {
        // Stopper les coroutines de fondu en cours
        foreach (var c in externalFadeCoroutines)
        {
            if (c != null) StopCoroutine(c);
        }
        externalFadeCoroutines.Clear();

        if (pausedExternalSources.Count == 0) return;

        foreach (var data in pausedExternalSources)
        {
            if (data.source != null)
            {
                Debug.Log($"[AudioManager] Reprise de la source audio environnementale '{data.source.gameObject.name}' (clip: {(data.clip != null ? data.clip.name : "null")}) à {data.savedTime:F1}s.");
                Coroutine coroutine = StartCoroutine(FadeInAndResumeExternalSource(data, fadeDuration));
                externalFadeCoroutines.Add(coroutine);
            }
        }

        pausedExternalSources.Clear();
    }

    private IEnumerator FadeInAndResumeExternalSource(PausedAudioSourceData data, float duration)
    {
        AudioSource src = data.source;
        if (src == null) yield break;

        src.gameObject.SetActive(true);
        src.enabled = true;
        src.volume = 0f;

        if (src.clip == null && data.clip != null)
        {
            src.clip = data.clip;
        }

        if (data.clip != null)
        {
            src.time = Mathf.Clamp(data.savedTime, 0f, Mathf.Max(0f, data.clip.length - 0.1f));
        }

        src.UnPause();
        if (!src.isPlaying)
        {
            src.Play();
        }

        float targetVol = data.savedVolume;
        float elapsed = 0f;

        while (elapsed < duration && src != null)
        {
            elapsed += Time.deltaTime;
            src.volume = Mathf.Lerp(0f, targetVol, elapsed / duration);
            yield return null;
        }

        if (src != null)
        {
            src.volume = targetVol;
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
