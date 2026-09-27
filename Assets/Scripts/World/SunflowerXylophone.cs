using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Contrôle l'animation et la synchronisation du Tournesol qui joue du Xylophone.
/// Gère la lecture à partir d'un fichier WAV complet OU note par note.
/// Déplace les baguettes (gauche/droite) pour frapper les lames au rythme de la musique.
/// Anime le sinking (enfoncement) et le scale pop (agrandissement) des lames à l'impact.
/// Se met automatiquement en pause lors des dialogues et reprend X secondes après la fin du dialogue.
/// </summary>
[AddComponentMenu("2.5D RPG/World/Sunflower Xylophone")]
public class SunflowerXylophone : MonoBehaviour
{
    [System.Serializable]
    public struct MelodyNote
    {
        [Tooltip("Index de la lame (0 = plus grave / gauche, N = plus aigu / droite)")]
        public int keyIndex;

        [Tooltip("Timestamp exact en secondes depuis le début de la chanson (utilisé avec useFullWavAudio)")]
        public float timestamp;

        [Tooltip("Durée/délai en battements avant la note suivante (utilisé en mode BPM sans WAV)")]
        public float beatDuration;

        [Tooltip("Force l'utilisation de la baguette gauche (true) ou droite (false)")]
        public bool forceCustomMallet;
        public bool useLeftMallet;

        public MelodyNote(int keyIndex, float timestamp, float beatDuration = 1.0f)
        {
            this.keyIndex = keyIndex;
            this.timestamp = timestamp;
            this.beatDuration = beatDuration;
            this.forceCustomMallet = false;
            this.useLeftMallet = false;
        }
    }

    [Header("Musique & Audio")]
    [Tooltip("Clip audio WAV complet de la chanson")]
    public AudioClip fullMelodyWavClip;

    [Tooltip("Si vrai, joue le fichier WAV complet et synchronise les baguettes selon les timestamps")]
    public bool useFullWavAudio = true;

    [Tooltip("Jouer aussi le son de la lame individuelle lors de l'impact (décochez si le son du xylophone est déjà dans le fichier WAV)")]
    public bool playKeySoundsWithWav = false;

    [Header("Pause lors du Dialogue")]
    [Tooltip("Met automatiquement en pause le xylophone si un dialogue commence, puis reprend après la fin du dialogue.")]
    public bool pauseDuringDialogue = true;

    [Tooltip("Si vrai, le xylophone ne se met en pause QUE si le dialogue est avec le Tournesol (et continue de jouer pour les autres PNJ).")]
    public bool pauseOnlyForSunflowerDialogue = true;

    [Tooltip("Délai (en secondes) à attendre après la fin du dialogue avant de reprendre le xylophone.")]
    public float resumeDelayAfterDialogue = 1.0f;

    [Header("Lames du Xylophone (Du grave au plus aigu)")]
    [Tooltip("Liste des Transform des lames du xylophone (ordonnées de gauche à droite)")]
    public Transform[] keys;

    [Tooltip("Sons des lames individuelles (si mode note par note ou playKeySoundsWithWav actif)")]
    public AudioClip[] keyAudioClips;

    [Tooltip("Composant AudioSource principal pour jouer le fichier WAV ou les notes")]
    public AudioSource audioSource;

    [Header("Baguettes / Batons")]
    [Tooltip("Baguette de gauche (côté notes graves)")]
    public Transform leftMallet;

    [Tooltip("Baguette de droite (côté notes aiguës)")]
    public Transform rightMallet;

    [Tooltip("Point de repos optionnel pour la baguette gauche quand elle est inactive")]
    public Transform leftRestPoint;

    [Tooltip("Point de repos optionnel pour la baguette droite quand elle est inactive")]
    public Transform rightRestPoint;

    [Header("Répartition des Baguettes")]
    [Tooltip("Index de la lame qui sépare les notes graves (baguette gauche) des aiguës (baguette droite). Exemple: 4 sur 8 lames.")]
    public int highPitchSplitIndex = 4;

    [Tooltip("Inverser les baguettes (si la baguette gauche doit jouer les aiguës et inversement)")]
    public bool invertMallets = false;

    [Header("Paramètres d'Animation des Baguettes")]
    [Tooltip("Hauteur de survol de la baguette au-dessus de la lame (en unités monde)")]
    public float hoverHeight = 0.35f;

    [Tooltip("Temps de déplacement horizontal vers la lame (secondes)")]
    public float moveDuration = 0.12f;

