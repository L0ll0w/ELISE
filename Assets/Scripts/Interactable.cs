using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Classe de base pour tous les objets interactifs du jeu (PNJ, Dialogues, Coffres, Portes, etc.).
/// Gère la détection de proximité avec le joueur et écoute l'input d'interaction.
/// </summary>
[RequireComponent(typeof(Collider))]
public abstract class Interactable : MonoBehaviour
{
    protected bool isPlayerInRange = false;
    private Collider interactionCollider;

    [Header("Visual Prompt Configuration")]
    [Tooltip("Centralized settings for the interaction prompt. If empty, loads 'DefaultInteractionPromptSettings' from Resources.")]
    [SerializeField] protected InteractionPromptSettings promptSettings;

    private static InteractionPromptSettings defaultSettings;
    private GameObject indicatorInstance;
    private SpriteRenderer indicatorSR;
    private float currentAlpha = 0f;

    protected bool hasInteracted = false;
    private float interactTime = -1f;
    private bool wasDialogueActive = false;

    private InteractionPromptSettings ActiveSettings
    {
        get
        {
            if (promptSettings != null) return promptSettings;
            if (defaultSettings == null)
            {
                defaultSettings = Resources.Load<InteractionPromptSettings>("DefaultInteractionPromptSettings");
            }
            return defaultSettings;
        }
    }

