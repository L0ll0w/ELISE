using System;
using UnityEngine;

/// <summary>
/// Gère la synchronisation temporelle ultra-précise de la musique et des battements (Beats)
/// en utilisant AudioSettings.dspTime pour éviter le décalage (lag) lié au rafraîchissement d'affichage.
/// </summary>
[AddComponentMenu("2.5D RPG/Rhythm/Beat Manager")]
public class BeatManager : MonoBehaviour
{
    public static BeatManager Instance { get; private set; }

    [Header("Configuration Audio")]
    [Tooltip("Piste audio de combat.")]
    [SerializeField] private AudioSource audioSource;
    
    [Tooltip("Battements par minute (BPM) de la musique de combat.")]
    [SerializeField] private float bpm = 120f;

    [Header("Décalages & Calibration")]
    [Tooltip("Offset manuel en secondes pour aligner parfaitement les événements visuels avec le son (latence audio).")]
    [SerializeField] private float audioOffsetSeconds = 0f;

    // Événements rythmiques
    public event Action<int> OnBeat;       // Appelé à chaque Beat entier (noire) avec l'index du beat
    public event Action<int> OnSubBeat;    // Appelé à chaque Sub-Beat (croche - 2 par beat)

    private double dspTimeStart;
    private bool isPlaying = false;
    private float lastBeatEvaluated = -1f;
    private float lastSubBeatEvaluated = -1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Démarre la lecture de la musique et initialise le chronomètre rythmique.
    /// </summary>
    public void StartMusic()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null || audioSource.clip == null)
        {
            Debug.LogWarning("[BeatManager] Impossible de démarrer : Aucun AudioSource ou clip audio trouvé.");
            return;
        }

        lastBeatEvaluated = -1f;
        lastSubBeatEvaluated = -1f;
        
        // Démarrage précis du temps de traitement du signal de Unity
        dspTimeStart = AudioSettings.dspTime;
        audioSource.Play();
        isPlaying = true;
        
        Debug.Log($"[BeatManager] Musique démarrée. BPM: {bpm}, DSP Start: {dspTimeStart}");
    }

    /// <summary>
    /// Arrête la musique et réinitialise le manager.
    /// </summary>
    public void StopMusic()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
        }
        isPlaying = false;
    }

    private void Update()
    {
        if (!isPlaying || audioSource == null || !audioSource.isPlaying) return;

        // Calcul du temps écoulé précis de la musique
        double currentDspTime = AudioSettings.dspTime;
        float elapsedSeconds = (float)(currentDspTime - dspTimeStart) - audioOffsetSeconds;

        // Éviter les valeurs négatives en cas d'offset
        if (elapsedSeconds < 0f) return;

        // Calcul des Beats (Battements)
        float beatInterval = 60f / bpm;
        float currentBeat = elapsedSeconds / beatInterval;
        float currentSubBeat = currentBeat * 2f; // Deux sous-battements (croches) par noire

        // Évaluation des Beats entiers (noires)
        int currentBeatInt = Mathf.FloorToInt(currentBeat);
        if (currentBeatInt > lastBeatEvaluated)
        {
            lastBeatEvaluated = currentBeatInt;
            OnBeat?.Invoke(currentBeatInt);
        }

        // Évaluation des Sub-Beats (croches)
        int currentSubBeatInt = Mathf.FloorToInt(currentSubBeat);
        if (currentSubBeatInt > lastSubBeatEvaluated)
        {
            lastSubBeatEvaluated = currentSubBeatInt;
            OnSubBeat?.Invoke(currentSubBeatInt);
        }
    }

    /// <summary>
    /// Retourne le beat actuel sous forme décimale (utile pour interpoler des animations en rythme).
    /// </summary>
    public float GetCurrentBeatDecimal()
    {
        if (!isPlaying || audioSource == null) return 0f;
        float elapsedSeconds = (float)(AudioSettings.dspTime - dspTimeStart) - audioOffsetSeconds;
        return elapsedSeconds * (bpm / 60f);
    }

    /// <summary>
    /// Permet de changer dynamiquement la musique et le BPM.
    /// </summary>
    public void SetTrack(AudioClip clip, float trackBpm)
    {
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.clip = clip;
        audioSource.loop = true;
        bpm = trackBpm;
    }

    public float Volume
    {
        get { return audioSource != null ? audioSource.volume : 1f; }
        set 
        { 
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
            }
            audioSource.volume = value; 
        }
    }

    public float Bpm => bpm;
    public bool IsPlaying => isPlaying;
}
