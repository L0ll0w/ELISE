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
    /// <param name="force">Si vrai, force le déverrouillage même si un combat semble actif (ex: fin officielle d'un combat).</param>
    public static void SetPlayerLocked(bool isLocked, bool hideFollowers = false, bool force = false)
    {
        bool isCombatActive = (RhythmCombatManager.Instance != null && RhythmCombatManager.Instance.IsCombatActive) ||
                              (CombatManager.Instance != null && CombatManager.Instance.IsCombatActive);

        // Si on tente de déverrouiller le joueur (ex: fin de dialogue ou fermeture de menu de pause)
        // alors qu'un combat est toujours actif, ne pas réactiver PlayerMovement ni GroupManager,
        // et ne pas écraser l'animation de combat avec "idle" !
        if (!isLocked && isCombatActive && !force)
        {
            return;
        }

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
            if (!isLocked)
            {
                Animator anim = pm.GetComponent<Animator>();
                if (anim == null) anim = pm.GetComponentInChildren<Animator>();
                if (anim != null)
                {
                    anim.Play("idle");
                    anim.SetBool("isWalking", false);
                }
            }
        }
    }
}
