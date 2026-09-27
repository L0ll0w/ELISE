using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Composant pour tout objet ramassable et portable par le joueur.
/// Hérite d'Interactable : lorsque le joueur interagit (touche E / bouton d'interaction),
/// il ramasse l'objet au-dessus de sa tête et le joueur passe en animation/sprite "Tennir" (Hold).
/// Une deuxième interaction permet de poser ou lancer l'objet.
/// Masque l'indicateur d'interaction ("?") lors du ramassage et empêche de ramasser d'autres objets simultanément.
/// </summary>
[AddComponentMenu("2.5D RPG/World/Carriable Item")]
public class CarriableItem : Interactable
{
    // Référence globale statique pour l'objet actuellement porté par le joueur
    public static CarriableItem CurrentlyCarriedItem { get; private set; }

    public static bool IsPlayerCarryingAnyItem()
    {
        return CurrentlyCarriedItem != null && CurrentlyCarriedItem.isCarried;
    }

    [Header("Positionnement au-dessus du Joueur")]
    [Tooltip("Décalage de position par rapport aux pieds du joueur lorsqu'il porte l'objet (ex: (0, 1.8, 0))")]
    [SerializeField] private Vector3 carryOffset = new Vector3(0f, 1.8f, 0f);

    [Tooltip("L'objet doit-il suivre le flip du sprite du joueur (gauche/droite) ?")]
    [SerializeField] private bool matchPlayerFacing = true;

    [Header("Animation & Sprite du Joueur")]
    [Tooltip("Nom du paramètre booléen dans l'Animator du joueur (ex: 'isHolding', 'isCarrying', 'Tennir')")]
    [SerializeField] private string animatorBoolParameter = "isHolding";

    [Tooltip("Nom de l'état/animation à jouer dans l'Animator (laisser vide si vous utilisez le paramètre booléen)")]
    [SerializeField] private string animatorStateToPlay = "Tennir";

    [Tooltip("Sprite optionnel pour remplacer directement le sprite du joueur pendant qu'il porte l'objet (si pas d'Animator)")]
    [SerializeField] private Sprite playerHoldSprite;

    [Header("Lancer / Poser l'Objet")]
    [Tooltip("Autoriser le joueur à poser ou lancer l'objet en ré-appuyant sur la touche d'interaction")]
    [SerializeField] private bool allowDrop = true;

    [Tooltip("Distance devant le joueur où l'objet est posé")]
    [SerializeField] private float dropForwardDistance = 1.0f;

    [Tooltip("Force de lancer vers l'avant (0 pour simplement poser l'objet au sol)")]
    [SerializeField] private float throwForce = 0f;

    [Header("Audio")]
    [SerializeField] private AudioClip pickUpSound;
    [SerializeField] private AudioClip dropSound;

    // État interne
    private bool isCarried = false;
    private Transform carryingPlayer;
    private Animator playerAnimator;
    private SpriteRenderer playerSpriteRenderer;
    private Sprite originalPlayerSprite;
    private Rigidbody rb;
    private Collider itemCollider;
    private AudioSource audioSource;
    private float pickUpTime = -1f;

    public bool IsCarried => isCarried;

    protected override void Start()
    {
        base.Start();

        rb = GetComponent<Rigidbody>();
        itemCollider = GetComponent<Collider>();
        audioSource = GetComponent<AudioSource>();
    }

    /// <summary>
    /// Empêche l'affichage du prompt d'interaction si cet objet est porté OU si le joueur porte déjà un autre objet.
    /// </summary>
    protected override bool CanInteract()
    {
        if (isCarried) return false;
        if (IsPlayerCarryingAnyItem()) return false;
        return base.CanInteract();
    }

