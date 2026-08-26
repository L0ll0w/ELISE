using UnityEngine;

/// <summary>
/// Projectile visuel qui descend du ciel et s'écrase sur une case au moment de l'impact du beat.
/// </summary>
public class FallingProjectile : MonoBehaviour
{
    private Vector3 startPos;
    private Vector3 endPos;
    private float duration;
    private float elapsed = 0f;
    private GameObject impactPrefab;

    public void Initialize(Vector3 start, Vector3 end, float time, GameObject impact = null)
    {
        startPos = start;
        endPos = end;
        duration = time;
        impactPrefab = impact;
        transform.position = start;
        elapsed = 0f;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / duration);
        
        // Descente avec accélération (Ease-In) pour simuler la gravité de la goutte
        transform.position = Vector3.Lerp(startPos, endPos, t * t);

        if (t >= 1f)
        {
            if (impactPrefab != null)
            {
                GameObject impactObj = Instantiate(impactPrefab, endPos, Quaternion.identity);
                Destroy(impactObj, 1.5f);
            }
            Destroy(gameObject);
        }
    }
}
