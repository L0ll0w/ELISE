using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Gère le groupe de personnages (Party Manager) qui suit le joueur leader.
/// Supporte le suivi en file indienne (Trail / style Octopath) et en formation (côte à côte/devant/derrière).
/// </summary>
[AddComponentMenu("2.5D RPG/Group Manager")]
public class GroupManager : MonoBehaviour
{
    public static GroupManager Instance { get; private set; }

    public enum FollowMode
    {
        Trail,      // File indienne (Octopath Traveler style)
        Formation   // Alignement statique relatif
    }

    public enum FormationDirection
    {
        Behind,
        Left,
        Right,
        Front
    }

    [Header("Leader Cible")]
    [Tooltip("Le personnage principal à suivre. Si vide, sera recherché automatiquement (Tag 'Player' ou script PlayerMovement).")]
    [SerializeField] private Transform leader;

    [Header("Configuration du Suivi")]
    [Tooltip("Mode de suivi actuel.")]
    [SerializeField] private FollowMode followMode = FollowMode.Trail;
    [Tooltip("Direction de la formation si le mode Formation est actif.")]
    [SerializeField] private FormationDirection formationDirection = FormationDirection.Behind;
    [Tooltip("Distance (espacement) entre chaque personnage.")]
    [SerializeField] private float spacing = 1.2f;
    [Tooltip("Vitesse maximale de déplacement des compagnons.")]
    [SerializeField] private float followSpeed = 6f;
    [Tooltip("Délai de suivi (SmoothDamp) pour le mode Formation.")]
    [SerializeField] private float smoothTime = 0.25f;

    [Header("Paramètres Trail (File Indienne)")]
    [Tooltip("Distance minimale de déplacement du leader avant d'enregistrer un nouveau point dans l'historique.")]
    [SerializeField] private float minMoveDistance = 0.05f;

    [Header("Base de Données Prefabs (Pour tests/spawns)")]
    [Tooltip("Liste de prefabs de personnages de groupe disponibles.")]
    [SerializeField] private List<GameObject> characterPrefabs = new List<GameObject>();

    [Header("Débogage")]
    [Tooltip("Active les touches de test (Pavé num ou touches 1 à 8) pour tester le groupe.")]
    [SerializeField] private bool enableKeyboardTesting = true;

    // Liste des scripts followers actifs
    private List<GroupFollower> activeFollowers = new List<GroupFollower>();

    /// <summary>
    /// Liste en lecture seule des compagnons actuellement actifs dans le groupe.
    /// </summary>
    public IReadOnlyList<GroupFollower> ActiveFollowers => activeFollowers;

    /// <summary>
    /// Le Transform du leader actuel du groupe (généralement le joueur).
    /// </summary>
    public Transform Leader => leader;
    
    // Historique des positions du leader (pour le mode Trail)
    private List<Vector3> trail = new List<Vector3>();
    
    // Suivi de l'orientation/direction du leader
    private Vector3 leaderDirection = Vector3.forward;
    private Vector3 lastLeaderPosition;
    
    // Données pour le SmoothDamp (mode Formation)
    private List<Vector3> velocityBuffer = new List<Vector3>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Recherche automatique du leader si non assigné
        if (leader == null)
        {
            PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();
            if (pm != null)
            {
                leader = pm.transform;
            }
            else
            {
                GameObject playerObj = GameObject.FindWithTag("Player");
                if (playerObj != null)
                {
                    leader = playerObj.transform;
                }
            }
        }

