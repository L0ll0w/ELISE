using System.Collections;
using UnityEngine;

/// <summary>
/// Projectile d'encre tiré en balle de pistolet depuis le bout du doigt du joueur lors du QTE d'attaque.
/// Vise le centre exact de l'ennemi et déclenche un effet d'éclaboussure d'encre à l'impact.
/// </summary>
public class InkProjectile : MonoBehaviour
{
    private Transform targetTransform;
    private System.Action onImpactCallback;
    private float moveSpeed = 38f; // Vitesse vive de balle de pistolet

    /// <summary>
    /// Initialise et lance la balle d'encre vers la cible.
    /// </summary>
    public void Launch(Transform target, System.Action onImpact)
    {
        targetTransform = target;
        onImpactCallback = onImpact;

        // Visuel d'ogive/balle fuselée d'encre
        EnsureVisuals();

        StartCoroutine(FlyRoutine());
    }

    private void EnsureVisuals()
    {
        if (GetComponent<MeshFilter>() == null)
        {
            // Balle fuselée et écrasée (style cartouche d'encre)
            GameObject bulletMesh = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bulletMesh.transform.SetParent(transform, false);

            // Écrasé en diamètre X/Y et allongé sur l'axe Z (forme de balle de pistolet)
            bulletMesh.transform.localScale = new Vector3(0.18f, 0.18f, 0.75f);
            
            Collider col = bulletMesh.GetComponent<Collider>();
            if (col != null) Destroy(col);

            MeshRenderer mr = bulletMesh.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.material = new Material(Shader.Find("Sprites/Default"));
                mr.material.color = new Color(0.04f, 0.04f, 0.06f, 1f); // Noir charbon brillant
            }
        }
    }

    private Vector3 GetCenterPosition(Transform t)
    {
        if (t == null) return transform.position + transform.forward * 8f;

        Renderer r = t.GetComponent<Renderer>();
        if (r == null) r = t.GetComponentInChildren<Renderer>();
        if (r != null)
        {
            return r.bounds.center;
        }

        Collider c = t.GetComponent<Collider>();
        if (c == null) c = t.GetComponentInChildren<Collider>();
        if (c != null)
        {
            return c.bounds.center;
        }

        return t.position + Vector3.up * 0.7f;
    }

    private IEnumerator FlyRoutine()
    {
        Vector3 startPos = transform.position;
        Vector3 targetPos = GetCenterPosition(targetTransform);

        float dist = Vector3.Distance(startPos, targetPos);
        float duration = Mathf.Clamp(dist / moveSpeed, 0.10f, 0.32f); // Trajectoire ultra rapide
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            if (targetTransform != null)
            {
                targetPos = GetCenterPosition(targetTransform);
            }

            // Trajectoire 100% droite de balle de pistolet vers le centre exact
            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);

            // Orienter la balle de pistolet le long de son vecteur de mouvement
            Vector3 moveDir = (targetPos - transform.position).normalized;
            if (moveDir != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(moveDir);
            }

            transform.position = currentPos;
            yield return null;
        }

        transform.position = targetPos;

        // Effet d'éclaboussure d'encre à l'impact
        CreateInkSplash(targetPos);

        // Callback d'impact (Dégâts + Animation Hit de l'ennemi)
        onImpactCallback?.Invoke();

        Destroy(gameObject);
    }

    private void CreateInkSplash(Vector3 impactPos)
    {
        GameObject splashObj = new GameObject("InkSplash");
        splashObj.transform.position = impactPos;

        // Tache d'encre centrale
        GameObject centerSpot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        centerSpot.transform.SetParent(splashObj.transform, false);
        centerSpot.transform.localScale = new Vector3(0.4f, 0.4f, 0.1f);
        Collider cc = centerSpot.GetComponent<Collider>();
        if (cc != null) Destroy(cc);

        MeshRenderer mrCenter = centerSpot.GetComponent<MeshRenderer>();
        if (mrCenter != null)
        {
            mrCenter.material = new Material(Shader.Find("Sprites/Default"));
            mrCenter.material.color = new Color(0.04f, 0.04f, 0.06f, 0.95f);
        }

        // Gouttes d'encre projetees en couronne
        for (int i = 0; i < 8; i++)
        {
            GameObject drop = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            drop.transform.SetParent(splashObj.transform, false);
            float scale = Random.Range(0.1f, 0.22f);
            drop.transform.localScale = Vector3.one * scale;
            
            Collider c = drop.GetComponent<Collider>();
            if (c != null) Destroy(c);

            MeshRenderer mr = drop.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.material = new Material(Shader.Find("Sprites/Default"));
                mr.material.color = new Color(0.04f, 0.04f, 0.06f, 0.9f);
            }

            Vector3 randomDir = Random.onUnitSphere;
            drop.transform.localPosition = randomDir * Random.Range(0.2f, 0.55f);
        }

        Destroy(splashObj, 0.35f);
    }
}
