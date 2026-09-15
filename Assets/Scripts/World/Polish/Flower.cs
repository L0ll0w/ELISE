using System.Collections;
using UnityEngine;

public class Flower : MonoBehaviour
{
    [Header("Shake Settings")]
    [Tooltip("")]
    [SerializeField] private float _shakeForce = 20f;
    [Tooltip("Oscillation speed")]
    [SerializeField] private float _shakeSpeed = 15f;
    [Tooltip("Hight Value, Quick Stop")]
    [SerializeField] private float _damping = 2f;
    [Tooltip("DURATION PD")]
    [SerializeField] private float _duration = 1.5f;

    [Header("Scale Settings")]
    [SerializeField] private Vector3 maxScaleMultiplier = new Vector3(1.1f, 1.2f, 1.1f);

    private Vector3 initialScale;
    private Quaternion initialRotation;
    private Coroutine shakeCoroutine;

    void Start()
    {
        initialScale = transform.localScale;
        initialRotation = transform.localRotation;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            Vector3 playerPosition = other.transform.position;
            HandleShakeAndScale(playerPosition);
        }
    }

    private void HandleShakeAndScale(Vector3 playerPosition)
    {
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
        }

        shakeCoroutine = StartCoroutine(ShakeAndScaleRoutine(playerPosition));
    }

    private IEnumerator ShakeAndScaleRoutine(Vector3 playerPosition)
    {
        float elapsed = 0f;

        Vector3 direction = (transform.position - playerPosition);
        direction.y = 0;
        direction.Normalize();

        Vector3 rotationAxis = Vector3.Cross(Vector3.up, direction).normalized;
        Debug.DrawLine(playerPosition, rotationAxis);

        while (elapsed < _duration)
        {
            // time continue
            elapsed += Time.deltaTime;
            float progress = elapsed / _duration;

            float currentDamping = Mathf.Lerp(1f, 0f, progress * _damping);

            float angle = Mathf.Sin(elapsed * _shakeSpeed) * _shakeForce * currentDamping;
            transform.localRotation = initialRotation * Quaternion.AngleAxis(angle, rotationAxis);

            // scale = sin (vitesse*temps)
            // 
            float scaleEffect = 
                Mathf.Sin(elapsed * _shakeSpeed) 
                * currentDamping; //  * (1f - progress) ?

            if (scaleEffect > 0)
            {
                transform.localScale = Vector3.Lerp(initialScale, Vector3.Scale(initialScale, maxScaleMultiplier), scaleEffect);
            }
            else
            {
                Vector3 _squishMultiplier = new Vector3(1.05f, 0.9f, 1.05f);
                transform.localScale = Vector3.Lerp(initialScale, Vector3.Scale(initialScale, _squishMultiplier), -scaleEffect);
            }

            yield return null;
        }



        transform.localRotation = initialRotation;
        transform.localScale = initialScale;
        shakeCoroutine = null;
    }
}