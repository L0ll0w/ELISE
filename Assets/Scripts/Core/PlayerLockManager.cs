using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Gestionnaire centralisé du gel des mouvements du joueur, du groupe et de la caméra.
/// Évite la duplication répétée de code d'inhibition des composants lors des cinématiques, dialogues et combats.
/// </summary>
public static class PlayerLockManager
{
    /// <summary>
    /// Verrouille ou déverrouille les déplacements du joueur principal et de son groupe.
    /// </summary>
    /// <param name="isLocked">Vrai pour geler le joueur, faux pour lui rendre les commandes.</param>
    /// <param name="hideFollowers">Si vrai, masque les membres du groupe pendant le gel (ex: au début d'un combat).</param>
    public static void SetPlayerLocked(bool isLocked, bool hideFollowers = false)
    {
        // 1. Gestion du groupe (GroupManager)
        if (GroupManager.Instance != null)
        {
            GroupManager.Instance.enabled = !isLocked;
            foreach (var follower in GroupManager.Instance.ActiveFollowers)
            {
                if (follower != null)
                {
                    if (hideFollowers && isLocked)
                    {
                        follower.gameObject.SetActive(false);
                    }
                    else
                    {
                        follower.enabled = !isLocked;
                        if (!isLocked) follower.gameObject.SetActive(true);
                    }
                }
            }

            if (!isLocked)
            {
                GroupManager.Instance.TeleportPartyToLeader();
                GroupManager.Instance.ReapplyAllCollisions();
            }
        }

        // 2. Gestion du leader / joueur solo (PlayerMovement)
        PlayerMovement pm = Object.FindFirstObjectByType<PlayerMovement>();
        if (pm != null)
        {
            pm.enabled = !isLocked;
        }
    }
}