        if (leader != null)
        {
            DontDestroyOnLoad(leader.gameObject);
            lastLeaderPosition = leader.position;
            trail.Add(leader.position);
        }
    }

    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        if (leader != null)
        {
            // Trouver s'il y a un autre joueur local présent dans la nouvelle scène
            PlayerMovement[] allPlayers = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
            foreach (var p in allPlayers)
            {
                // Si c'est un autre GameObject de joueur
                if (p.gameObject != leader.gameObject)
                {
                    Debug.Log($"Joueur local trouvé dans la scène chargée : '{p.gameObject.name}'. On le remplace par le joueur persistant.");
                    
                    // Téléporter le joueur persistant sur le joueur local de la scène
                    leader.position = p.transform.position;
                    leader.rotation = p.transform.rotation;
                    
                    // Détruire le joueur local de la scène pour éviter le doublon
                    Destroy(p.gameObject);
                    break;
                }
            }

            // Réinitialiser le trail à la nouvelle position du leader
            trail.Clear();
            trail.Add(leader.position);
            lastLeaderPosition = leader.position;

            // Replacer les compagnons sur le joueur et réappliquer les collisions ignorées
            TeleportPartyToLeader();
            ReapplyAllCollisions();
        }
    }

    /// <summary>
    /// Réapplique les ignores de collisions physiques entre tous les membres actifs du groupe.
    /// </summary>
    public void ReapplyAllCollisions()
    {
        foreach (var follower in activeFollowers)
        {
            if (follower != null)
            {
                IgnoreCollisionsFor(follower.gameObject);
            }
        }
    }

    private void Update()
    {
        if (leader == null) return;

        // 1. Mise à jour de la direction du leader
        Vector3 leaderMovement = leader.position - lastLeaderPosition;
        if (leaderMovement.sqrMagnitude > 0.0001f)
        {
            // On projette sur le plan XZ (2.5D) pour éviter des inclinaisons verticales bizarres
            Vector3 planMovement = new Vector3(leaderMovement.x, 0f, leaderMovement.z);
            if (planMovement.sqrMagnitude > 0.0001f)
            {
                leaderDirection = planMovement.normalized;
            }
        }

        // 2. Enregistrement de l'historique du chemin (Trail)
        if (Vector3.Distance(leader.position, trail[0]) > minMoveDistance)
        {
            // On insère au début de la liste pour que trail[0] soit le point le plus récent après la position actuelle
            trail.Insert(0, leader.position);
            
            // Nettoyage de l'historique pour ne pas saturer la mémoire
            // On n'a besoin du trail que pour couvrir la distance totale du groupe
            float maxTrailLength = activeFollowers.Count * spacing + 2f;
            TrimTrail(maxTrailLength);
        }

        // 3. Déplacement des compagnons
        UpdateFollowersPosition();

        lastLeaderPosition = leader.position;

        // 4. Gestion des touches de débogage si activé
        if (enableKeyboardTesting)
        {
            HandleDebugInputs();
        }
    }

    /// <summary>
    /// Met à jour les positions cibles de tous les compagnons selon le mode choisi.
    /// </summary>
    private void UpdateFollowersPosition()
    {
        // S'assurer que le buffer de vélocité est de la bonne taille pour le SmoothDamp
        while (velocityBuffer.Count < activeFollowers.Count)
        {
            velocityBuffer.Add(Vector3.zero);
        }

        for (int i = 0; i < activeFollowers.Count; i++)
        {
            Vector3 targetPos = Vector3.zero;
            float targetDistance = (i + 1) * spacing;

            if (followMode == FollowMode.Trail)
            {
                // Mode File Indienne (Octopath Traveler) : suit l'historique du chemin
                Vector3 moveDir;
                targetPos = GetPositionAlongTrail(targetDistance, out moveDir);
                
                // Déplacement direct / interpolé fluide vers le point de la piste
                activeFollowers[i].MoveTo(targetPos, followSpeed);
            }
            else
            {
                // Mode Formation : position géométrique relative au leader
                Vector3 offsetDir = GetFormationOffsetDirection(formationDirection);
                targetPos = leader.position + (offsetDir * targetDistance);

                // Déplacement SmoothDamp pour simuler le délai/inertie dans les actions de suivi
                Vector3 currentVelocity = velocityBuffer[i];
                Vector3 newPos = Vector3.SmoothDamp(
                    activeFollowers[i].transform.position, 
                    targetPos, 
                    ref currentVelocity, 
                    smoothTime, 
                    followSpeed
                );
                velocityBuffer[i] = currentVelocity;

                activeFollowers[i].MoveTo(newPos, followSpeed);
            }
        }
    }

    /// <summary>
    /// Calcule la position le long de la piste enregistrée pour une distance donnée.
    /// </summary>
    private Vector3 GetPositionAlongTrail(float targetDistance, out Vector3 direction)
    {
        direction = leaderDirection;
        if (trail.Count == 0) return leader.position;

        float accumulatedDistance = 0f;
        Vector3 prevPoint = leader.position;

        for (int i = 0; i < trail.Count; i++)
        {
            Vector3 currPoint = trail[i];
            float segmentLength = Vector3.Distance(prevPoint, currPoint);

            if (accumulatedDistance + segmentLength >= targetDistance)
            {
                float t = (targetDistance - accumulatedDistance) / Mathf.Max(segmentLength, 0.0001f);
                direction = (currPoint - prevPoint).normalized;
                return Vector3.Lerp(prevPoint, currPoint, t);
            }

            accumulatedDistance += segmentLength;
            prevPoint = currPoint;
        }

        // Si on dépasse le chemin enregistré, on prend le dernier point
        if (trail.Count > 0)
        {
            direction = (trail[trail.Count - 1] - prevPoint).normalized;
            return trail[trail.Count - 1];
        }

        return leader.position;
    }

    /// <summary>
    /// Retourne la direction vectorielle de la formation selon le choix.
    /// </summary>
    private Vector3 GetFormationOffsetDirection(FormationDirection direction)
    {
        Vector3 leaderRight = Vector3.Cross(Vector3.up, leaderDirection).normalized;

        switch (direction)
        {
            case FormationDirection.Left:
                return -leaderRight;
            case FormationDirection.Right:
                return leaderRight;
            case FormationDirection.Front:
                return leaderDirection;
            case FormationDirection.Behind:
            default:
                return -leaderDirection;
        }
    }

    /// <summary>
    /// Coupe la liste des positions historiques pour ne conserver que la longueur nécessaire.
    /// </summary>
    private void TrimTrail(float maxLength)
    {
        if (trail.Count < 2) return;

        float accumulatedDistance = 0f;
        Vector3 prevPoint = leader.position;

        for (int i = 0; i < trail.Count; i++)
        {
            accumulatedDistance += Vector3.Distance(prevPoint, trail[i]);
            prevPoint = trail[i];

            if (accumulatedDistance > maxLength)
            {
                // Supprime tous les points au-delà de la longueur requise
                if (i < trail.Count - 1)
                {
                    trail.RemoveRange(i + 1, trail.Count - (i + 1));
                }
                break;
            }
        }
    }

    #region API Publique de Gestion du Groupe

    /// <summary>
    /// Ajoute un membre au groupe à partir d'un Prefab de personnage.
    /// </summary>
    public GameObject AddMember(GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogError("Impossible d'ajouter un membre : le Prefab fourni est nul.");
            return null;
        }

        // Position de spawn initiale sur le leader
        Vector3 spawnPos = leader != null ? leader.position : transform.position;

        GameObject followerGo = Instantiate(prefab, spawnPos, Quaternion.identity);
        DontDestroyOnLoad(followerGo); // Rendre le compagnon persistant entre les scènes
        GroupFollower follower = followerGo.GetComponent<GroupFollower>();
        
        if (follower == null)
        {
            follower = followerGo.AddComponent<GroupFollower>();
        }

        // Gérer les collisions pour éviter que les membres ne se bloquent entre eux
        IgnoreCollisionsFor(followerGo);

        // Ajout à la liste
        activeFollowers.Add(follower);
        velocityBuffer.Add(Vector3.zero);

        // Téléporter le membre à sa position cible immédiate si possible
        float targetDistance = activeFollowers.Count * spacing;
        Vector3 targetDir;
        Vector3 initialPos = GetPositionAlongTrail(targetDistance, out targetDir);
        follower.TeleportTo(initialPos);

        Debug.Log($"Membre '{prefab.name}' ajouté au groupe. Taille actuelle : {activeFollowers.Count}");
        return followerGo;
    }

    /// <summary>
    /// Ajoute un membre au groupe via son index dans la liste des Prefabs de test.
    /// </summary>
    public void AddMemberByIndex(int index)
    {
        if (index >= 0 && index < characterPrefabs.Count)
        {
            AddMember(characterPrefabs[index]);
        }
        else
        {
            Debug.LogWarning($"Index de prefab '{index}' invalide ou base de prefabs vide.");
        }
    }

    /// <summary>
    /// Retire un membre du groupe en détruisant son GameObject.
    /// </summary>
    public void RemoveMember(GameObject memberGo)
    {
        if (memberGo == null) return;

        GroupFollower follower = memberGo.GetComponent<GroupFollower>();
        if (follower != null && activeFollowers.Contains(follower))
        {
            int index = activeFollowers.IndexOf(follower);
            activeFollowers.RemoveAt(index);
            if (index < velocityBuffer.Count) velocityBuffer.RemoveAt(index);

            Destroy(memberGo);
            Debug.Log($"Membre retiré du groupe. Taille actuelle : {activeFollowers.Count}");
        }
    }

    /// <summary>
    /// Retire le membre à l'index donné dans la liste active du groupe.
    /// </summary>
    public void RemoveMemberAt(int index)
    {
        if (index >= 0 && index < activeFollowers.Count)
        {
            RemoveMember(activeFollowers[index].gameObject);
        }
    }

    /// <summary>
    /// Vide entièrement le groupe de compagnons.
    /// </summary>
    public void ClearGroup()
    {
        foreach (var follower in activeFollowers)
        {
            if (follower != null)
            {
                Destroy(follower.gameObject);
            }
        }
        activeFollowers.Clear();
        velocityBuffer.Clear();
        trail.Clear();
        if (leader != null) trail.Add(leader.position);
        Debug.Log("Groupe entièrement vidé.");
    }

    /// <summary>
    /// Téléporte instantanément tous les membres sur leurs positions cibles (ex: après un chargement).
    /// </summary>
    public void TeleportPartyToLeader()
    {
        if (leader == null) return;

        trail.Clear();
        trail.Add(leader.position);

        for (int i = 0; i < activeFollowers.Count; i++)
        {
            float targetDistance = (i + 1) * spacing;
            Vector3 targetPos;

            if (followMode == FollowMode.Trail)
            {
                Vector3 dir;
                targetPos = GetPositionAlongTrail(targetDistance, out dir);
            }
            else
            {
                Vector3 offsetDir = GetFormationOffsetDirection(formationDirection);
                targetPos = leader.position + (offsetDir * targetDistance);
            }

            activeFollowers[i].TeleportTo(targetPos);
            velocityBuffer[i] = Vector3.zero;
        }
    }

    /// <summary>
    /// Définit un nouveau leader pour le groupe (ex: après remplacement de prefab).
    /// </summary>
    public void SetLeader(Transform newLeader)
    {
        leader = newLeader;
        if (leader != null)
        {
            DontDestroyOnLoad(leader.gameObject);
            lastLeaderPosition = leader.position;
            trail.Clear();
            trail.Add(leader.position);

            // Replacer les compagnons sur le joueur et réappliquer les collisions
            TeleportPartyToLeader();
            ReapplyAllCollisions();
        }
    }

    /// <summary>
    /// Permet de configurer à la volée la formation et le mode de suivi.
    /// </summary>
    public void ChangeFormation(FollowMode mode, FormationDirection direction)
    {
        followMode = mode;
        formationDirection = direction;
        Debug.Log($"Formation modifiée. Mode : {followMode}, Direction : {formationDirection}");
    }

    #endregion

    /// <summary>
    /// Désactive les collisions mutuelles entre le leader et les compagnons pour éviter les frictions.
    /// </summary>
    private void IgnoreCollisionsFor(GameObject newMember)
    {
        Collider newCollider = newMember.GetComponent<Collider>();
        if (newCollider == null) return;

        // Ignorer avec le leader
        if (leader != null)
        {
            Collider leaderCollider = leader.GetComponent<Collider>();
            if (leaderCollider != null)
            {
                Physics.IgnoreCollision(newCollider, leaderCollider, true);
            }
        }

        // Ignorer avec les autres followers existants
        foreach (var follower in activeFollowers)
        {
            if (follower != null)
            {
                Collider otherCollider = follower.GetComponent<Collider>();
                if (otherCollider != null)
                {
                    Physics.IgnoreCollision(newCollider, otherCollider, true);
                }
            }
        }
    }

    /// <summary>
    /// Raccourcis clavier pour tester le système facilement en jeu.
    /// </summary>
    private void HandleDebugInputs()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        // Touche 1 : Ajouter le prochain personnage de la liste (dans l'ordre)
        if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame)
        {
            if (characterPrefabs.Count > 0)
            {
                int nextIndex = activeFollowers.Count % characterPrefabs.Count;
                AddMemberByIndex(nextIndex);
            }
            else
            {
                Debug.LogWarning("Aucun prefab de personnage configuré dans la liste 'Character Prefabs'.");
            }
        }

        // Touche 3 : Retirer le dernier membre
        if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame)
        {
            if (activeFollowers.Count > 0)
            {
                RemoveMemberAt(activeFollowers.Count - 1);
            }
        }

        // Touche 4 : Passer en mode File Indienne (Trail / Octopath)
        if (keyboard.digit4Key.wasPressedThisFrame || keyboard.numpad4Key.wasPressedThisFrame)
        {
            ChangeFormation(FollowMode.Trail, FormationDirection.Behind);
        }

        // Touche 5 : Formation Derrière
        if (keyboard.digit5Key.wasPressedThisFrame || keyboard.numpad5Key.wasPressedThisFrame)
        {
            ChangeFormation(FollowMode.Formation, FormationDirection.Behind);
        }

        // Touche 6 : Formation Gauche
        if (keyboard.digit6Key.wasPressedThisFrame || keyboard.numpad6Key.wasPressedThisFrame)
        {
            ChangeFormation(FollowMode.Formation, FormationDirection.Left);
        }

        // Touche 7 : Formation Droite
        if (keyboard.digit7Key.wasPressedThisFrame || keyboard.numpad7Key.wasPressedThisFrame)
        {
            ChangeFormation(FollowMode.Formation, FormationDirection.Right);
        }

        // Touche 8 : Formation Devant
        if (keyboard.digit8Key.wasPressedThisFrame || keyboard.numpad8Key.wasPressedThisFrame)
        {
            ChangeFormation(FollowMode.Formation, FormationDirection.Front);
        }
    }
}
