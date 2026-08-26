using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Cinemachine;
using UnityEngine.InputSystem;

/// <summary>
/// Gestionnaire principal du système de combat rythmique radial.
/// Gère la boucle de combat, la génération des attaques ennemies en rythme,
/// la détection des dégâts, les attaques du joueur sur le beat, et le système de Tag-Team.
/// </summary>
[AddComponentMenu("2.5D RPG/Rhythm/Rhythm Combat Manager")]
public class RhythmCombatManager : MonoBehaviour
{
    public static RhythmCombatManager Instance { get; private set; }

    public enum CombatState { Transitioning, Active, Victory, Defeat }

    [Header("Configuration Rythmique")]
    [Tooltip("Musique de combat (Clip audio).")]
    [SerializeField] private AudioClip combatMusicClip;
    [Tooltip("BPM de la musique de combat.")]
    [SerializeField] private float musicBpm = 120f;

    [Header("Configuration de la Grille")]
    [Tooltip("Prefab ou composant de la grille circulaire.")]
    [SerializeField] private RadialCombatGrid radialGrid;

    [Header("Configuration Ennemi par Défaut (Fallback)")]
    [SerializeField] private EnemyCombatData defaultCombatData;

    [Header("Paramètres du Joueur")]
    [Tooltip("Prefab de particules lors d'une attaque réussie sur le beat.")]
    [SerializeField] private ParticleSystem attackSuccessParticles;
    [Tooltip("Prefab de particules quand le joueur se fait toucher.")]
    [SerializeField] private ParticleSystem hitParticles;

    [Header("Animations du Joueur")]
    [Tooltip("Le nom de l'état d'animation à jouer sur l'Animator du joueur pendant le combat rythmique (ex: dance).")]
    [SerializeField] private string combatAnimationStateName = "dance";

    [Header("Configuration Caméra")]
    [Tooltip("Distance de la caméra par rapport au boss.")]
    [SerializeField] private float cameraDistance = 10f;
    [Tooltip("Hauteur de la caméra au-dessus de la grille.")]
    [SerializeField] private float cameraHeight = 6f;
    [Tooltip("Inclinaison (Pitch) de la caméra.")]
    [SerializeField] private float cameraPitch = 30f;

    // État du combat
    private CombatState currentState = CombatState.Transitioning;
    private GameObject activeEnemy;
    private RhythmPlayerController playerController;
    private EnemyCombatData activeCombatData;
    private GameObject activeVisualPrefab;
    private CinemachineBrain brain;

    // Gestion du Groupe et PV (Tag-Team)
    private List<Transform> allies = new List<Transform>();
    private List<int> allyHP = new List<int>();
    private List<int> allyMaxHP = new List<int>();
    private List<Sprite> allyOriginalSprites = new List<Sprite>();
    private List<RuntimeAnimatorController> allyOriginalAnimators = new List<RuntimeAnimatorController>();
    private int activeAllyIndex = 0;
    
    private int enemyHP;
    private int enemyMaxHP = 300;

    // Références Caméra
    private CinemachineCamera virtualCamera;
    private CinemachineHelper cameraHelper;

    // Références UI (Gérées dynamiquement)
    private Canvas combatCanvas;
    private CanvasGroup fadeCanvasGroup;
    private TextMeshProUGUI logText;
    private TextMeshProUGUI comboFeedbackText;
    private Image bossHPImage;
    private TextMeshProUGUI bossNameText;
    private List<Image> allyHPImages = new List<Image>();
    private List<TextMeshProUGUI> allyHPTexts = new List<TextMeshProUGUI>();
    private GameObject tagPromptPanel;
    private Sprite uiFillSprite;