    protected virtual void Reset()
    {
        // Configure automatiquement le collider en Trigger
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    protected virtual void Start()
    {
        interactionCollider = GetComponent<Collider>();
        if (interactionCollider != null)
        {
            interactionCollider.isTrigger = true;
        }

        InitializeIndicator();
    }

    protected virtual void Update()
    {
        bool isDialogueActive = DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive;

        if (isPlayerInRange && CanInteract() && !hasInteracted && !isDialogueActive)
        {
            bool interact = false;
            
            // Écoute de l'input avec l'Input System ou fallback classique
            #if ENABLE_INPUT_SYSTEM
            if ((Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) ||
                (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame))
            {
                interact = true;
            }
            #else
            if (Input.GetKeyDown(KeyCode.E) || Input.GetButtonDown("Submit") || Input.GetKeyDown(KeyCode.JoystickButton0))
            {
                interact = true;
            }
            #endif

            if (interact)
            {
                hasInteracted = true;
                interactTime = Time.unscaledTime;
                Interact();
            }
        }

        UpdateIndicator();
    }

    protected virtual void OnDisable()
    {
        CleanupIndicator();
    }

    protected virtual void OnDestroy()
    {
        CleanupIndicator();
    }

    private void CleanupIndicator()
    {
        if (indicatorInstance != null)
        {
            Destroy(indicatorInstance);
            indicatorInstance = null;
            indicatorSR = null;
        }
    }

    private void InitializeIndicator()
    {
        InteractionPromptSettings settings = ActiveSettings;
        if (settings == null || settings.indicatorSprite == null) return;

        indicatorInstance = new GameObject("InteractionIndicator");
        // We DO NOT set parent to transform so it is not affected by parent's scale!
        indicatorInstance.transform.localScale = settings.indicatorScale;

        indicatorSR = indicatorInstance.AddComponent<SpriteRenderer>();
        indicatorSR.sprite = settings.indicatorSprite;
        
        // Start with an invisible color
        Color initialColor = indicatorSR.color;
        initialColor.a = 0f;
        indicatorSR.color = initialColor;
        
        indicatorInstance.SetActive(false);
    }

    private void UpdateIndicator()
    {
        InteractionPromptSettings settings = ActiveSettings;
        if (settings == null || settings.indicatorSprite == null)
        {
            CleanupIndicator();
            return;
        }

        // Initialize indicator if it was created/configured after Start or dynamic settings changed
        if (indicatorInstance == null)
        {
            InitializeIndicator();
            if (indicatorInstance == null) return;
        }

        // Dialogue state and interaction tracking
        bool isDialogueActive = DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive;
        
        // Detect transition from dialogue active to inactive
        if (wasDialogueActive && !isDialogueActive)
        {
            hasInteracted = false;
        }
        wasDialogueActive = isDialogueActive;

        // Reappear delay if no dialogue is active
        if (hasInteracted && !isDialogueActive)
        {
            if (Time.unscaledTime - interactTime >= settings.reappearDelay)
            {
                hasInteracted = false;
            }
        }

        // Determine target alpha based on range, ability to interact, and interaction state
        float targetAlpha = (isPlayerInRange && CanInteract() && !hasInteracted && !isDialogueActive) ? 1f : 0f;

        // Fade animation
        if (Mathf.Abs(currentAlpha - targetAlpha) > 0.001f)
        {
            currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, settings.fadeSpeed * Time.unscaledDeltaTime);
            if (indicatorSR != null)
            {
                Color color = indicatorSR.color;
                color.a = currentAlpha;
                indicatorSR.color = color;
            }
        }

        // Handle activity status
        if (currentAlpha > 0f)
        {
            if (!indicatorInstance.activeSelf)
            {
                indicatorInstance.SetActive(true);
            }

            // Update Position (World Space)
            Vector3 basePosition = transform.position;
            Vector3 topRightOffset = Vector3.zero;

            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                Vector3 cameraRight = mainCam.transform.right;
                Vector3 cameraUp = mainCam.transform.up;
                Vector3 cameraForward = mainCam.transform.forward;

                if (settings.useBoundsTopRight)
                {
                    // Compute size of collider/renderer
                    Vector3 extents = Vector3.zero;
                    if (interactionCollider != null)
                    {
                        basePosition = interactionCollider.bounds.center;
                        extents = interactionCollider.bounds.extents;
                    }
                    else
                    {
                        Renderer renderer = GetComponent<Renderer>();
                        if (renderer != null)
                        {
                            basePosition = renderer.bounds.center;
                            extents = renderer.bounds.extents;
                        }
                    }

                    // Height and width along camera-relative axes
                    float height = extents.y > 0 ? extents.y : 0.5f;
                    float width = Mathf.Max(extents.x, extents.z);
                    if (width <= 0) width = 0.5f;

                    topRightOffset = cameraUp * height + cameraRight * width;
                }

                // Apply dynamic position
                Vector3 localOffset = cameraRight * settings.indicatorOffset.x + cameraUp * settings.indicatorOffset.y + cameraForward * settings.indicatorOffset.z;
                indicatorInstance.transform.position = basePosition + topRightOffset + localOffset;

                // Apply rotation / billboard
                float angle = Mathf.Sin(Time.unscaledTime * settings.rotationSpeed) * settings.maxRotationAngle;
                if (settings.billboard)
                {
                    indicatorInstance.transform.rotation = mainCam.transform.rotation * Quaternion.Euler(0, 0, angle);
                }
                else
                {
                    indicatorInstance.transform.localRotation = Quaternion.Euler(0, 0, angle);
                }
            }
            else
            {
                // Fallback if main camera is missing
                indicatorInstance.transform.position = basePosition + settings.indicatorOffset;
                float angle = Mathf.Sin(Time.unscaledTime * settings.rotationSpeed) * settings.maxRotationAngle;
                indicatorInstance.transform.localRotation = Quaternion.Euler(0, 0, angle);
            }
        }
        else
        {
            if (indicatorInstance.activeSelf)
            {
                indicatorInstance.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Vérifie si l'interaction est possible (ex: jeu non en pause).
    /// </summary>
    protected virtual bool CanInteract()
    {
        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused)
        {
            return false;
        }
        return true;
    }

    /// <summary>
    /// Action à exécuter lors de l'interaction. Doit être implémentée par les classes enfants.
    /// </summary>
    protected abstract void Interact();

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.GetComponent<PlayerMovement>() != null)
        {
            isPlayerInRange = true;
            hasInteracted = false;
        }
    }

    protected virtual void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") || other.GetComponent<PlayerMovement>() != null)
        {
            isPlayerInRange = false;
            hasInteracted = false;
        }
    }
}