    [Tooltip("Temps d'impact vers le bas pour frapper la lame (secondes)")]
    public float strikeDownDuration = 0.05f;

    [Tooltip("Temps de remontée de la baguette après la frappe (secondes)")]
    public float retractUpDuration = 0.08f;

    [Tooltip("Angle d'inclinaison (rotation X en degrés) lors de la frappe")]
    public float strikeTiltAngle = 20f;

    [Tooltip("Courbe de lissage pour le déplacement")]
    public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Animation de la Lame à l'Impact")]
    [Tooltip("Enfoncement vertical de la lame vers le bas à la frappe")]
    public float keySinkDepth = 0.05f;

    [Tooltip("Multiplicateur d'agrandissement (scale) de la lame lors de la frappe (ex: 1.15 = 15% plus grand)")]
    public float keyHitScaleMultiplier = 1.15f;

    [Tooltip("Durée totale du rebond/agrandissement de la lame (secondes)")]
    public float keyBounceDuration = 0.12f;

    [Header("Réglages de la Mélodie")]
    [Tooltip("Lancer automatiquement au démarrage")]
    public bool playOnStart = true;

    [Tooltip("Boucler la mélodie une fois terminée")]
    public bool loopMelody = true;

    [Tooltip("Délai de pause avant de répéter la mélodie bouclée (en secondes)")]
    public float loopDelay = 1.5f;

    [Tooltip("Tempo en BPM (utilisé si useFullWavAudio est décoché)")]
    public float bpm = 120f;

    [Tooltip("Liste des notes de la partition")]
    public List<MelodyNote> melody = new List<MelodyNote>();

    [Header("Délais d'Interaction avec Objet Porté")]
    [Tooltip("Délai de pause (en secondes) après l'interaction avant de jouer la note de l'objet.")]
    public float delayBeforeItemNote = 1.0f;

    [Tooltip("Délai de pause (en secondes) après avoir joué la note avant de reprendre la musique.")]
    public float delayAfterItemNote = 1.0f;

    // État interne
    private bool isPlaying = false;
    private bool isPausedForDialogue = false;
    private bool wasDialogueActive = false;
    private Coroutine melodyCoroutine;
    private Coroutine resumeDialogueRoutine;
    private Coroutine itemInteractionRoutine;

    private Vector3 leftInitialPos;
    private Vector3 rightInitialPos;
    private Quaternion leftInitialRot;
    private Quaternion rightInitialRot;

    private Coroutine leftMalletRoutine;
    private Coroutine rightMalletRoutine;

