using System.Collections;
using UnityEngine;

public class WaterLilly : MonoBehaviour
{
    [SerializeField] private GameObject _waterLilly;
    [SerializeField] private float _heightOffset = 2f;

    [Header("Rebond du Joueur")]
    [Tooltip("Activer le rebond lorsque le joueur saute depuis ce nénuphar.")]
    [SerializeField] private bool _enableBounce = true;
    [Tooltip("Force du saut conférée au joueur (défaut : 12f).")]
    [SerializeField] private float _bounceJumpForce = 12f;
    [Tooltip("Ajouter l'inertie verticale ascendante du ressort au saut du joueur.")]
    [SerializeField] private bool _addSpringMomentum = true;
    [Tooltip("Impulsion vers le bas infligée au nénuphar lors du saut pour exciter le SpringJoint.")]
    [SerializeField] private float _downwardImpulseOnJump = 7f;

    void Start()
    {
        if (_waterLilly != null)
        {
            this.transform.position = new Vector3(_waterLilly.transform.position.x, _waterLilly.transform.position.y + _heightOffset, _waterLilly.transform.position.z);
            
            SpringJoint sj = _waterLilly.GetComponent<SpringJoint>();
            if (sj != null)
            {
                Rigidbody parentRb = this.GetComponent<Rigidbody>();
                Rigidbody lilypadRb = _waterLilly.GetComponent<Rigidbody>();

                if (parentRb != null && parentRb != lilypadRb)
                {
                    sj.connectedBody = parentRb;
                }
            }

            if (_enableBounce)
            {
                WaterLillyBounce bounce = _waterLilly.GetComponent<WaterLillyBounce>();
                if (bounce == null)
                {
                    bounce = _waterLilly.AddComponent<WaterLillyBounce>();
                }
                bounce.Configure(_bounceJumpForce, _addSpringMomentum, _downwardImpulseOnJump);
            }
        }
    }
}

/// <summary>
/// Gère le comportement de rebond lorsqu'un personnage saute depuis ou atterrit sur un nénuphar.
/// S'intègre avec le SpringJoint et le Rigidbody du nénuphar pour un rendu physique dynamique.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[AddComponentMenu("ELISE/World/Water Lilly Bounce")]
public class WaterLillyBounce : MonoBehaviour
{
    [Header("Paramètres de Rebond du Joueur")]
    [Tooltip("Force du saut conférée au joueur lorsqu'il saute depuis le nénuphar (défaut : 12f).")]
    [SerializeField] private float bounceJumpForce = 12f;

    [Tooltip("Si activé, ajoute la vitesse ascendante du nénuphar au saut du joueur (récompense le bon timing avec le ressort).")]
    [SerializeField] private bool addSpringReboundMomentum = true;

    [Tooltip("Multiplicateur de la vitesse verticale du nénuphar ajoutée au saut.")]
    [SerializeField] private float springMomentumMultiplier = 1.2f;

    [Tooltip("Délai de tolérance (en secondes) après avoir quitté le nénuphar pendant lequel le super saut reste actif (Coyote Time).")]
    [SerializeField] private float coyoteGraceTime = 0.2f;

    [Header("Rebond Automatique (Optionnel)")]
    [Tooltip("Si coché, fait rebondir automatiquement le joueur dès son atterrissage sur le nénuphar (mode trampoline).")]
    [SerializeField] private bool autoBounceOnLanding = false;

    [Tooltip("Force du rebond automatique à l'atterrissage.")]
    [SerializeField] private float autoLandingBounceForce = 10f;

    [Header("Réaction Physique du Nénuphar")]
    [Tooltip("Impulsion vers le bas appliquée au nénuphar lorsque le joueur saute (excite le SpringJoint).")]
    [SerializeField] private float downwardImpulseOnJump = 7f;

    [Tooltip("Impulsion vers le bas appliquée au nénuphar lorsque le joueur atterrit dessus.")]
    [SerializeField] private float downwardImpulseOnLanding = 3f;

    [Header("Effets & Jus (Optionnel)")]
    [Tooltip("Système de particules déclenché lors d'un rebond (ex: splash / éclaboussures d'eau).")]
    [SerializeField] private ParticleSystem splashParticles;

    [Tooltip("AudioSource ou son déclenché lors du rebond.")]
    [SerializeField] private AudioSource bounceAudio;

    private Rigidbody lilypadRigidbody;
    private PlayerMovement activePlayer;
    private Coroutine exitGraceCoroutine;

    private void Awake()
    {
        lilypadRigidbody = GetComponent<Rigidbody>();
    }

    public void Configure(float jumpForce, bool addMomentum, float downwardJumpForce)
    {
        bounceJumpForce = jumpForce;
        addSpringReboundMomentum = addMomentum;
        downwardImpulseOnJump = downwardJumpForce;
    }