    protected override void Interact()
    {
        if (!isCarried)
        {
            if (IsPlayerCarryingAnyItem()) return;

            // Trouver le joueur à proximité
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj == null)
            {
                PlayerMovement movement = FindFirstObjectByType<PlayerMovement>();
                if (movement != null) playerObj = movement.gameObject;
            }

            if (playerObj != null)
            {
                PickUp(playerObj);
            }
        }
        else if (allowDrop && Time.unscaledTime - pickUpTime > 0.2f)
        {
            Drop();
        }
    }

    /// <summary>
    /// Ramasse l'objet et le place au-dessus de la tête du joueur.
    /// </summary>
    public void PickUp(GameObject playerObj)
    {
        if (isCarried || IsPlayerCarryingAnyItem()) return;

        isCarried = true;
        CurrentlyCarriedItem = this;
        carryingPlayer = playerObj.transform;
        pickUpTime = Time.unscaledTime;
        hasInteracted = true;

        // Cacher et détruire l'indicateur d'interaction ("?")
        HideIndicator();
        CleanupIndicator();

        // 1. Désactiver la physique et les collisions de l'objet pendant le transport
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (itemCollider != null)
        {
            itemCollider.enabled = false;
        }

        // 2. Configurer l'animation et le sprite du Joueur
        playerAnimator = playerObj.GetComponent<Animator>();
        if (playerAnimator == null) playerAnimator = playerObj.GetComponentInChildren<Animator>();

        playerSpriteRenderer = playerObj.GetComponent<SpriteRenderer>();
        if (playerSpriteRenderer == null) playerSpriteRenderer = playerObj.GetComponentInChildren<SpriteRenderer>();

        if (playerAnimator != null)
        {
            if (!string.IsNullOrEmpty(animatorBoolParameter))
            {
                playerAnimator.SetBool(animatorBoolParameter, true);
            }

            if (!string.IsNullOrEmpty(animatorStateToPlay))
            {
                playerAnimator.Play(animatorStateToPlay);
            }
        }

        if (playerHoldSprite != null && playerSpriteRenderer != null)
        {
            originalPlayerSprite = playerSpriteRenderer.sprite;
            playerSpriteRenderer.sprite = playerHoldSprite;
        }

        // 3. Son de ramassage
        if (pickUpSound != null)
        {
            if (audioSource != null) audioSource.PlayOneShot(pickUpSound);
            else AudioSource.PlayClipAtPoint(pickUpSound, transform.position);
        }

        // Repositionnement initial au-dessus du joueur
        UpdateCarryPosition();
    }

    /// <summary>
    /// Pose ou lance l'objet au sol.
    /// </summary>
    public void Drop()
    {
        if (!isCarried) return;

        if (CurrentlyCarriedItem == this)
        {
            CurrentlyCarriedItem = null;
        }

        isCarried = false;

        // 1. Restaurer l'animation et le sprite du joueur
        if (playerAnimator != null)
        {
            if (!string.IsNullOrEmpty(animatorBoolParameter))
            {
                playerAnimator.SetBool(animatorBoolParameter, false);
            }
        }

        if (playerHoldSprite != null && playerSpriteRenderer != null && originalPlayerSprite != null)
        {
            playerSpriteRenderer.sprite = originalPlayerSprite;
        }

        // 2. Positionner l'objet devant le joueur
        if (carryingPlayer != null)
        {
            Vector3 dropDir = carryingPlayer.forward;
            if (playerSpriteRenderer != null && playerSpriteRenderer.flipX)
            {
                dropDir = -carryingPlayer.right;
            }

            Vector3 targetDropPos = carryingPlayer.position + dropDir * dropForwardDistance;
            transform.position = targetDropPos;
        }

        // 3. Réactiver les collisions et la physique
        if (itemCollider != null)
        {
            itemCollider.enabled = true;
        }

        if (rb != null)
        {
            rb.isKinematic = false;
            if (throwForce > 0f && carryingPlayer != null)
            {
                Vector3 throwDir = carryingPlayer.forward + Vector3.up * 0.5f;
                rb.AddForce(throwDir.normalized * throwForce, ForceMode.Impulse);
            }
        }

        // 4. Son de dépose
        if (dropSound != null)
        {
            if (audioSource != null) audioSource.PlayOneShot(dropSound);
            else AudioSource.PlayClipAtPoint(dropSound, transform.position);
        }

        carryingPlayer = null;
        hasInteracted = false;
    }

    protected override void Update()
    {
        if (isCarried)
        {
            HideIndicator();
            UpdateCarryPosition();

            // Écouter l'input pour poser l'objet si allowDrop est activé
            if (allowDrop && Time.unscaledTime - pickUpTime > 0.2f)
            {
                bool interact = false;
                #if ENABLE_INPUT_SYSTEM
                if ((Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) ||
                    (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame))
                {
                    interact = true;
                }
                #else
                if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.JoystickButton2))
                {
                    interact = true;
                }
                #endif

                if (interact)
                {
                    Drop();
                }
            }
            return;
        }

        base.Update();
    }

    private void LateUpdate()
    {
        if (isCarried)
        {
            UpdateCarryPosition();
        }
    }

    private void UpdateCarryPosition()
    {
        if (carryingPlayer == null) return;

        Vector3 targetPos = carryingPlayer.position + carryOffset;
        transform.position = targetPos;

        // Ajuster l'ordre de rendu du sprite de l'objet pour qu'il s'affiche devant/au-dessus du joueur
        SpriteRenderer itemSR = GetComponent<SpriteRenderer>();
        if (itemSR != null && playerSpriteRenderer != null)
        {
            itemSR.sortingOrder = playerSpriteRenderer.sortingOrder + 1;
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        if (CurrentlyCarriedItem == this)
        {
            CurrentlyCarriedItem = null;
        }
    }
}