    // Dictionnaire pour conserver les positions et scales de base originaux des lames
    private Dictionary<Transform, Vector3> defaultKeyLocalPositions = new Dictionary<Transform, Vector3>();
    private Dictionary<Transform, Vector3> defaultKeyLocalScales = new Dictionary<Transform, Vector3>();
    private Dictionary<Transform, Coroutine> activeKeyBounceRoutines = new Dictionary<Transform, Coroutine>();

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
        }

        if (leftMallet != null)
        {
            leftInitialPos = leftRestPoint != null ? leftRestPoint.position : leftMallet.position;
            leftInitialRot = leftRestPoint != null ? leftRestPoint.rotation : leftMallet.rotation;
        }

        if (rightMallet != null)
        {
            rightInitialPos = rightRestPoint != null ? rightRestPoint.position : rightMallet.position;
            rightInitialRot = rightRestPoint != null ? rightRestPoint.rotation : rightMallet.rotation;
        }

        CacheDefaultKeyTransforms();
    }

    private void CacheDefaultKeyTransforms()
    {
        if (keys != null)
        {
            foreach (Transform k in keys)
            {
                if (k != null && !defaultKeyLocalPositions.ContainsKey(k))
                {
                    defaultKeyLocalPositions[k] = k.localPosition;
                    defaultKeyLocalScales[k] = k.localScale;
                }
            }
        }
    }

    private void Start()
    {
        if (playOnStart)
        {
            PlayMelody();
        }
    }

    private void Update()
    {
        // Auto-pause et reprise lors des dialogues
        if (pauseDuringDialogue && DialogueManager.Instance != null)
        {
            bool isDialogueActive = DialogueManager.Instance.IsDialogueActive;

            // Début du dialogue -> mettre en pause le xylophone uniquement s'il s'agit du Tournesol
            if (isDialogueActive && !wasDialogueActive)
            {
                bool shouldPause = !pauseOnlyForSunflowerDialogue || IsDialogueWithSunflower();
                if (shouldPause && (isPlaying || (audioSource != null && audioSource.isPlaying)))
                {
                    PauseMelodyForDialogue();
                }
            }
            // Fin du dialogue -> attendre le délai puis reprendre la musique s'il était en pause
            else if (!isDialogueActive && wasDialogueActive)
            {
                if (isPausedForDialogue)
                {
                    if (resumeDialogueRoutine != null) StopCoroutine(resumeDialogueRoutine);
                    resumeDialogueRoutine = StartCoroutine(ResumeAfterDialogueDelayRoutine());
                }
            }

            wasDialogueActive = isDialogueActive;
        }
    }

    private bool IsDialogueWithSunflower()
    {
        // 1. Vérifier la distance avec le joueur : si le joueur est loin du tournesol, ce n'est pas son dialogue !
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();
            if (pm != null) playerObj = pm.gameObject;
        }

        if (playerObj != null)
        {
            float dist = Vector3.Distance(transform.position, playerObj.transform.position);
            if (dist > 7.0f)
            {
                return false;
            }
        }

        // 2. Si un composant DialogueTrigger est présent sur cet objet ou ses enfants/parents
        DialogueTrigger dt = GetComponent<DialogueTrigger>();
        if (dt == null) dt = GetComponentInParent<DialogueTrigger>();
        if (dt == null) dt = GetComponentInChildren<DialogueTrigger>();

        if (dt != null)
        {
            return true;
        }

        // 3. Fallback nom d'objet
        string objName = gameObject.name.ToLower();
        if (objName.Contains("devoveo") || objName.Contains("tournesol") || objName.Contains("sunflower") || objName.Contains("xylo"))
        {
            return true;
        }

        return false;
    }

    public void PauseMelodyForDialogue()
    {
        isPausedForDialogue = true;
        if (resumeDialogueRoutine != null)
        {
            StopCoroutine(resumeDialogueRoutine);
            resumeDialogueRoutine = null;
        }

        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Pause();
        }

        ReturnMalletsToRest();
    }

    private IEnumerator ResumeAfterDialogueDelayRoutine()
    {
        yield return new WaitForSeconds(resumeDelayAfterDialogue);

        if (isPausedForDialogue)
        {
            isPausedForDialogue = false;

            if (useFullWavAudio && fullMelodyWavClip != null && audioSource != null)
            {
                audioSource.UnPause();
            }
            else if (!isPlaying)
            {
                PlayMelody();
            }
        }
    }

    /// <summary>
    /// Active ou masque l'ensemble du modèle 3D du xylophone, ses lames et ses baguettes, et contrôle sa musique.
    /// </summary>
    public void SetXylophoneVisualsAndAudioActive(bool active)
    {
        if (active)
        {
            // Réactiver les visuels du xylophone
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                if (r != null && !r.gameObject.name.ToLower().Contains("devoveo") && !r.gameObject.name.ToLower().Contains("tournesol"))
                {
                    r.enabled = true;
                }
            }
            PlayMelody();
        }
        else
        {
            // Stopper la musique
            StopMelody();
            // Masquer le xylophone, ses lames et ses baguettes
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                if (r != null && !r.gameObject.name.ToLower().Contains("devoveo") && !r.gameObject.name.ToLower().Contains("tournesol"))
                {
                    r.enabled = false;
                }
            }
        }
    }

    public void PlayMelody()
    {
        StopMelody();
        isPlaying = true;
        isPausedForDialogue = false;

        if (useFullWavAudio && fullMelodyWavClip != null)
        {
            melodyCoroutine = StartCoroutine(PlayWavMelodyRoutine());
        }
        else if (melody != null && melody.Count > 0)
        {
            melodyCoroutine = StartCoroutine(PlayBpmMelodyRoutine());
        }
    }

    public void StopMelody()
    {
        isPlaying = false;
        isPausedForDialogue = false;

        if (melodyCoroutine != null)
        {
            StopCoroutine(melodyCoroutine);
            melodyCoroutine = null;
        }
        if (resumeDialogueRoutine != null)
        {
            StopCoroutine(resumeDialogueRoutine);
            resumeDialogueRoutine = null;
        }

        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
        ReturnMalletsToRest();
    }

    public void PlayNote(int keyIndex)
    {
        PlayNote(keyIndex, null, null);
    }

    public void PlayNote(int keyIndex, AudioClip customClip = null, Color? flashColor = null)
    {
        if (keyIndex < 0 || keys == null || keyIndex >= keys.Length) return;

        bool isHighPitch = keyIndex >= highPitchSplitIndex;
        bool useLeft = isHighPitch ? invertMallets : !invertMallets;

        Transform chosenMallet = useLeft ? leftMallet : rightMallet;
        if (chosenMallet == null) chosenMallet = leftMallet ?? rightMallet;
        if (chosenMallet == null) return;

        Quaternion baseRot = useLeft ? leftInitialRot : rightInitialRot;
        if (useLeft)
        {
            if (leftMalletRoutine != null) StopCoroutine(leftMalletRoutine);
            leftMalletRoutine = StartCoroutine(AnimateMalletStrike(chosenMallet, keyIndex, baseRot, true, customClip, flashColor));
        }
        else
        {
            if (rightMalletRoutine != null) StopCoroutine(rightMalletRoutine);
            rightMalletRoutine = StartCoroutine(AnimateMalletStrike(chosenMallet, keyIndex, baseRot, true, customClip, flashColor));
        }
    }

    /// <summary>
    /// Joue la note et applique la couleur associées à un objet porté par le joueur quand il parle au Tournesol.
    /// Effectue une pause de 1s au moment de l'interaction, joue la note, puis marque une pause de 1s avant de reprendre la musique.
    /// </summary>
    public void PlayNoteForCarriedItem(CarriableItem item)
    {
        if (item == null) return;

        if (itemInteractionRoutine != null)
        {
            StopCoroutine(itemInteractionRoutine);
        }
        itemInteractionRoutine = StartCoroutine(PlayNoteForCarriedItemRoutine(item));
    }

    private IEnumerator PlayNoteForCarriedItemRoutine(CarriableItem item)
    {
        if (item == null) yield break;

        Debug.Log($"[SunflowerXylophone] 🌻 Interaction avec '{item.gameObject.name}'. Pause de {delayBeforeItemNote}s avant la note...");

        // 1. Stopper la mélodie du xylophone
        StopMelody();

        // 2. Pause au moment de l'interaction (1 seconde par défaut)
        yield return new WaitForSeconds(delayBeforeItemNote);

        // 3. Jouer la note correspondant à l'objet et attendre la fin de l'animation de frappe
        int keyIndex = item.ItemKeyIndex;
        AudioClip customClip = item.ItemNoteSound;
        Color itemColor = item.ItemColor;

        if (keys != null && keyIndex >= 0 && keyIndex < keys.Length)
        {
            bool isHighPitch = keyIndex >= highPitchSplitIndex;
            bool useLeft = isHighPitch ? invertMallets : !invertMallets;
            Transform chosenMallet = useLeft ? leftMallet : rightMallet;
            if (chosenMallet == null) chosenMallet = leftMallet ?? rightMallet;

            if (chosenMallet != null)
            {
                Quaternion baseRot = useLeft ? leftInitialRot : rightInitialRot;

                if (useLeft && leftMalletRoutine != null) StopCoroutine(leftMalletRoutine);
                if (!useLeft && rightMalletRoutine != null) StopCoroutine(rightMalletRoutine);

                Coroutine strikeRoutine = StartCoroutine(AnimateMalletStrike(chosenMallet, keyIndex, baseRot, true, customClip, itemColor));
                yield return strikeRoutine;
            }
        }

        // 4. Pause après la frappe (1 seconde par défaut)
        ReturnMalletsToRest();
        Debug.Log($"[SunflowerXylophone] 🌻 Note jouée. Pause de {delayAfterItemNote}s avant de recommencer la mélodie de zéro...");
        yield return new WaitForSeconds(delayAfterItemNote);

        // 5. Recommencer la mélodie depuis le début (temps 0)
        PlayMelody();

        itemInteractionRoutine = null;
    }

    private IEnumerator PlayWavMelodyRoutine()
    {
        while (isPlaying)
        {
            melody.Sort((a, b) => a.timestamp.CompareTo(b.timestamp));

            audioSource.clip = fullMelodyWavClip;
            audioSource.time = 0f;
            audioSource.Play();

            float leadTime = moveDuration + strikeDownDuration;

            int noteIndex = 0;
            while ((audioSource.isPlaying || isPausedForDialogue) && isPlaying && noteIndex < melody.Count)
            {
                if (isPausedForDialogue)
                {
                    yield return null;
                    continue;
                }

                float currentAudioTime = audioSource.time;
                MelodyNote note = melody[noteIndex];

                float timeToStartMove = note.timestamp - leadTime - currentAudioTime;

                if (timeToStartMove <= 0f)
                {
                    TriggerNoteAnimation(note, playKeySoundsWithWav);
                    noteIndex++;
                }
                else
                {
                    yield return null;
                }
            }

            while ((audioSource.isPlaying || isPausedForDialogue) && isPlaying)
            {
                yield return null;
            }

            if (loopMelody && isPlaying && !isPausedForDialogue)
            {
                ReturnMalletsToRest();
                yield return new WaitForSeconds(loopDelay);
            }
            else if (!isPausedForDialogue)
            {
                isPlaying = false;
            }
        }

        ReturnMalletsToRest();
    }

    private IEnumerator PlayBpmMelodyRoutine()
    {
        while (isPlaying)
        {
            float secondsPerBeat = 60f / Mathf.Max(1f, bpm);

            for (int i = 0; i < melody.Count; i++)
            {
                if (!isPlaying) yield break;

                while (isPausedForDialogue)
                {
                    yield return null;
                }

                MelodyNote note = melody[i];
                TriggerNoteAnimation(note, true);

                float waitTime = note.beatDuration * secondsPerBeat;
                yield return new WaitForSeconds(waitTime);
            }

            if (loopMelody && isPlaying && !isPausedForDialogue)
            {
                yield return new WaitForSeconds(loopDelay);
            }
            else if (!isPausedForDialogue)
            {
                isPlaying = false;
            }
        }

        ReturnMalletsToRest();
    }

    private void TriggerNoteAnimation(MelodyNote note, bool playSound)
    {
        if (keys == null || note.keyIndex < 0 || note.keyIndex >= keys.Length) return;

        bool useLeft;
        if (note.forceCustomMallet)
        {
            useLeft = note.useLeftMallet;
        }
        else
        {
            bool isHighPitch = note.keyIndex >= highPitchSplitIndex;
            useLeft = isHighPitch ? invertMallets : !invertMallets;
        }

        Transform mallet = useLeft ? leftMallet : rightMallet;
        if (mallet == null) mallet = leftMallet ?? rightMallet;
        if (mallet == null) return;

        Quaternion baseRot = useLeft ? leftInitialRot : rightInitialRot;

        if (useLeft)
        {
            if (leftMalletRoutine != null) StopCoroutine(leftMalletRoutine);
            leftMalletRoutine = StartCoroutine(AnimateMalletStrike(mallet, note.keyIndex, baseRot, playSound));
        }
        else
        {
            if (rightMalletRoutine != null) StopCoroutine(rightMalletRoutine);
            rightMalletRoutine = StartCoroutine(AnimateMalletStrike(mallet, note.keyIndex, baseRot, playSound));
        }
    }

    private IEnumerator AnimateMalletStrike(Transform mallet, int keyIndex, Quaternion baseRotation, bool playSound, AudioClip customClip = null, Color? flashColor = null)
    {
        Transform targetKey = keys[keyIndex];
        if (targetKey == null) yield break;

        Vector3 keyTargetPos = targetKey.position;
        Vector3 hoverTargetPos = keyTargetPos + Vector3.up * hoverHeight;
        Vector3 startPos = mallet.position;

        // 1. Déplacement horizontal vers la lame
        float elapsed = 0f;
        while (elapsed < moveDuration)
        {
            if (isPausedForDialogue) yield break;
            elapsed += Time.deltaTime;
            float t = moveCurve.Evaluate(elapsed / moveDuration);
            mallet.position = Vector3.Lerp(startPos, hoverTargetPos, t);
            yield return null;
        }
        mallet.position = hoverTargetPos;

        // 2. Frappe vers le bas
        elapsed = 0f;
        Quaternion startRot = mallet.rotation;
        Quaternion strikeRot = baseRotation * Quaternion.Euler(strikeTiltAngle, 0f, 0f);

        while (elapsed < strikeDownDuration)
        {
            if (isPausedForDialogue) yield break;
            elapsed += Time.deltaTime;
            float t = elapsed / strikeDownDuration;
            mallet.position = Vector3.Lerp(hoverTargetPos, keyTargetPos, t);
            mallet.rotation = Quaternion.Slerp(startRot, strikeRot, t);
            yield return null;
        }

        mallet.position = keyTargetPos;
        mallet.rotation = strikeRot;

        // --- IMPACT ! ---
        if (playSound)
        {
            if (customClip != null)
            {
                if (audioSource != null) audioSource.PlayOneShot(customClip);
                else AudioSource.PlayClipAtPoint(customClip, keyTargetPos);
            }
            else
            {
                PlaySoundForKey(keyIndex);
            }
        }

        TriggerKeyHitEffect(targetKey, flashColor);

        // 3. Remontée
        elapsed = 0f;
        while (elapsed < retractUpDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / retractUpDuration;
            mallet.position = Vector3.Lerp(keyTargetPos, hoverTargetPos, t);
            mallet.rotation = Quaternion.Slerp(strikeRot, baseRotation, t);
            yield return null;
        }

        mallet.position = hoverTargetPos;
        mallet.rotation = baseRotation;
    }

    private void TriggerKeyHitEffect(Transform targetKey, Color? flashColor = null)
    {
        if (targetKey == null) return;

        if (activeKeyBounceRoutines.TryGetValue(targetKey, out Coroutine existingRoutine) && existingRoutine != null)
        {
            StopCoroutine(existingRoutine);
        }

        activeKeyBounceRoutines[targetKey] = StartCoroutine(AnimateKeyBounceAndScale(targetKey, flashColor));
    }

    private IEnumerator AnimateKeyBounceAndScale(Transform targetKey, Color? flashColor = null)
    {
        if (!defaultKeyLocalPositions.TryGetValue(targetKey, out Vector3 origLocalPos))
        {
            origLocalPos = targetKey.localPosition;
            defaultKeyLocalPositions[targetKey] = origLocalPos;
        }

        if (!defaultKeyLocalScales.TryGetValue(targetKey, out Vector3 origScale))
        {
            origScale = targetKey.localScale;
            defaultKeyLocalScales[targetKey] = origScale;
        }

        SpriteRenderer keySR = targetKey.GetComponent<SpriteRenderer>();
        if (keySR == null) keySR = targetKey.GetComponentInChildren<SpriteRenderer>();
        Color origColor = keySR != null ? keySR.color : Color.white;

        Vector3 sunkenPos = origLocalPos - new Vector3(0, keySinkDepth, 0);
        Vector3 expandedScale = origScale * keyHitScaleMultiplier;

        float downTime = keyBounceDuration * 0.35f;
        float elapsed = 0f;

        while (elapsed < downTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / downTime;
            targetKey.localPosition = Vector3.Lerp(origLocalPos, sunkenPos, t);
            targetKey.localScale = Vector3.Lerp(origScale, expandedScale, t);

            if (flashColor.HasValue && keySR != null)
            {
                keySR.color = Color.Lerp(origColor, flashColor.Value, t);
            }

            yield return null;
        }

        targetKey.localPosition = sunkenPos;
        targetKey.localScale = expandedScale;

        float upTime = keyBounceDuration * 0.65f;
        elapsed = 0f;

        while (elapsed < upTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / upTime;
            targetKey.localPosition = Vector3.Lerp(sunkenPos, origLocalPos, t);
            targetKey.localScale = Vector3.Lerp(expandedScale, origScale, t);

            if (flashColor.HasValue && keySR != null)
            {
                keySR.color = Color.Lerp(flashColor.Value, origColor, t);
            }

            yield return null;
        }

        targetKey.localPosition = origLocalPos;
        targetKey.localScale = origScale;

        if (keySR != null)
        {
            keySR.color = origColor;
        }
    }

    private void PlaySoundForKey(int keyIndex)
    {
        if (!useFullWavAudio && keyAudioClips != null && keyIndex < keyAudioClips.Length && keyAudioClips[keyIndex] != null)
        {
            audioSource.PlayOneShot(keyAudioClips[keyIndex]);
        }
    }

    private void ReturnMalletsToRest()
    {
        if (leftMallet != null && leftInitialPos != Vector3.zero)
        {
            StartCoroutine(AnimateReturnToRest(leftMallet, leftInitialPos, leftInitialRot));
        }

        if (rightMallet != null && rightInitialPos != Vector3.zero)
        {
            StartCoroutine(AnimateReturnToRest(rightMallet, rightInitialPos, rightInitialRot));
        }
    }

    private IEnumerator AnimateReturnToRest(Transform mallet, Vector3 restPos, Quaternion restRot)
    {
        Vector3 startPos = mallet.position;
        Quaternion startRot = mallet.rotation;
        float elapsed = 0f;
        float duration = 0.3f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            mallet.position = Vector3.Lerp(startPos, restPos, t);
            mallet.rotation = Quaternion.Slerp(startRot, restRot, t);
            yield return null;
        }

        mallet.position = restPos;
        mallet.rotation = restRot;
    }
}
