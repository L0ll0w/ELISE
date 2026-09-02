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
    /// Indique si le jeu est actuellement sous une pause globale ou un verrouillage de contrôle.
    /// </summary>
    public bool IsPaused => activePauseSources.Count > 0;

    /// <summary>
    /// Indique si le temps de jeu est complètement gelé (Menu de pause actif).
    /// </summary>
    public bool IsTimePaused => activePauseSources.Contains(PauseSource.Menu);

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
        bool isFullMenuPause = activePauseSources.Contains(PauseSource.Menu);
        bool isPlayerLocked = activePauseSources.Count > 0;

        // Le temps global du monde (TimeScale) est uniquement gelé lors de la pause Menu.
        // Les dialogues laissent le monde animé (TimeScale = 1) et verrouillent seulement le joueur.
        Time.timeScale = isFullMenuPause ? 0f : 1f;

        // Verrouillage / déverrouillage des commandes du joueur et du groupe
        PlayerLockManager.SetPlayerLocked(isPlayerLocked);

        if (isFullMenuPause)
        {
            Debug.Log($"[PauseManager] Jeu en PAUSE TOTALE (TimeScale = 0). Sources actives : {string.Join(", ", activePauseSources)}");
        }
        else if (isPlayerLocked)
        {
            Debug.Log($"[PauseManager] Joueur VERROUILLÉ (Monde actif, TimeScale = 1). Sources actives : {string.Join(", ", activePauseSources)}");
        }
        else
        {
            Debug.Log("[PauseManager] Jeu REPRIS et Joueur DÉVERROUILLÉ.");
        }
    }
}
