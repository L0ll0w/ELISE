using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Cinemachine;

/// <summary>
/// Gestionnaire centralisé du système de combat au tour par tour (sans changement de scène).
/// Gère le placement sécurisé au sol, le fondu, l'UI générée à la volée, et les angles caméra dynamiques.
/// </summary>
[AddComponentMenu("2.5D RPG/Combat Manager")]
public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance { get; private set; }

    /// <summary>
    /// Indique si le combat est actuellement actif.
    /// </summary>
    public bool IsCombatActive => activeEnemy != null;

    public enum CombatState { Transitioning, PlayerTurn, EnemyTurn, Victory, Defeat }

    [Header("Configuration de l'Arène")]
    [Tooltip("Rayon d'espace libre recherché pour placer le combat sans bugger dans le décor.")]
    [SerializeField] private float arenaRadius = 4f;
    [Tooltip("Distance de recul des alliés par rapport au centre du combat.")]
    [SerializeField] private float playerOffset = 3f;
    [Tooltip("Distance de recul du monstre par rapport au centre du combat.")]
    [SerializeField] private float monsterOffset = 3f;
    [Tooltip("Angle total de l'arc de cercle pour placer le groupe (en degrés).")]
    [SerializeField] private float arcAngleSpan = 70f;
    [Tooltip("Layers considérés comme des obstacles statiques à éviter.")]
    [SerializeField] private LayerMask obstacleLayers;

    [Header("Configuration Caméra")]
    [Tooltip("Distance de la caméra derrière le personnage actif.")]
    [SerializeField] private float cameraBehindDistance = 4f;
    [Tooltip("Hauteur de la caméra derrière le personnage actif.")]
    [SerializeField] private float cameraBehindHeight = 2.5f;
    [Tooltip("Inclinaison X (Pitch) de la caméra derrière le personnage actif.")]
    [SerializeField] private float cameraBehindPitch = 15f;
    [Tooltip("Champ de vision (FOV) derrière le personnage actif.")]
    [SerializeField] private float cameraBehindFOV = 35f;

    [Space(10)]
    [Tooltip("Distance de la caméra lors du zoom-out du tour ennemi.")]
    [SerializeField] private float cameraMonsterTurnDistance = 8f;
    [Tooltip("Hauteur de la caméra lors du zoom-out du tour ennemi.")]
    [SerializeField] private float cameraMonsterTurnHeight = 4.5f;
    [Tooltip("Inclinaison X (Pitch) de la caméra lors du zoom-out du tour ennemi.")]
    [SerializeField] private float cameraMonsterTurnPitch = 22f;
    [Tooltip("Champ de vision (FOV) lors du zoom-out du tour ennemi.")]
    [SerializeField] private float cameraMonsterTurnFOV = 48f;

    [Space(10)]
    [Tooltip("Vitesse de transition/déplacement fluide de la caméra.")]
    [SerializeField] private float cameraLerpSpeed = 4f;

    [Header("Configuration Transitions")]
    [Tooltip("Durée du fondu au noir (en secondes).")]
    [SerializeField] private float fadeDuration = 0.5f;

    private CombatState currentState;
    private GameObject activeEnemy;
    private List<Transform> allies = new List<Transform>();
    private List<int> allyHP = new List<int>();
    private List<int> allyMaxHP = new List<int>();
    private int monsterHP;
    private int monsterMaxHP;
    private string monsterName;

    private int currentAllyIndex = 0;
    private bool isPlayerActionActive = false;

    // Références Caméra
    private CinemachineCamera virtualCamera;
    private CinemachineHelper cameraHelper;
    private Vector3 targetCamPosition;
    private Quaternion targetCamRotation;
    private float targetCamFOV;

    // Références UI (Gérées dynamiquement)
    private Canvas combatCanvas;
    private CanvasGroup fadeCanvasGroup;
    private TextMeshProUGUI logText;
    private Image bossHPImage;
    private TextMeshProUGUI bossNameText;
    private List<Image> allyHPImages = new List<Image>();
    private List<TextMeshProUGUI> allyHPTexts = new List<TextMeshProUGUI>();
    private GameObject mainPanel;

    private Vector3 combatCenter;
    private Vector3 combatDirection;

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
    }

    private void Update()
    {
        if (currentState == CombatState.Transitioning) return;

        // Mise à jour de la caméra vers sa cible
        UpdateCameraLerp();
    }

    #region Lancement et Arrêt du Combat

    /// <summary>
    /// Démarre la séquence de combat au tour par tour directement dans le monde.
    /// </summary>
    /// <param name="enemy">Le GameObject de l'ennemi.</param>
    public void StartCombat(GameObject enemy)
    {
        if (enemy == null)
        {
            Debug.LogError("[CombatManager] Impossible de lancer le combat car l'ennemi est nul.");
            return;
        }

        // Si le système de combat rythmique est présent, on lui délègue le combat !
        if (RhythmCombatManager.Instance != null)
        {
            RhythmCombatManager.Instance.StartCombat(enemy);
            return;
        }

        activeEnemy = enemy;
        StartCoroutine(StartCombatRoutine());
    }

    private IEnumerator StartCombatRoutine()
    {
        currentState = CombatState.Transitioning;
        Debug.Log("[CombatManager] Initialisation du combat au tour par tour...");

        // 1. Fondu au noir
        yield return StartCoroutine(UIFadeManager.Instance.FadeRoutine(1f, fadeDuration));

        // 2. Geler le joueur et désactiver le suivi de groupe
        PlayerLockManager.SetPlayerLocked(true);

        Transform leader = GroupManager.Instance != null ? GroupManager.Instance.Leader : null;
        if (leader == null)
        {
            PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();
            if (pm != null) leader = pm.transform;
        }

        // Désactiver CinemachineHelper et détacher la caméra de son suivi automatique
        virtualCamera = FindFirstObjectByType<CinemachineCamera>();
        if (virtualCamera != null)
        {
            virtualCamera.Follow = null;
        }

        // 3. Récupérer et configurer tous les membres du groupe
        allies.Clear();
        allyHP.Clear();
        allyMaxHP.Clear();

        if (leader != null) allies.Add(leader);
        if (GroupManager.Instance != null)
        {
            foreach (var follower in GroupManager.Instance.ActiveFollowers)
            {
                if (follower != null) allies.Add(follower.transform);
            }
        }

        // Limiter le groupe à 4 membres max par sécurité
        if (allies.Count > 4)
        {
            allies.RemoveRange(4, allies.Count - 4);
        }

        // Initialisation des PV à 100 par défaut pour le prototype
        for (int i = 0; i < allies.Count; i++)
        {
            allyHP.Add(100);
            allyMaxHP.Add(100);
        }

        // Configurer le monstre
        monsterMaxHP = 150;
        monsterHP = monsterMaxHP;
        monsterName = activeEnemy.name;

        // 4. Recherche de Zone Libre (Safe Center)
        Vector3 initialCenter = activeEnemy.transform.position;
        combatCenter = FindSafeCombatCenter(initialCenter);

        // Direction du combat (le monstre fait face à la position d'origine du joueur)
        Vector3 leaderPos = leader != null ? leader.position : Vector3.zero;
        combatDirection = (initialCenter - leaderPos).normalized;
        combatDirection.y = 0f;
        if (combatDirection.sqrMagnitude < 0.001f) combatDirection = Vector3.forward;
        else combatDirection.Normalize();

        // 5. Repositionnement géométrique au sol
        // Positionner le monstre
        Vector3 rawMonsterPos = combatCenter + combatDirection * monsterOffset;
        activeEnemy.transform.position = SnapToGround(rawMonsterPos);

        // Positionner les alliés
        int count = allies.Count;
        if (count == 1)
        {
            // Face à face direct
            Vector3 rawAllyPos = combatCenter - combatDirection * playerOffset;
            allies[0].position = SnapToGround(rawAllyPos);
        }
        else
        {
            // Arc de cercle face au monstre
            float startAngle = -arcAngleSpan / 2f;
            float angleStep = arcAngleSpan / (count - 1);
            float radius = playerOffset + monsterOffset;

            for (int i = 0; i < count; i++)
            {
                float angle = startAngle + i * angleStep;
                Vector3 rotDir = Quaternion.Euler(0f, angle, 0f) * (-combatDirection);
                Vector3 rawAllyPos = activeEnemy.transform.position + rotDir * radius;
                allies[i].position = SnapToGround(rawAllyPos);
            }
        }

        // Orienter les sprites
        OrientSpriteTowards(activeEnemy.transform, allies[0]); // Le monstre regarde le premier allié
        for (int i = 0; i < count; i++)
        {
            OrientSpriteTowards(allies[i], activeEnemy.transform); // Chaque allié regarde le monstre
        }

        // 6. Génération de l'UI de Combat
        CreateCombatUI();
        PopulateAlliesUI();
        UpdateUI();

        // 7. Initialisation de la caméra
        // On commence par cibler le premier allié (currentAllyIndex = 0)
        currentAllyIndex = 0;
        SetCameraBehindActiveAllyInstant();

        // Attendre un peu pour stabiliser le placement
        yield return new WaitForSeconds(0.2f);

        // 8. Fondu de retour (Fade In)
        yield return StartCoroutine(UIFadeManager.Instance.FadeRoutine(0f, fadeDuration));

        // 9. Lancer la boucle
        currentState = CombatState.PlayerTurn;
        isPlayerActionActive = false;
        logText.text = $"C'est au tour de {allies[currentAllyIndex].name} !";
    }

    private void EndCombat(bool victory)
    {
        StartCoroutine(EndCombatRoutine(victory));
    }

    private IEnumerator EndCombatRoutine(bool victory)
    {
        currentState = CombatState.Transitioning;

        // 1. Fondu au noir
        yield return StartCoroutine(UIFadeManager.Instance.FadeRoutine(1f, fadeDuration));

        // 2. Nettoyage de l'UI
        if (combatCanvas != null)
        {
            Destroy(combatCanvas.gameObject);
        }
        allyHPImages.Clear();
        allyHPTexts.Clear();

        // 3. Si victoire, on détruit le monstre
        if (victory && activeEnemy != null)
        {
            Destroy(activeEnemy);
        }

        // 4. Réactiver la caméra Cinemachine
        if (virtualCamera != null)
        {
            // Restaurer le suivi du leader
            if (GroupManager.Instance != null && GroupManager.Instance.Leader != null)
            {
                virtualCamera.Follow = GroupManager.Instance.Leader;
            }
        }

        // 5. Réactiver les contrôles du joueur et le suivi du groupe
        PlayerLockManager.SetPlayerLocked(false);

        yield return new WaitForSeconds(0.2f);

        // 6. Fondu de retour au jeu
        yield return StartCoroutine(UIFadeManager.Instance.FadeRoutine(0f, fadeDuration));

        currentState = CombatState.Transitioning; // Arrêt complet
        activeEnemy = null;
        Debug.Log("[CombatManager] Combat terminé !");
    }

    #endregion

    #region Algorithme de Recherche et Positionnement

    private Vector3 FindSafeCombatCenter(Vector3 initialCenter)
    {
        // Si aucun obstacle n'est détecté, on garde la position
        if (!Physics.CheckSphere(initialCenter, arenaRadius, obstacleLayers))
        {
            return initialCenter;
        }

        // Recherche en cercles concentriques extérieurs
        int steps = 8;
        float stepDistance = 1.5f;
        int maxRings = 5;

        for (int ring = 1; ring <= maxRings; ring++)
        {
            float radius = ring * stepDistance;
            for (int i = 0; i < steps; i++)
            {
                float angle = i * (2f * Mathf.PI / steps);
                Vector3 offset = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                Vector3 candidate = initialCenter + offset;

                if (!Physics.CheckSphere(candidate, arenaRadius, obstacleLayers))
                {
                    Debug.Log($"[CombatManager] Zone de combat libre trouvée à {candidate} après {ring} cercles de recherche.");
                    return candidate;
                }
            }
        }

        Debug.LogWarning("[CombatManager] Impossible de trouver une zone de combat 100% libre. Utilisation de la position initiale.");
        return initialCenter;
    }

    private Vector3 SnapToGround(Vector3 position)
    {
        RaycastHit hit;
        Vector3 origin = new Vector3(position.x, position.y + 10f, position.z);
        if (Physics.Raycast(origin, Vector3.down, out hit, 25f))
        {
            return hit.point;
        }
        return position;
    }

    private void OrientSpriteTowards(Transform unit, Transform target)
    {
        SpriteRenderer sr = unit.GetComponent<SpriteRenderer>();
        if (sr == null) sr = unit.GetComponentInChildren<SpriteRenderer>();

        if (sr != null && Camera.main != null)
        {
            Vector3 screenUnit = Camera.main.WorldToScreenPoint(unit.position);
            Vector3 screenTarget = Camera.main.WorldToScreenPoint(target.position);

            // Gérer le retournement du sprite en fonction de sa position relative à l'écran
            sr.flipX = screenTarget.x < screenUnit.x;
        }
    }

    #endregion

    #region Comportement de la Caméra de Combat

    private void SetCameraBehindActiveAllyInstant()
    {
        if (allies.Count == 0 || activeEnemy == null || virtualCamera == null) return;

        Vector3 allyPos = allies[currentAllyIndex].position;
        Vector3 enemyPos = activeEnemy.transform.position;

        Vector3 dirToEnemy = (enemyPos - allyPos).normalized;
        dirToEnemy.y = 0f;
        dirToEnemy.Normalize();

        Vector3 camPos = allyPos - dirToEnemy * cameraBehindDistance + Vector3.up * cameraBehindHeight;
        // Regarder légèrement au-dessus du centre de l'ennemi
        Quaternion camRot = Quaternion.LookRotation((enemyPos + Vector3.up * 1f) - camPos);

        virtualCamera.transform.position = camPos;
        virtualCamera.transform.rotation = camRot;
        virtualCamera.Lens.FieldOfView = cameraBehindFOV;

        targetCamPosition = camPos;
        targetCamRotation = camRot;
        targetCamFOV = cameraBehindFOV;
    }

    private void UpdateCameraLerp()
    {
        if (virtualCamera == null || activeEnemy == null) return;

        if (currentState == CombatState.PlayerTurn)
        {
            // Caméra derrière le joueur actif orientée vers le monstre
            Vector3 allyPos = allies[currentAllyIndex].position;
            Vector3 enemyPos = activeEnemy.transform.position;

            Vector3 dirToEnemy = (enemyPos - allyPos).normalized;
            dirToEnemy.y = 0f;
            dirToEnemy.Normalize();

            targetCamPosition = allyPos - dirToEnemy * cameraBehindDistance + Vector3.up * cameraBehindHeight;
            targetCamRotation = Quaternion.LookRotation((enemyPos + Vector3.up * 1f) - targetCamPosition);
            targetCamFOV = cameraBehindFOV;
        }
        else if (currentState == CombatState.EnemyTurn)
        {
            // Caméra reculée de face montrant tout le groupe
            Vector3 groupCenter = Vector3.zero;
            foreach (var a in allies) groupCenter += a.position;
            groupCenter /= allies.Count;

            Vector3 enemyPos = activeEnemy.transform.position;

            targetCamPosition = groupCenter - combatDirection * cameraMonsterTurnDistance + Vector3.up * cameraMonsterTurnHeight;
            targetCamRotation = Quaternion.LookRotation((enemyPos + Vector3.up * 1f) - targetCamPosition);
            targetCamFOV = cameraMonsterTurnFOV;
        }

        // Interpolation fluide
        virtualCamera.transform.position = Vector3.Lerp(virtualCamera.transform.position, targetCamPosition, Time.deltaTime * cameraLerpSpeed);
        virtualCamera.transform.rotation = Quaternion.Slerp(virtualCamera.transform.rotation, targetCamRotation, Time.deltaTime * cameraLerpSpeed);
        virtualCamera.Lens.FieldOfView = Mathf.Lerp(virtualCamera.Lens.FieldOfView, targetCamFOV, Time.deltaTime * cameraLerpSpeed);
    }

    #endregion

    #region Actions de Combat et Tour par Tour

    public void OnAttackClicked()
    {
        if (currentState != CombatState.PlayerTurn || isPlayerActionActive) return;
        StartCoroutine(PlayerAttackRoutine());
    }

    private IEnumerator PlayerAttackRoutine()
    {
        isPlayerActionActive = true;
        mainPanel.SetActive(false); // Masquer le panneau d'action

        Transform attacker = allies[currentAllyIndex];
        Transform target = activeEnemy.transform;

        logText.text = $"{attacker.name} charge le monstre !";

        // Animation de saut/charge (bump)
        yield return StartCoroutine(PerformAttackBumpAnimation(attacker, target));

        // Calcul des dégâts
        int dmg = Random.Range(20, 36);
        monsterHP = Mathf.Max(0, monsterHP - dmg);
        UpdateUI();

        logText.text = $"{attacker.name} inflige {dmg} points de dégâts au monstre !";
        yield return new WaitForSeconds(1.5f);

        // Vérification de la victoire
        if (monsterHP <= 0)
        {
            logText.text = "Le monstre a été vaincu ! Victoire !";
            currentState = CombatState.Victory;
            yield return new WaitForSeconds(2.0f);
            EndCombat(true);
        }
        else
        {
            // Passer au personnage suivant
            currentAllyIndex++;
            if (currentAllyIndex < allies.Count)
            {
                logText.text = $"C'est au tour de {allies[currentAllyIndex].name} !";
                mainPanel.SetActive(true);
                isPlayerActionActive = false;
            }
            else
            {
                // Fin du tour des alliés -> Début du tour de l'ennemi
                StartCoroutine(EnemyTurnRoutine());
            }
        }
    }

    public void OnFleeClicked()
    {
        if (currentState != CombatState.PlayerTurn || isPlayerActionActive) return;
        StartCoroutine(FleeRoutine());
    }

    private IEnumerator FleeRoutine()
    {
        isPlayerActionActive = true;
        mainPanel.SetActive(false);

        logText.text = "Vous essayez de fuir...";
        yield return new WaitForSeconds(1.5f);

        // Chance de fuite (75% de réussite pour le prototype)
        if (Random.value < 0.75f)
        {
            logText.text = "Fuite réussie !";
            yield return new WaitForSeconds(1.5f);
            EndCombat(false);
        }
        else
        {
            logText.text = "Fuite échouée ! Le monstre bloque la sortie !";
            yield return new WaitForSeconds(1.5f);
            
            // Fin du tour direct -> Tour de l'ennemi
            StartCoroutine(EnemyTurnRoutine());
        }
    }

    private IEnumerator EnemyTurnRoutine()
    {
        currentState = CombatState.EnemyTurn;
        mainPanel.SetActive(false);

        logText.text = $"C'est au tour de {monsterName} !";
        yield return new WaitForSeconds(1.5f);

        // Choisir un allié vivant au hasard
        List<int> aliveIndexList = new List<int>();
        for (int i = 0; i < allies.Count; i++)
        {
            if (allyHP[i] > 0) aliveIndexList.Add(i);
        }

        if (aliveIndexList.Count == 0)
        {
            // Plus personne en vie
            currentState = CombatState.Defeat;
            logText.text = "Tout le groupe a succombé...";
            yield return new WaitForSeconds(2f);
            EndCombat(false);
            yield break;
        }

        int targetIndex = aliveIndexList[Random.Range(0, aliveIndexList.Count)];
        Transform target = allies[targetIndex];

        logText.text = $"{monsterName} attaque {target.name} !";

        // Animation de saut du monstre vers sa cible
        yield return StartCoroutine(PerformAttackBumpAnimation(activeEnemy.transform, target));

        // Calcul des dégâts infligés au joueur
        int dmg = Random.Range(15, 26);
        allyHP[targetIndex] = Mathf.Max(0, allyHP[targetIndex] - dmg);
        UpdateUI();

        logText.text = $"{monsterName} inflige {dmg} dégâts à {target.name} !";
        yield return new WaitForSeconds(1.5f);

        // Vérification de la défaite
        bool allDead = true;
        for (int i = 0; i < allies.Count; i++)
        {
            if (allyHP[i] > 0) allDead = false;
        }

        if (allDead)
        {
            logText.text = "Défaite... Tout le groupe a été décimé.";
            currentState = CombatState.Defeat;
            yield return new WaitForSeconds(2.0f);
            EndCombat(false);
        }
        else
        {
            // Retour au tour des alliés
            currentAllyIndex = 0;
            // Trouver le premier allié en vie pour commencer
            while (allyHP[currentAllyIndex] <= 0)
            {
                currentAllyIndex++;
            }

            currentState = CombatState.PlayerTurn;
            isPlayerActionActive = false;
            logText.text = $"C'est au tour de {allies[currentAllyIndex].name} !";
            mainPanel.SetActive(true);
        }
    }

    private IEnumerator PerformAttackBumpAnimation(Transform attacker, Transform target)
    {
        Vector3 startPos = attacker.position;
        Vector3 targetPos = Vector3.Lerp(startPos, target.position, 0.25f); // Avance de 25% de la distance

        float elapsed = 0f;
        float duration = 0.25f;

        // Bump en avant
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            attacker.position = Vector3.Lerp(startPos, targetPos, elapsed / duration);
            yield return null;
        }

        // Léger temps d'arrêt
        yield return new WaitForSeconds(0.05f);

        // Retour en arrière
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            attacker.position = Vector3.Lerp(targetPos, startPos, elapsed / duration);
            yield return null;
        }

        attacker.position = startPos;
    }

    #endregion

    #region Génération de l'UI Dynamique

    private void CreateCombatUI()
    {
        // 1. Canvas
        GameObject canvasObj = new GameObject("CombatUI_Canvas");
        combatCanvas = canvasObj.AddComponent<Canvas>();
        combatCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        combatCanvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<GraphicRaycaster>();

        // 2. Panneau Principal (Bas de l'écran)
        mainPanel = new GameObject("MainPanel");
        mainPanel.transform.SetParent(canvasObj.transform, false);
        Image panelImage = mainPanel.AddComponent<Image>();
        panelImage.color = new Color(0.08f, 0.08f, 0.12f, 0.88f); // Thème sombre

        RectTransform panelRect = panelImage.rectTransform;
        panelRect.anchorMin = new Vector2(0f, 0f);
        panelRect.anchorMax = new Vector2(1f, 0.25f);
        panelRect.sizeDelta = Vector2.zero;

        // 3. Zone des boutons d'actions
        GameObject actionsObj = new GameObject("ActionsPanel");
        actionsObj.transform.SetParent(mainPanel.transform, false);
        RectTransform actionsRect = actionsObj.AddComponent<RectTransform>();
        actionsRect.anchorMin = new Vector2(0.04f, 0.15f);
        actionsRect.anchorMax = new Vector2(0.24f, 0.85f);
        actionsRect.sizeDelta = Vector2.zero;

        // Bouton ATTAQUER
        GameObject btnAttackObj = new GameObject("Button_Attack");
        btnAttackObj.transform.SetParent(actionsObj.transform, false);
        Image btnAttackImg = btnAttackObj.AddComponent<Image>();
        btnAttackImg.color = new Color(0.65f, 0.12f, 0.16f, 0.95f); // Rouge combat
        Button btnAttack = btnAttackObj.AddComponent<Button>();
        btnAttack.onClick.AddListener(OnAttackClicked);

        RectTransform btnAttackRect = btnAttackImg.rectTransform;
        btnAttackRect.anchorMin = new Vector2(0f, 0.52f);
        btnAttackRect.anchorMax = new Vector2(1f, 0.98f);
        btnAttackRect.sizeDelta = Vector2.zero;

        GameObject txtAttackObj = new GameObject("Text");
        txtAttackObj.transform.SetParent(btnAttackObj.transform, false);
        TextMeshProUGUI txtAttack = txtAttackObj.AddComponent<TextMeshProUGUI>();
        txtAttack.text = "ATTAQUER";
        txtAttack.fontStyle = FontStyles.Bold;
        txtAttack.alignment = TextAlignmentOptions.Center;
        txtAttack.fontSize = 24f;
        txtAttack.color = Color.white;

        RectTransform txtAttackRect = txtAttack.rectTransform;
        txtAttackRect.anchorMin = Vector2.zero;
        txtAttackRect.anchorMax = Vector2.one;
        txtAttackRect.sizeDelta = Vector2.zero;

        // Bouton FUIR
        GameObject btnFleeObj = new GameObject("Button_Flee");
        btnFleeObj.transform.SetParent(actionsObj.transform, false);
        Image btnFleeImg = btnFleeObj.AddComponent<Image>();
        btnFleeImg.color = new Color(0.22f, 0.22f, 0.28f, 0.95f); // Gris foncé
        Button btnFlee = btnFleeObj.AddComponent<Button>();
        btnFlee.onClick.AddListener(OnFleeClicked);

        RectTransform btnFleeRect = btnFleeImg.rectTransform;
        btnFleeRect.anchorMin = new Vector2(0f, 0.02f);
        btnFleeRect.anchorMax = new Vector2(1f, 0.48f);
        btnFleeRect.sizeDelta = Vector2.zero;

        GameObject txtFleeObj = new GameObject("Text");
        txtFleeObj.transform.SetParent(btnFleeObj.transform, false);
        TextMeshProUGUI txtFlee = txtFleeObj.AddComponent<TextMeshProUGUI>();
        txtFlee.text = "FUIR";
        txtFlee.fontStyle = FontStyles.Bold;
        txtFlee.alignment = TextAlignmentOptions.Center;
        txtFlee.fontSize = 24f;
        txtFlee.color = Color.white;

        RectTransform txtFleeRect = txtFlee.rectTransform;
        txtFleeRect.anchorMin = Vector2.zero;
        txtFleeRect.anchorMax = Vector2.one;
        txtFleeRect.sizeDelta = Vector2.zero;

        // 4. Cadre Journal (Texte défilant au milieu-haut)
        GameObject logObj = new GameObject("LogPanel");
        logObj.transform.SetParent(canvasObj.transform, false);
        Image logImage = logObj.AddComponent<Image>();
        logImage.color = new Color(0.04f, 0.04f, 0.06f, 0.85f);

        RectTransform logRect = logImage.rectTransform;
        logRect.anchorMin = new Vector2(0.25f, 0.28f);
        logRect.anchorMax = new Vector2(0.75f, 0.38f); // Juste au-dessus du panneau d'actions
        logRect.sizeDelta = Vector2.zero;

        GameObject logTxtObj = new GameObject("Text");
        logTxtObj.transform.SetParent(logObj.transform, false);
        logText = logTxtObj.AddComponent<TextMeshProUGUI>();
        logText.fontSize = 24f;
        logText.alignment = TextAlignmentOptions.Center;
        logText.color = Color.white;

        RectTransform logTxtRect = logText.rectTransform;
        logTxtRect.anchorMin = Vector2.zero;
        logTxtRect.anchorMax = Vector2.one;
        logTxtRect.sizeDelta = Vector2.zero;

        // 5. Boss HP Bar (Haut de l'écran)
        GameObject bossPanelObj = new GameObject("BossHPPanel");
        bossPanelObj.transform.SetParent(canvasObj.transform, false);
        Image bossPanelImage = bossPanelObj.AddComponent<Image>();
        bossPanelImage.color = new Color(0.12f, 0.02f, 0.02f, 0.8f);

        RectTransform bossPanelRect = bossPanelImage.rectTransform;
        bossPanelRect.anchorMin = new Vector2(0.3f, 0.85f);
        bossPanelRect.anchorMax = new Vector2(0.7f, 0.92f);
        bossPanelRect.sizeDelta = Vector2.zero;

        GameObject bossBarObj = new GameObject("BarFill");
        bossBarObj.transform.SetParent(bossPanelObj.transform, false);
        bossHPImage = bossBarObj.AddComponent<Image>();
        bossHPImage.color = new Color(0.85f, 0.1f, 0.15f, 1f); // Rouge vif

        RectTransform bossBarRect = bossHPImage.rectTransform;
        bossBarRect.anchorMin = new Vector2(0f, 0f);
        bossBarRect.anchorMax = new Vector2(1f, 1f);
        bossBarRect.pivot = new Vector2(0f, 0.5f);
        bossBarRect.sizeDelta = Vector2.zero;

        GameObject bossTxtObj = new GameObject("Text");
        bossTxtObj.transform.SetParent(bossPanelObj.transform, false);
        bossNameText = bossTxtObj.AddComponent<TextMeshProUGUI>();
        bossNameText.text = monsterName.ToUpper();
        bossNameText.fontStyle = FontStyles.Bold;
        bossNameText.alignment = TextAlignmentOptions.Center;
        bossNameText.fontSize = 20f;
        bossNameText.color = Color.white;

        RectTransform bossTxtRect = bossNameText.rectTransform;
        bossTxtRect.anchorMin = Vector2.zero;
        bossTxtRect.anchorMax = Vector2.one;
        bossTxtRect.sizeDelta = Vector2.zero;
    }

    private void PopulateAlliesUI()
    {
        allyHPImages.Clear();
        allyHPTexts.Clear();

        GameObject statsPanel = new GameObject("StatsPanel");
        statsPanel.transform.SetParent(mainPanel.transform, false);
        RectTransform statsRect = statsPanel.AddComponent<RectTransform>();
        statsRect.anchorMin = new Vector2(0.28f, 0.15f);
        statsRect.anchorMax = new Vector2(0.96f, 0.85f);
        statsRect.sizeDelta = Vector2.zero;

        int count = allies.Count;
        float columnWidth = 1f / count;

        for (int i = 0; i < count; i++)
        {
            Transform ally = allies[i];
            float left = i * columnWidth;
            float right = (i + 1) * columnWidth;

            GameObject colObj = new GameObject($"AllyCol_{i}");
            colObj.transform.SetParent(statsPanel.transform, false);
            RectTransform colRect = colObj.AddComponent<RectTransform>();
            colRect.anchorMin = new Vector2(left + 0.01f, 0f);
            colRect.anchorMax = new Vector2(right - 0.01f, 1f);
            colRect.sizeDelta = Vector2.zero;

            // Nom du joueur
            GameObject nameObj = new GameObject("Name");
            nameObj.transform.SetParent(colObj.transform, false);
            TextMeshProUGUI nameTxt = nameObj.AddComponent<TextMeshProUGUI>();
            nameTxt.text = ally.name.Replace("(Clone)", "").ToUpper();
            nameTxt.fontSize = 22f;
            nameTxt.fontStyle = FontStyles.Bold;
            nameTxt.alignment = TextAlignmentOptions.Left;
            nameTxt.color = Color.white;

            RectTransform nameRect = nameTxt.rectTransform;
            nameRect.anchorMin = new Vector2(0f, 0.65f);
            nameRect.anchorMax = new Vector2(1f, 1f);
            nameRect.sizeDelta = Vector2.zero;

            // Barre de vie Background
            GameObject bgBar = new GameObject("HPBg");
            bgBar.transform.SetParent(colObj.transform, false);
            Image bgImg = bgBar.AddComponent<Image>();
            bgImg.color = new Color(0.15f, 0.15f, 0.15f, 1f);

            RectTransform bgRect = bgImg.rectTransform;
            bgRect.anchorMin = new Vector2(0f, 0.15f);
            bgRect.anchorMax = new Vector2(0.95f, 0.5f);
            bgRect.sizeDelta = Vector2.zero;

            // Remplissage Barre
            GameObject fillBar = new GameObject("HPFill");
            fillBar.transform.SetParent(bgBar.transform, false);
            Image fillImg = fillBar.AddComponent<Image>();
            fillImg.color = new Color(0.15f, 0.75f, 0.25f, 1f); // Vert vif

            RectTransform fillRect = fillImg.rectTransform;
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(1f, 1f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.sizeDelta = Vector2.zero;

            // Texte HP numérique
            GameObject txtHPObj = new GameObject("HPText");
            txtHPObj.transform.SetParent(bgBar.transform, false);
            TextMeshProUGUI txtHP = txtHPObj.AddComponent<TextMeshProUGUI>();
            txtHP.text = "100 / 100";
            txtHP.fontSize = 16f;
            txtHP.fontStyle = FontStyles.Bold;
            txtHP.alignment = TextAlignmentOptions.Center;
            txtHP.color = Color.white;

            RectTransform txtHPRect = txtHP.rectTransform;
            txtHPRect.anchorMin = Vector2.zero;
            txtHPRect.anchorMax = Vector2.one;
            txtHPRect.sizeDelta = Vector2.zero;

            // Enregistrer
            allyHPImages.Add(fillImg);
            allyHPTexts.Add(txtHP);
        }
    }

    private void UpdateUI()
    {
        // Mettre à jour la barre de vie du Boss
        float bossRatio = (float)monsterHP / monsterMaxHP;
        bossHPImage.rectTransform.anchorMax = new Vector2(bossRatio, 1f);
        bossNameText.text = $"{monsterName.ToUpper()} : {monsterHP} / {monsterMaxHP}";

        // Mettre à jour les barres de vie des alliés
        for (int i = 0; i < allies.Count; i++)
        {
            float allyRatio = (float)allyHP[i] / allyMaxHP[i];
            allyHPImages[i].rectTransform.anchorMax = new Vector2(allyRatio, 1f);
            allyHPTexts[i].text = $"{allyHP[i]} / {allyMaxHP[i]}";

            // Changer la couleur de la barre de vie si les PV sont bas
            if (allyHP[i] <= 0)
            {
                allyHPImages[i].color = Color.gray; // Mort
            }
            else if (allyRatio < 0.3f)
            {
                allyHPImages[i].color = new Color(0.85f, 0.15f, 0.15f, 1f); // Danger : Rouge
            }
            else if (allyRatio < 0.6f)
            {
                allyHPImages[i].color = new Color(0.85f, 0.6f, 0.1f, 1f); // Moyen : Orange
            }
            else
            {
                allyHPImages[i].color = new Color(0.15f, 0.75f, 0.25f, 1f); // Bon : Vert
            }
        }
    }

    #endregion
}
