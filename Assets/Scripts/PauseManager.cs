using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gère la mise en pause du jeu de façon centralisée afin que différents systèmes
/// (Menu, Dialogues) ne s'annulent pas mutuellement.
/// </summary>
[AddComponentMenu("2.5D RPG/Pause Manager")]
public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }

    public enum PauseSource
    {
        Menu,
        Dialogue
    }

    private HashSet<PauseSource> activePauseSources = new HashSet<PauseSource>();
    private PlayerMovement cachedPlayerMovement;

    /// <summary>
    /// Indique si le jeu est actuellement en pause par au moins une source.
    /// </summary>
    public bool IsPaused => activePauseSources.Count > 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // Permet au PauseManager de persister d'une scène à l'autre
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Demande la mise en pause du jeu pour une source donnée.
    /// </summary>
    public void RequestPause(PauseSource source)
    {
        activePauseSources.Add(source);
        ApplyPauseState();
    }

    /// <summary>
    /// Demande la reprise du jeu pour une source donnée.
    /// </summary>
    public void RequestUnpause(PauseSource source)
    {
        activePauseSources.Remove(source);
        ApplyPauseState();
    }

    /// <summary>
    /// Force la réinitialisation de tous les états de pause (utile lors de retours au menu principal).
    /// </summary>
    public void ClearAllPauses()
    {
        activePauseSources.Clear();
        ApplyPauseState();
    }

    /// <summary>
    /// Applique les effets physiques et logiques de la pause.
    /// </summary>
    private void ApplyPauseState()
    {
        if (IsPaused)
        {
            Time.timeScale = 0f;
            SetPlayerMovementEnabled(false);
            Debug.Log($"Jeu mis en PAUSE. Sources actives : {string.Join(", ", activePauseSources)}");
        }
        else
        {
            Time.timeScale = 1f;
            SetPlayerMovementEnabled(true);
            Debug.Log("Jeu REPRIS. Plus aucune source de pause active.");
        }
    }

    /// <summary>
    /// Active ou désactive le script de déplacement du joueur.
    /// </summary>
    private void SetPlayerMovementEnabled(bool enabled)
    {
        if (cachedPlayerMovement == null)
        {
            cachedPlayerMovement = FindFirstObjectByType<PlayerMovement>();
        }

        if (cachedPlayerMovement != null)
        {
            cachedPlayerMovement.enabled = enabled;
        }
    }
}