    // Système d'Attaque Ennemie (Telegraphs)
    private Dictionary<string, int> activeTelegraphs = new Dictionary<string, int>(); // Clé: ring_sector, Valeur: beat à laquelle l'attaque frappe
    private HashSet<string> groundOnlyTelegraphs = new HashSet<string>(); // Clés des attaques au sol esquivables par le saut

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
        CreateFadeCanvas();
    }

    private void Update()
    {
        if (currentState != CombatState.Active) return;

        // Suivi de caméra fluide derrière le joueur et orientation du boss
        UpdateCameraView(false);
        OrientBossTowardsPlayer();

        bool attackPressed = false;
        bool tagPressed = false;

        // Lecture des entrées clavier via Input System
        if (Keyboard.current != null)
        {
            // Clic gauche pour attaquer (Espace étant utilisé pour sauter)
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) attackPressed = true;
            if (Keyboard.current.tabKey.wasPressedThisFrame || Keyboard.current.leftShiftKey.wasPressedThisFrame) tagPressed = true;
        }

        // Lecture des entrées manette (Optionnel)
        if (Gamepad.current != null)
        {
            // Bouton Ouest (touche X sur Xbox) pour attaquer (bouton A/Sud étant utilisé pour sauter)
            if (Gamepad.current.buttonWest.wasPressedThisFrame) attackPressed = true;
            if (Gamepad.current.buttonNorth.wasPressedThisFrame) tagPressed = true;
        }

        // Détecter l'attaque du joueur sur le Beat (Touche Espace ou bouton Sud)
        if (attackPressed)
        {
            EvaluatePlayerAttack();
        }

        // Détecter le changement de personnage (Touche Tab / Shift ou bouton Nord)
        if (tagPressed)
        {
            TagNextCharacter();
        }
    }

    #region Lancement du Combat Rythmique

    public void StartCombat(GameObject enemy)
    {
        if (enemy == null)
        {
            Debug.LogError("[RhythmCombatManager] Impossible de lancer le combat car l'ennemi est nul.");
            return;
        }

        activeEnemy = enemy;

        // Récupérer le conteneur de données de combat s'il existe
        EnemyCombatDataHolder holder = enemy.GetComponent<EnemyCombatDataHolder>();
        activeCombatData = holder != null ? holder.CombatData : defaultCombatData;

        StartCoroutine(StartCombatRoutine());
    }

    private IEnumerator StartCombatRoutine()
    {
        currentState = CombatState.Transitioning;
        Debug.Log("[RhythmCombatManager] Initialisation du combat rythmique radial...");

        // 1. Fondu au noir
        yield return StartCoroutine(Fade(1f));

        // 2. Geler le joueur et désactiver le suivi de groupe
        Transform leader = null;
        if (GroupManager.Instance != null)
        {
            leader = GroupManager.Instance.Leader;
            GroupManager.Instance.enabled = false;
            foreach (var follower in GroupManager.Instance.ActiveFollowers)
            {
                if (follower != null) follower.gameObject.SetActive(false); // Cacher les compagnons
            }
        }
        else
        {
            PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();
            if (pm != null) leader = pm.transform;
        }

        if (leader != null)
        {
            PlayerMovement pm = leader.GetComponent<PlayerMovement>();
            if (pm != null) pm.enabled = false;
        }

        // Désactiver Cinemachine (Cerveau de la caméra principale + Caméra virtuelle)
        brain = Camera.main != null ? Camera.main.GetComponent<CinemachineBrain>() : null;
        if (brain != null)
        {
            brain.enabled = false; // Bloquer Cinemachine pour pouvoir modifier directement Camera.main
        }

        virtualCamera = FindFirstObjectByType<CinemachineCamera>();
        if (virtualCamera != null)
        {
            virtualCamera.enabled = false;
            cameraHelper = virtualCamera.GetComponent<CinemachineHelper>();
            if (cameraHelper != null)
            {
                cameraHelper.SaveOriginalSettings();
                cameraHelper.enabled = false;
            }
        }

        // 3. Configurer le Groupe (PV et Sauvegarde des Visuels originaux)
        allies.Clear();
        allyHP.Clear();
        allyMaxHP.Clear();
        allyOriginalSprites.Clear();
        allyOriginalAnimators.Clear();

        if (leader != null) allies.Add(leader);
        if (GroupManager.Instance != null)
        {
            foreach (var follower in GroupManager.Instance.ActiveFollowers)
            {
                if (follower != null) allies.Add(follower.transform);
            }
        }

        activeAllyIndex = 0;
        for (int i = 0; i < allies.Count; i++)
        {
            allyHP.Add(100);
            allyMaxHP.Add(100);

            // Récupérer et sauvegarder le sprite renderer / controller de chaque allié
            SpriteRenderer sr = allies[i].GetComponentInChildren<SpriteRenderer>();
            Animator anim = allies[i].GetComponentInChildren<Animator>();
            
            allyOriginalSprites.Add(sr != null ? sr.sprite : null);
            allyOriginalAnimators.Add(anim != null ? anim.runtimeAnimatorController : null);
        }

        // Configurer le boss à partir des données ou utiliser les valeurs par défaut
        string enemyName = "Boss";
        int maxHP = 300;
        AudioClip musicClip = combatMusicClip;
        float musicBPM = musicBpm;

        if (activeCombatData != null)
        {
            enemyName = activeCombatData.EnemyName;
            maxHP = activeCombatData.MaxHP;
            musicClip = activeCombatData.MusicTrack != null ? activeCombatData.MusicTrack : combatMusicClip;
            musicBPM = activeCombatData.Bpm;
        }

        enemyMaxHP = maxHP;
        enemyHP = enemyMaxHP;

        // Si un préfabriqué visuel est fourni par les données de l'ennemi, l'instancier
        if (activeCombatData != null && activeCombatData.VisualPrefab != null)
        {
            // Masquer les visuels d'exploration
            SpriteRenderer[] explorationSprites = activeEnemy.GetComponentsInChildren<SpriteRenderer>();
            foreach (var sprite in explorationSprites)
            {
                sprite.enabled = false;
            }

            // Instancier le visuel de combat au centre
            activeVisualPrefab = Instantiate(activeCombatData.VisualPrefab, activeEnemy.transform);
            activeVisualPrefab.transform.localPosition = Vector3.zero;
            activeVisualPrefab.transform.localRotation = Quaternion.identity;
        }

        // 4. Positionner la grille sous l'ennemi
        Vector3 combatCenter = activeEnemy.transform.position;
        // Aligner l'ennemi au sol au centre
        activeEnemy.transform.position = SnapToGround(combatCenter);

        if (radialGrid == null)
        {
            radialGrid = FindFirstObjectByType<RadialCombatGrid>();
        }

        if (radialGrid == null)
        {
            GameObject gridObj = new GameObject("RadialCombatGrid");
            radialGrid = gridObj.AddComponent<RadialCombatGrid>();
        }
        radialGrid.transform.position = activeEnemy.transform.position;
        radialGrid.SetGridActive(true);

        // 5. Calculer le secteur de départ le plus proche de la position actuelle du joueur face au boss
        Vector3 dirToPlayer = (leader.position - activeEnemy.transform.position).normalized;
        float angleRad = Mathf.Atan2(dirToPlayer.z, dirToPlayer.x);
        float angleDeg = angleRad * Mathf.Rad2Deg;
        if (angleDeg < 0f) angleDeg += 360f;
        int startSector = Mathf.RoundToInt((angleDeg - 22.5f) / 45f) % 8;
        startSector = (startSector + 8) % 8;

        playerController = leader.gameObject.GetComponent<RhythmPlayerController>();
        if (playerController == null)
        {
            playerController = leader.gameObject.AddComponent<RhythmPlayerController>();
        }
        playerController.Initialize(radialGrid, 0, startSector);
        playerController.SetInputEnabled(true);

        // Jouer l'animation de combat (danse) sur l'Animator du joueur
        if (!string.IsNullOrEmpty(combatAnimationStateName))
        {
            Animator anim = playerController.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                Debug.Log($"[RhythmCombatManager] Lecture de l'état d'animation de combat '{combatAnimationStateName}' sur '{anim.gameObject.name}'.");
                anim.Play(combatAnimationStateName);
            }
        }

        // 6. Orienter la caméra derrière le joueur vers le boss (instantanément au début)
        UpdateCameraView(true);

        // 7. Initialiser l'UI et la musique
        CreateCombatUI();
        UpdateUI();

        if (BeatManager.Instance == null)
        {
            GameObject bmObj = new GameObject("BeatManager");
            bmObj.AddComponent<BeatManager>();
        }
        
        BeatManager.Instance.SetTrack(musicClip, musicBPM);
        BeatManager.Instance.StartMusic();

        // S'abonner aux battements
        BeatManager.Instance.OnBeat += ProcessEnemyAttackBeat;

        // Attendre la stabilisation
        yield return new WaitForSeconds(0.2f);

        // 8. Fondu de retour (Fade In)
        yield return StartCoroutine(Fade(0f));

        currentState = CombatState.Active;
        logText.text = "ESQUIVEZ EN RYTHME ! Appuyez sur ESPACE sur le Beat pour attaquer !";
    }

    #endregion

    #region Boucle de Combat & Attaques de l'Ennemi (Dodge Phase)

    private void ProcessEnemyAttackBeat(int beatIndex)
    {
        if (currentState != CombatState.Active) return;

        // A. Évaluer et appliquer les dégâts des alertes qui devaient frapper à ce beat
        ApplyTelegraphDamage(beatIndex);

        // B. Générer de nouveaux patterns d'attaque en déléguant au ScriptableObject du boss
        if (activeCombatData != null && activeCombatData.BeatPattern != null)
        {
            activeCombatData.BeatPattern.ProcessBeat(beatIndex, this, radialGrid, playerController);
        }
        else
        {
            GenerateAttackPattern(beatIndex); // Fallback standard
        }
    }

    private void ApplyTelegraphDamage(int currentBeat)
    {
        List<string> resolvedKeys = new List<string>();

        foreach (var pair in activeTelegraphs)
        {
            if (pair.Value <= currentBeat)
            {
                resolvedKeys.Add(pair.Key);
                
                // Extraire ring et sector
                string[] parts = pair.Key.Split('_');
                int ring = int.Parse(parts[0]);
                int sector = int.Parse(parts[1]);

                // Effacer l'alerte visuelle sur la grille
                radialGrid.SetCellWarning(ring, sector, false);

                bool isGroundOnly = groundOnlyTelegraphs.Contains(pair.Key);

                // Vérifier si le joueur se trouve sur cette case au moment de l'impact
                if (playerController.CurrentRing == ring && playerController.CurrentSector == sector)
                {
                    if (!playerController.IsInvincible)
                    {
                        // Si c'est une attaque au sol, et que le joueur est en train de SAUTER, on ignore les dégâts
                        if (isGroundOnly && playerController.IsJumping)
                        {
                            Debug.Log("[RhythmCombatManager] Le joueur esquive l'attaque au sol grâce au Saut ! Dégâts évités.");
                        }
                        else
                        {
                            TakeDamage(25); // Dégâts standard
                        }
                    }
                }
            }
        }

        // Nettoyer les alertes résolues
        foreach (var key in resolvedKeys)
        {
            activeTelegraphs.Remove(key);
            groundOnlyTelegraphs.Remove(key);
        }
    }

    private void GenerateAttackPattern(int currentBeat)
    {
        // On prévient 1 beat à l'avance (Telegraph à beat N, dégâts à beat N+1)
        int targetImpactBeat = currentBeat + 1;

        // Génération de patterns simples alternés
        int patternIndex = currentBeat % 8;

        switch (patternIndex)
        {
            case 0:
                // 1. Attaque sur tout le cercle intérieur (Ring 0)
                TelegraphEntireRing(0, targetImpactBeat);
                break;
            case 2:
                // 2. Attaque sur tout le cercle extérieur (Ring 1)
                TelegraphEntireRing(1, targetImpactBeat);
                break;
            case 4:
                // 3. Attaque sur les secteurs pairs (0, 2, 4, 6)
                for (int s = 0; s < radialGrid.SectorsCount; s += 2)
                {
                    TelegraphCell(0, s, targetImpactBeat);
                    TelegraphCell(1, s, targetImpactBeat);
                }
                break;
            case 6:
                // 4. Attaque sur les secteurs impairs (1, 3, 5, 7)
                for (int s = 1; s < radialGrid.SectorsCount; s += 2)
                {
                    TelegraphCell(0, s, targetImpactBeat);
                    TelegraphCell(1, s, targetImpactBeat);
                }
                break;
            case 7:
                // 5. Attaque ciblée sur le secteur actuel du joueur
                TelegraphCell(0, playerController.CurrentSector, targetImpactBeat);
                TelegraphCell(1, playerController.CurrentSector, targetImpactBeat);
                break;
        }
    }

    public void TelegraphCell(int ring, int sector, int impactBeat, bool isGroundOnly = false)
    {
        string key = $"{ring}_{sector}";
        if (!activeTelegraphs.ContainsKey(key))
        {
            activeTelegraphs.Add(key, impactBeat);
            
            if (isGroundOnly)
            {
                groundOnlyTelegraphs.Add(key);
                // Couleur d'alerte Orange pour les attaques au sol esquivables en sautant
                Color warningOrange = new Color(1.0f, 0.5f, 0.0f, 0.6f);
                radialGrid.SetCellWarning(ring, sector, true, warningOrange);
            }
            else
            {
                radialGrid.SetCellWarning(ring, sector, true);
            }

            // Déclencher le projectile visuel spécifique à cet ennemi
            GameObject projectilePrefab = activeCombatData != null ? activeCombatData.WarningProjectilePrefab : null;
            GameObject impactPrefab = activeCombatData != null ? activeCombatData.ImpactVisualPrefab : null;

            if (projectilePrefab != null && BeatManager.Instance != null)
            {
                float currentBeatDec = BeatManager.Instance.GetCurrentBeatDecimal();
                float beatsRemaining = impactBeat - currentBeatDec;
                // Calculer le temps restant en secondes d'après le BPM de la musique
                float timeToImpact = Mathf.Max(0.05f, beatsRemaining * (60f / musicBpm));

                Vector3 targetPos = radialGrid.GetCellPosition(ring, sector);
                targetPos.y = radialGrid.transform.position.y + 0.02f; // S'aligner sur la hauteur de la grille

                Vector3 startPos = targetPos + Vector3.up * 8f; // Le projectile commence 8 mètres au-dessus de sa cible

                GameObject dropObj = Instantiate(projectilePrefab, startPos, Quaternion.identity);
                dropObj.SetActive(true); // S'assurer que le projectile est actif (au cas où le prefab d'origine était désactivé)
                
                FallingProjectile proj = dropObj.GetComponent<FallingProjectile>();
                if (proj == null) proj = dropObj.AddComponent<FallingProjectile>();
                
                proj.Initialize(startPos, targetPos, timeToImpact, impactPrefab);
            }
            else if (projectilePrefab == null)
            {
                Debug.LogWarning($"[RhythmCombatManager] Le prefab de projectile est null pour l'ennemi {(activeCombatData != null ? activeCombatData.EnemyName : "Inconnu")}. Glissez votre prefab d'encre dans l'EnemyCombatData.");
            }
        }
    }

    public void TelegraphEntireRing(int ringIndex, int impactBeat)
    {
        for (int s = 0; s < radialGrid.SectorsCount; s++)
        {
            TelegraphCell(ringIndex, s, impactBeat);
        }
    }

    #endregion

    #region Action du Joueur : Attaque sur le Beat

    private void EvaluatePlayerAttack()
    {
        float currentBeatDec = BeatManager.Instance.GetCurrentBeatDecimal();
        float closestBeat = Mathf.Round(currentBeatDec);
        float diff = Mathf.Abs(currentBeatDec - closestBeat);

        // Seuil de précision rythmique
        float perfectThreshold = 0.12f;
        float goodThreshold = 0.25f;

        if (diff <= perfectThreshold)
        {
            TriggerAttackCombo("PERFECT !", 25, Color.green);
        }
        else if (diff <= goodThreshold)
        {
            TriggerAttackCombo("BIEN !", 12, Color.yellow);
        }
        else
        {
            TriggerAttackCombo("RATE !", 0, Color.red);
        }
    }

    private void TriggerAttackCombo(string rating, int damage, Color color)
    {
        // Pop-up text
        comboFeedbackText.text = rating;
        comboFeedbackText.color = color;
        StartCoroutine(AnimateComboText());

        if (damage > 0)
        {
            enemyHP = Mathf.Max(0, enemyHP - damage);
            UpdateUI();

            // Particules de succès
            if (attackSuccessParticles != null && playerController != null)
            {
                ParticleSystem ps = Instantiate(attackSuccessParticles, activeEnemy.transform.position, Quaternion.identity);
                Destroy(ps.gameObject, 1.0f);
            }

            logText.text = $"Vous touchez le boss en rythme ! Dégâts : {damage}";

            if (enemyHP <= 0)
            {
                StartCoroutine(VictoryRoutine());
            }
        }
        else
        {
            logText.text = "Trop tard ou trop tôt ! Suivez la pulsation.";
        }
    }

    private IEnumerator AnimateComboText()
    {
        comboFeedbackText.transform.localScale = Vector3.one * 1.5f;
        float elapsed = 0f;
        while (elapsed < 0.2f)
        {
            elapsed += Time.deltaTime;
            comboFeedbackText.transform.localScale = Vector3.Lerp(Vector3.one * 1.5f, Vector3.one, elapsed / 0.2f);
            yield return null;
        }
    }

    #endregion

    #region Système de Tag-Team (Changement d'alliés)

    private void TagNextCharacter()
    {
        if (allies.Count <= 1) return;

        // Trouver le prochain allié vivant
        int nextIndex = (activeAllyIndex + 1) % allies.Count;
        while (nextIndex != activeAllyIndex && allyHP[nextIndex] <= 0)
        {
            nextIndex = (nextIndex + 1) % allies.Count;
        }

        if (nextIndex == activeAllyIndex) return; // Aucun autre personnage vivant

        SwapActiveCharacter(nextIndex);
    }

    private void SwapActiveCharacter(int newIndex)
    {
        activeAllyIndex = newIndex;

        // Mettre à jour l'apparence visuelle du leader pour refléter le personnage sélectionné
        SpriteRenderer playerSR = playerController.GetComponentInChildren<SpriteRenderer>();
        Animator playerAnim = playerController.GetComponentInChildren<Animator>();

        if (playerSR != null && newIndex < allyOriginalSprites.Count)
        {
            playerSR.sprite = allyOriginalSprites[newIndex];
        }

        if (playerAnim != null && newIndex < allyOriginalAnimators.Count)
        {
            // Mettre à jour l'animator original du personnage taggé
            playerAnim.runtimeAnimatorController = allyOriginalAnimators[newIndex];
            playerAnim.Rebind();
            playerAnim.Update(0f);

            // Relancer la danse sur le nouveau personnage s'il possède cet état
            if (!string.IsNullOrEmpty(combatAnimationStateName))
            {
                playerAnim.Play(combatAnimationStateName);
            }
        }

        // Effet de tag
        playerController.TriggerInvincibility();
        if (attackSuccessParticles != null)
        {
            ParticleSystem ps = Instantiate(attackSuccessParticles, playerController.transform.position, Quaternion.identity);
            Destroy(ps.gameObject, 1.0f);
        }

        logText.text = $"{allies[activeAllyIndex].name} entre dans le combat !";
        UpdateUI();
    }

    #endregion

    #region Gestion des PV et Dégâts

    private void TakeDamage(int dmg)
    {
        if (currentState != CombatState.Active) return;

        // Appliquer les dégâts à l'allié actif
        allyHP[activeAllyIndex] = Mathf.Max(0, allyHP[activeAllyIndex] - dmg);
        playerController.TriggerInvincibility();

        // Particules d'impact
        if (hitParticles != null)
        {
            ParticleSystem ps = Instantiate(hitParticles, playerController.transform.position, Quaternion.identity);
            Destroy(ps.gameObject, 1.0f);
        }

        // Secouer la caméra (Screenshake simple)
        StartCoroutine(ScreenShake(0.15f, 0.2f));

        UpdateUI();

        if (allyHP[activeAllyIndex] <= 0)
        {
            logText.text = $"{allies[activeAllyIndex].name} est K.O. !";
            
            // Vérifier s'il reste des survivants
            bool anyAlive = false;
            for (int i = 0; i < allies.Count; i++)
            {
                if (allyHP[i] > 0)
                {
                    anyAlive = true;
                    break;
                }
            }

            if (anyAlive)
            {
                // Tag-team automatique sur le prochain allié vivant
                TagNextCharacter();
            }
            else
            {
                StartCoroutine(DefeatRoutine());
            }
        }
    }

    #endregion

    #region Victoire et Défaite

    private IEnumerator VictoryRoutine()
    {
        currentState = CombatState.Victory;
        logText.text = "Victoire éclatante !";
        BeatManager.Instance.StopMusic();

        yield return new WaitForSeconds(2.0f);
        yield return StartCoroutine(EndCombatRoutine(true));
    }

    private IEnumerator DefeatRoutine()
    {
        currentState = CombatState.Defeat;
        logText.text = "Tout le groupe a succombé...";
        BeatManager.Instance.StopMusic();

        yield return new WaitForSeconds(2.5f);
        yield return StartCoroutine(EndCombatRoutine(false));
    }

    private IEnumerator EndCombatRoutine(bool victory)
    {
        currentState = CombatState.Transitioning;
        yield return StartCoroutine(Fade(1f));

        // 1. Désactiver la grille
        radialGrid.SetGridActive(false);

        // 2. Nettoyer l'UI et les visuels de combat
        if (combatCanvas != null)
        {
            Destroy(combatCanvas.gameObject);
        }
        allyHPImages.Clear();
        allyHPTexts.Clear();

        if (activeVisualPrefab != null)
        {
            Destroy(activeVisualPrefab);
        }

        // Restaurer les visuels d'exploration de l'ennemi s'il n'a pas été détruit (ex: défaite)
        if (activeEnemy != null)
        {
            SpriteRenderer[] explorationSprites = activeEnemy.GetComponentsInChildren<SpriteRenderer>();
            foreach (var sprite in explorationSprites)
            {
                sprite.enabled = true;
            }
        }

        // 3. Restaurer le leader d'origine (supprimer le contrôleur de rythme)
        if (playerController != null)
        {
            // Restaurer le sprite d'origine du leader
            SpriteRenderer sr = playerController.GetComponentInChildren<SpriteRenderer>();
            Animator anim = playerController.GetComponentInChildren<Animator>();
            if (sr != null && allyOriginalSprites.Count > 0) sr.sprite = allyOriginalSprites[0];
            if (anim != null && allyOriginalAnimators.Count > 0)
            {
                anim.runtimeAnimatorController = allyOriginalAnimators[0];
                anim.Rebind();
                anim.Update(0f);
            }

            Destroy(playerController);
        }

        // 4. Si victoire, détruire l'ennemi
        if (victory && activeEnemy != null)
        {
            Destroy(activeEnemy);
        }

        // 5. Réactiver Cinemachine et la caméra
        if (brain != null)
        {
            brain.enabled = true;
        }
        if (virtualCamera != null)
        {
            virtualCamera.enabled = true;
            if (cameraHelper != null)
            {
                cameraHelper.enabled = true;
                cameraHelper.UpdateCameraSettings(false);
            }
            if (GroupManager.Instance != null && GroupManager.Instance.Leader != null)
            {
                virtualCamera.Follow = GroupManager.Instance.Leader;
            }
        }

        // 6. Réactiver les compagnons et le mouvement normal
        if (GroupManager.Instance != null)
        {
            GroupManager.Instance.enabled = true;
            foreach (var follower in GroupManager.Instance.ActiveFollowers)
            {
                if (follower != null)
                {
                    follower.gameObject.SetActive(true); // Rendre visible
                    follower.enabled = true;
                }
            }
            GroupManager.Instance.TeleportPartyToLeader();
            GroupManager.Instance.ReapplyAllCollisions();
        }

        Transform leader = GroupManager.Instance != null ? GroupManager.Instance.Leader : null;
        if (leader == null)
        {
            PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();
            if (pm != null) leader = pm.transform;
        }

        if (leader != null)
        {
            PlayerMovement pm = leader.GetComponent<PlayerMovement>();
            if (pm != null) pm.enabled = true;
        }

        yield return new WaitForSeconds(0.2f);
        yield return StartCoroutine(Fade(0f));

        currentState = CombatState.Transitioning;
        Debug.Log("[RhythmCombatManager] Combat terminé !");
    }

    #endregion

    #region Helpers Caméra & Sol

    private void UpdateCameraView(bool instant)
    {
        if (Camera.main == null || playerController == null || radialGrid == null) return;
        
        // Utiliser la TargetPosition (au sol) au lieu de la position physique transform (qui inclut le saut Y)
        Vector3 playerPos = playerController.TargetPosition;
        Vector3 center = radialGrid.transform.position;

        Vector3 dirToCenter = (center - playerPos).normalized;
        dirToCenter.y = 0f;
        dirToCenter.Normalize();

        // Placer la caméra derrière le joueur et surélevée
        Vector3 targetCamPos = playerPos - dirToCenter * cameraDistance + Vector3.up * cameraHeight;
        Quaternion targetCamRot = Quaternion.LookRotation((center + Vector3.up * 1f) - targetCamPos);

        if (instant)
        {
            Camera.main.transform.position = targetCamPos;
            Camera.main.transform.rotation = targetCamRot;
        }
        else
        {
            Camera.main.transform.position = Vector3.Lerp(Camera.main.transform.position, targetCamPos, Time.deltaTime * 6f);
            Camera.main.transform.rotation = Quaternion.Slerp(Camera.main.transform.rotation, targetCamRot, Time.deltaTime * 6f);
        }
    }

    private Vector3 SnapToGround(Vector3 position)
    {
        // Récupérer et désactiver temporairement les colliders de l'ennemi et du joueur pour éviter l'auto-collision
        Collider[] enemyColliders = activeEnemy != null ? activeEnemy.GetComponentsInChildren<Collider>() : new Collider[0];
        bool[] enemyColStates = new bool[enemyColliders.Length];
        for (int i = 0; i < enemyColliders.Length; i++)
        {
            enemyColStates[i] = enemyColliders[i].enabled;
            enemyColliders[i].enabled = false;
        }

        Collider[] playerColliders = playerController != null ? playerController.GetComponentsInChildren<Collider>() : new Collider[0];
        if (playerColliders.Length == 0)
        {
            Transform leader = GroupManager.Instance != null ? GroupManager.Instance.Leader : null;
            if (leader == null)
            {
                PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();
                if (pm != null) leader = pm.transform;
            }
            if (leader != null)
            {
                playerColliders = leader.GetComponentsInChildren<Collider>();
            }
        }
        bool[] playerColStates = new bool[playerColliders.Length];
        for (int i = 0; i < playerColliders.Length; i++)
        {
            playerColStates[i] = playerColliders[i].enabled;
            playerColliders[i].enabled = false;
        }

        RaycastHit hit;
        Vector3 origin = new Vector3(position.x, position.y + 10f, position.z);
        Vector3 finalPos = position;
        
        if (Physics.Raycast(origin, Vector3.down, out hit, 25f))
        {
            finalPos = hit.point;
        }

        // Restaurer les colliders
        for (int i = 0; i < enemyColliders.Length; i++)
        {
            enemyColliders[i].enabled = enemyColStates[i];
        }
        for (int i = 0; i < playerColliders.Length; i++)
        {
            playerColliders[i].enabled = playerColStates[i];
        }

        return finalPos;
    }

    private void OrientBossTowardsPlayer()
    {
        if (activeEnemy != null && playerController != null)
        {
            SpriteRenderer sr = activeEnemy.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                // Dans la perspective 2.5D, si le joueur est à gauche (axe X) du boss, le boss regarde à gauche (flipX = true)
                sr.flipX = playerController.transform.position.x < activeEnemy.transform.position.x;
            }
        }
    }

    private IEnumerator ScreenShake(float duration, float magnitude)
    {
        if (Camera.main == null) yield break;
        Vector3 originalPos = Camera.main.transform.position;
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            Camera.main.transform.position = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);
            elapsed += Time.deltaTime;
            yield return null;
        }

        Camera.main.transform.position = originalPos;
    }

    #endregion

    #region Génération de l'UI Dynamique

    private void CreateFadeCanvas()
    {
        if (fadeCanvasGroup != null) return;

        GameObject canvasObj = new GameObject("RhythmFade_Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 2000;

        fadeCanvasGroup = canvasObj.AddComponent<CanvasGroup>();
        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = false;

        GameObject imageObj = new GameObject("BlackImage");
        imageObj.transform.SetParent(canvasObj.transform, false);
        Image img = imageObj.AddComponent<Image>();
        img.color = Color.black;

        RectTransform r = img.rectTransform;
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.sizeDelta = Vector2.zero;

        DontDestroyOnLoad(canvasObj);
    }

    private IEnumerator Fade(float targetAlpha)
    {
        CreateFadeCanvas();
        float startAlpha = fadeCanvasGroup.alpha;
        float elapsed = 0f;
        float duration = 0.4f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            yield return null;
        }
        fadeCanvasGroup.alpha = targetAlpha;
    }

    private void CreateCombatUI()
    {
        // Créer le Sprite blanc 1x1 s'il n'existe pas encore pour que le remplissage des jauges fonctionne
        if (uiFillSprite == null)
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            uiFillSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        }

        GameObject canvasObj = new GameObject("RhythmCombatUI_Canvas");
        combatCanvas = canvasObj.AddComponent<Canvas>();
        combatCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        combatCanvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<GraphicRaycaster>();

        // 1. Conteneur Principal (Complètement transparent pour laisser place aux graphismes)
        GameObject mainPanel = new GameObject("MainPanel");
        mainPanel.transform.SetParent(canvasObj.transform, false);
        Image panelImage = mainPanel.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0f);

        RectTransform panelRect = panelImage.rectTransform;
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;

        // 2. Texte de Journal/Log (Flottant au centre inférieur)
        GameObject logObj = new GameObject("LogText");
        logObj.transform.SetParent(mainPanel.transform, false);
        logText = logObj.AddComponent<TextMeshProUGUI>();
        logText.fontSize = 24f;
        logText.color = Color.white;
        logText.alignment = TextAlignmentOptions.Center;
        logText.enableWordWrapping = true;

        RectTransform logRect = logText.rectTransform;
        logRect.anchorMin = new Vector2(0.25f, 0.05f);
        logRect.anchorMax = new Vector2(0.75f, 0.12f);
        logRect.sizeDelta = Vector2.zero;

        // 3. Pop-up de retour de combo (Centre de l'écran)
        GameObject comboObj = new GameObject("ComboFeedbackText");
        comboObj.transform.SetParent(canvasObj.transform, false);
        comboFeedbackText = comboObj.AddComponent<TextMeshProUGUI>();
        comboFeedbackText.fontSize = 55f;
        comboFeedbackText.fontStyle = FontStyles.Bold;
        comboFeedbackText.alignment = TextAlignmentOptions.Center;
        comboFeedbackText.text = "";

        RectTransform comboRect = comboFeedbackText.rectTransform;
        comboRect.anchorMin = new Vector2(0.4f, 0.45f);
        comboRect.anchorMax = new Vector2(0.6f, 0.65f);
        comboRect.sizeDelta = Vector2.zero;

        // 4. Panel d'invitation à Tag (Changement - discret en bas à gauche)
        tagPromptPanel = new GameObject("TagPromptPanel");
        tagPromptPanel.transform.SetParent(mainPanel.transform, false);
        TextMeshProUGUI tagPrompt = tagPromptPanel.AddComponent<TextMeshProUGUI>();
        tagPrompt.text = "[TAB] Changer de héros";
        tagPrompt.fontSize = 15f;
        tagPrompt.color = new Color(0.2f, 0.7f, 1.0f, 0.6f);
        tagPrompt.alignment = TextAlignmentOptions.MidlineLeft;

        RectTransform tagPromptRect = tagPrompt.rectTransform;
        tagPromptRect.anchorMin = new Vector2(0.04f, 0.01f);
        tagPromptRect.anchorMax = new Vector2(0.28f, 0.04f);
        tagPromptRect.sizeDelta = Vector2.zero;

        // 5. Barre de vie Boss (Haut de l'écran - ultra fine et épurée)
        GameObject bossPanel = new GameObject("BossHPPanel");
        bossPanel.transform.SetParent(canvasObj.transform, false);
        Image bossBg = bossPanel.AddComponent<Image>();
        bossBg.sprite = uiFillSprite;
        bossBg.color = new Color(0.02f, 0.02f, 0.05f, 0.6f);

        RectTransform bossRect = bossBg.rectTransform;
        bossRect.anchorMin = new Vector2(0.3f, 0.94f);
        bossRect.anchorMax = new Vector2(0.7f, 0.955f);
        bossRect.sizeDelta = Vector2.zero;

        GameObject bossFillObj = new GameObject("BossFill");
        bossFillObj.transform.SetParent(bossPanel.transform, false);
        bossHPImage = bossFillObj.AddComponent<Image>();
        bossHPImage.sprite = uiFillSprite;
        bossHPImage.color = new Color(0.9f, 0.1f, 0.15f, 0.9f);
        bossHPImage.type = Image.Type.Filled;
        bossHPImage.fillMethod = Image.FillMethod.Horizontal;
        bossHPImage.fillOrigin = (int)Image.OriginHorizontal.Left;

        RectTransform bossFillRect = bossHPImage.rectTransform;
        bossFillRect.anchorMin = Vector2.zero;
        bossFillRect.anchorMax = Vector2.one;
        bossFillRect.sizeDelta = Vector2.zero;

        GameObject bossNameObj = new GameObject("BossName");
        bossNameObj.transform.SetParent(bossPanel.transform, false);
        bossNameText = bossNameObj.AddComponent<TextMeshProUGUI>();
        bossNameText.text = activeEnemy.name.ToUpper();
        bossNameText.fontSize = 16f;
        bossNameText.fontStyle = FontStyles.Bold;
        bossNameText.color = Color.white;
        bossNameText.alignment = TextAlignmentOptions.Center;

        RectTransform bossNameRect = bossNameText.rectTransform;
        bossNameRect.anchorMin = new Vector2(0f, 1.2f);
        bossNameRect.anchorMax = new Vector2(1f, 2.5f);
        bossNameRect.sizeDelta = Vector2.zero;

        // 6. Barres de PV des Alliés (Empilées verticalement en bas à gauche)
        PopulateAlliesUI(mainPanel.transform);
    }

    private void PopulateAlliesUI(Transform parent)
    {
        allyHPImages.Clear();
        allyHPTexts.Clear();

        // Conteneur vertical en bas à gauche pour les héros
        GameObject groupContainer = new GameObject("AlliesGroupContainer");
        groupContainer.transform.SetParent(parent, false);
        RectTransform groupRect = groupContainer.AddComponent<RectTransform>();
        groupRect.anchorMin = new Vector2(0.04f, 0.05f);
        groupRect.anchorMax = new Vector2(0.26f, 0.28f);
        groupRect.sizeDelta = Vector2.zero;

        float cardHeightPct = 1f / allies.Count;
        float spacing = 0.05f;

        for (int i = 0; i < allies.Count; i++)
        {
            float minY = 1f - (i + 1) * cardHeightPct + spacing / 2f;
            float maxY = 1f - i * cardHeightPct - spacing / 2f;

            // Carte individuelle d'allié
            GameObject allyPanel = new GameObject($"AllyPanel_{i}");
            allyPanel.transform.SetParent(groupContainer.transform, false);
            Image allyBg = allyPanel.AddComponent<Image>();
            allyBg.sprite = uiFillSprite;
            allyBg.color = new Color(0.02f, 0.02f, 0.05f, 0.6f);

            RectTransform allyRect = allyBg.rectTransform;
            allyRect.anchorMin = new Vector2(0f, minY);
            allyRect.anchorMax = new Vector2(1f, maxY);
            allyRect.sizeDelta = Vector2.zero;

            // Remplissage PV
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(allyPanel.transform, false);
            Image fillImg = fillObj.AddComponent<Image>();
            fillImg.sprite = uiFillSprite;
            fillImg.color = i == activeAllyIndex ? new Color(0.1f, 0.9f, 0.4f, 0.8f) : new Color(0.3f, 0.4f, 0.3f, 0.4f);
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;

            RectTransform fillRect = fillImg.rectTransform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;
            allyHPImages.Add(fillImg);

            // Texte PV épuré
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(allyPanel.transform, false);
            TextMeshProUGUI txt = textObj.AddComponent<TextMeshProUGUI>();
            txt.text = $"  {allies[i].name.ToUpper()}  |  100/100 PV";
            txt.fontSize = 14f;
            txt.color = Color.white;
            txt.alignment = TextAlignmentOptions.MidlineLeft;

            RectTransform txtRect = txt.rectTransform;
            txtRect.anchorMin = new Vector2(0.04f, 0f);
            txtRect.anchorMax = new Vector2(0.96f, 1f);
            txtRect.sizeDelta = Vector2.zero;
            allyHPTexts.Add(txt);
        }
    }

    private void UpdateUI()
    {
        if (bossHPImage != null)
        {
            bossHPImage.fillAmount = (float)enemyHP / enemyMaxHP;
        }

        for (int i = 0; i < allies.Count; i++)
        {
            if (i < allyHPImages.Count && allyHPImages[i] != null)
            {
                allyHPImages[i].fillAmount = (float)allyHP[i] / allyMaxHP[i];
                // Mettre en surbrillance l'allié actif
                if (i == activeAllyIndex)
                {
                    allyHPImages[i].color = new Color(0.1f, 0.9f, 0.4f, 0.8f); // Vert vif
                }
                else
                {
                    allyHPImages[i].color = new Color(0.3f, 0.4f, 0.3f, 0.4f); // Vert terne
                }
            }

            if (i < allyHPTexts.Count && allyHPTexts[i] != null)
            {
                allyHPTexts[i].text = $"  {allies[i].name.ToUpper()}  |  {allyHP[i]}/{allyMaxHP[i]} PV";
            }
        }

        // Cacher l'invite de tag s'il ne reste qu'un seul personnage vivant
        if (tagPromptPanel != null)
        {
            int aliveCount = 0;
            foreach (var hp in allyHP)
            {
                if (hp > 0) aliveCount++;
            }
            tagPromptPanel.SetActive(aliveCount > 1);
        }
    }

    #endregion

    private void OnDestroy()
    {
        if (BeatManager.Instance != null)
        {
            BeatManager.Instance.OnBeat -= ProcessEnemyAttackBeat;
        }
    }
}