    private void OnCollisionEnter(Collision collision)
    {
        PlayerMovement pm = GetPlayerMovement(collision.gameObject);
        if (pm == null) return;

        if (!IsPlayerAbove(collision)) return;

        if (exitGraceCoroutine != null)
        {
            StopCoroutine(exitGraceCoroutine);
            exitGraceCoroutine = null;
        }

        if (activePlayer != null && activePlayer != pm)
        {
            activePlayer.OnJump -= HandlePlayerJump;
            activePlayer.ResetJumpForce();
        }

        activePlayer = pm;

        if (autoBounceOnLanding)
        {
            TriggerBounce(activePlayer, autoLandingBounceForce);
            if (lilypadRigidbody != null)
            {
                lilypadRigidbody.AddForce(Vector3.down * downwardImpulseOnJump, ForceMode.Impulse);
            }
            PlayEffects();
            return;
        }

        if (lilypadRigidbody != null)
        {
            lilypadRigidbody.AddForce(Vector3.down * downwardImpulseOnLanding, ForceMode.Impulse);
        }

        activePlayer.OnJump += HandlePlayerJump;
        UpdatePlayerJumpForce();
    }

    private void OnCollisionStay(Collision collision)
    {
        if (activePlayer == null)
        {
            PlayerMovement pm = GetPlayerMovement(collision.gameObject);
            if (pm != null && IsPlayerAbove(collision))
            {
                activePlayer = pm;
                activePlayer.OnJump += HandlePlayerJump;
            }
        }

        if (activePlayer != null && !autoBounceOnLanding)
        {
            UpdatePlayerJumpForce();
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        PlayerMovement pm = GetPlayerMovement(collision.gameObject);
        if (pm != null && pm == activePlayer)
        {
            if (exitGraceCoroutine != null)
            {
                StopCoroutine(exitGraceCoroutine);
            }
            exitGraceCoroutine = StartCoroutine(ExitGraceRoutine());
        }
    }

    private void UpdatePlayerJumpForce()
    {
        if (activePlayer == null) return;

        float calculatedForce = bounceJumpForce;

        if (addSpringReboundMomentum && lilypadRigidbody != null)
        {
            float upwardVelocity = lilypadRigidbody.linearVelocity.y;
            if (upwardVelocity > 0.1f)
            {
                calculatedForce += upwardVelocity * springMomentumMultiplier;
            }
        }

        activePlayer.SetNextJumpForce(calculatedForce);
    }

    private void HandlePlayerJump()
    {
        if (activePlayer == null) return;

        if (addSpringReboundMomentum && lilypadRigidbody != null)
        {
            float upwardVelocity = lilypadRigidbody.linearVelocity.y;
            if (upwardVelocity > 0.1f)
            {
                float finalForce = bounceJumpForce + (upwardVelocity * springMomentumMultiplier);
                activePlayer.Bounce(finalForce);
            }
        }

        if (lilypadRigidbody != null)
        {
            lilypadRigidbody.AddForce(Vector3.down * downwardImpulseOnJump, ForceMode.Impulse);
        }

        PlayEffects();

        activePlayer.OnJump -= HandlePlayerJump;
        activePlayer = null;
    }

    private void TriggerBounce(PlayerMovement pm, float force)
    {
        pm.Bounce(force);
    }

    private IEnumerator ExitGraceRoutine()
    {
        yield return new WaitForSeconds(coyoteGraceTime);

        if (activePlayer != null)
        {
            activePlayer.OnJump -= HandlePlayerJump;
            activePlayer.ResetJumpForce();
            activePlayer = null;
        }

        exitGraceCoroutine = null;
    }

    private bool IsPlayerAbove(Collision collision)
    {
        foreach (ContactPoint contact in collision.contacts)
        {
            if (contact.normal.y < -0.3f || collision.transform.position.y > transform.position.y)
            {
                return true;
            }
        }
        return false;
    }

    private PlayerMovement GetPlayerMovement(GameObject go)
    {
        PlayerMovement pm = go.GetComponent<PlayerMovement>();
        if (pm == null)
        {
            pm = go.GetComponentInParent<PlayerMovement>();
        }
        return pm;
    }

    private void PlayEffects()
    {
        if (splashParticles != null)
        {
            splashParticles.Play();
        }

        if (bounceAudio != null)
        {
            bounceAudio.Play();
        }
    }

    private void OnDisable()
    {
        if (exitGraceCoroutine != null)
        {
            StopCoroutine(exitGraceCoroutine);
            exitGraceCoroutine = null;
        }

        if (activePlayer != null)
        {
            activePlayer.OnJump -= HandlePlayerJump;
            activePlayer.ResetJumpForce();
            activePlayer = null;
        }
    }
}
