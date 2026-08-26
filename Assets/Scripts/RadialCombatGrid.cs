using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gère la géométrie et la visualisation de la grille polaire (cercles et secteurs)
/// sur le sol autour de l'ennemi. Permet de mapper les coordonnées de cellules (ring, sector)
/// en positions dans l'espace 3D.
/// </summary>
[AddComponentMenu("2.5D RPG/Rhythm/Radial Combat Grid")]
public class RadialCombatGrid : MonoBehaviour
{
    [Header("Dimensions de la Grille")]
    [Tooltip("Rayon du cercle intérieur (première ligne).")]
    [SerializeField] private float innerRadius = 2.5f;

    [Tooltip("Rayon du cercle extérieur (deuxième ligne).")]
    [SerializeField] private float outerRadius = 5.0f;

    [Tooltip("Nombre de secteurs angulaires (divisions de camembert).")]
    [SerializeField] private int sectorsCount = 8;

    [Tooltip("Nombre de cercles concentriques (lignes).")]
    [SerializeField] private int ringsCount = 2;

    [Header("Visualisation au sol")]
    [Tooltip("Matériau pour dessiner les lignes de la grille.")]
    [SerializeField] private Material gridLineMaterial;
    
    [Tooltip("Couleur de base de la grille.")]
    [SerializeField] private Color gridColor = new Color(0.2f, 0.6f, 1.0f, 0.4f); // Bleu néon transparent

    [Tooltip("Couleur lorsque la grille pulse sur le rythme.")]
    [SerializeField] private Color pulseColor = new Color(0.2f, 0.8f, 1.0f, 0.9f);

    [Tooltip("Épaisseur des lignes de la grille.")]
    [SerializeField] private float lineWidth = 0.05f;

    [Header("Alerte & Télégraphes")]
    [Tooltip("Couleur d'alerte pour les cellules ciblées par une attaque.")]
    [SerializeField] private Color warningColor = new Color(1.0f, 0.1f, 0.1f, 0.6f); // Rouge alerte

    private List<LineRenderer> lineRenderers = new List<LineRenderer>();
    private Dictionary<string, SpriteRenderer> cellWarningIndicators = new Dictionary<string, SpriteRenderer>();
    private Sprite warningCellSprite;

    private bool isGridActive = false;

    private void Start()
    {
        // Générer le sprite pour les alertes de caisses si nécessaire
        warningCellSprite = GenerateCellWarningSprite();
    }

    /// <summary>
    /// Active ou désactive l'affichage de la grille au sol et l'écoute des pulsations de rythme.
    /// </summary>
    public void SetGridActive(bool active)
    {
        isGridActive = active;

        // Générer la grille si elle n'existe pas encore
        if (active && lineRenderers.Count == 0)
        {
            DrawGrid();
        }

        // Afficher ou masquer les lignes
        foreach (var lr in lineRenderers)
        {
            if (lr != null)
            {
                lr.enabled = active;
            }
        }

        // Gérer l'abonnement aux événements du BeatManager
        if (BeatManager.Instance != null)
        {
            if (active)
            {
                BeatManager.Instance.OnBeat -= HandleBeatPulse;
                BeatManager.Instance.OnBeat += HandleBeatPulse;
            }
            else
            {
                BeatManager.Instance.OnBeat -= HandleBeatPulse;
            }
        }

        if (!active)
        {
            ClearAllWarnings();
        }
    }

    private void HandleBeatPulse(int beatIndex)
    {
        if (!isGridActive) return;
        // Effet de pulsation visuelle : on lance une coroutine pour faire clignoter la grille
        StartCoroutine(PulseRoutine());
    }

    private IEnumerator PulseRoutine()
    {
        float elapsed = 0f;
        float duration = 0.2f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            Color current = Color.Lerp(pulseColor, gridColor, t);
            SetGridColor(current);
            yield return null;
        }
        SetGridColor(gridColor);
    }

    private void SetGridColor(Color color)
    {
        foreach (var lr in lineRenderers)
        {
            if (lr != null)
            {
                lr.startColor = color;
                lr.endColor = color;
            }
        }
    }

    /// <summary>
    /// Calcule la position 3D d'une case sur la grille polaire, alignée sur le sol.
    /// </summary>
    public Vector3 GetCellPosition(int ringIndex, int sectorIndex)
    {
        // Limiter les indices
        ringIndex = Mathf.Clamp(ringIndex, 0, ringsCount - 1);
        sectorIndex = (sectorIndex % sectorsCount + sectorsCount) % sectorsCount; // Gestion du modulo négatif

        // Calculer l'angle au milieu du secteur (pour centrer le joueur)
        float angleStep = 360f / sectorsCount;
        float angleDeg = (sectorIndex * angleStep) + (angleStep / 2f);
        float angleRad = angleDeg * Mathf.Deg2Rad;

        // Déterminer le rayon moyen de la case entre le cercle de début et de fin du couloir
        float rStart = GetRingRadius(ringIndex);
        float rEnd = GetRingRadius(ringIndex + 1);
        float meanRadius = (rStart + rEnd) / 2f;

        Vector3 offset = new Vector3(Mathf.Cos(angleRad) * meanRadius, 0.02f, Mathf.Sin(angleRad) * meanRadius); // Légèrement surélevé pour le rendu
        return transform.position + offset;
    }

    /// <summary>
    /// Retourne le rayon exact pour un index de cercle frontière.
    /// </summary>
    public float GetRingRadius(int ringIndex)
    {
        if (ringsCount <= 0) return innerRadius;
        float t = (float)ringIndex / ringsCount;
        return Mathf.Lerp(innerRadius, outerRadius, t);
    }

    /// <summary>
    /// Projette un point sur le sol sous la grille.
    /// </summary>
    private Vector3 SnapToGround(Vector3 position)
    {
        RaycastHit hit;
        Vector3 origin = new Vector3(position.x, position.y + 10f, position.z);
        if (Physics.Raycast(origin, Vector3.down, out hit, 20f))
        {
            return hit.point;
        }
        return position;
    }

    /// <summary>
    /// Affiche un indicateur d'alerte rouge sur une case donnée.
    /// </summary>
    public void SetCellWarning(int ringIndex, int sectorIndex, bool active, Color? customColor = null)
    {
        string key = $"{ringIndex}_{sectorIndex}";

        if (active)
        {
            if (!cellWarningIndicators.ContainsKey(key))
            {
                GameObject indicator = new GameObject($"Warning_{key}");
                indicator.transform.position = GetCellPosition(ringIndex, sectorIndex) + Vector3.up * 0.05f; // Légèrement surélevé
                
                // Placer l'indicateur à plat sur le sol
                indicator.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

                SpriteRenderer sr = indicator.AddComponent<SpriteRenderer>();
                sr.sprite = warningCellSprite;
                sr.color = customColor ?? warningColor;
                sr.sortingOrder = -2; // Rendu sous l'ombre du joueur et le joueur

                // Adapter l'échelle à la taille de la case
                float ringWidth = (outerRadius - innerRadius) / (ringsCount - 1);
                indicator.transform.localScale = new Vector3(ringWidth * 0.8f, ringWidth * 0.8f, 1f);

                cellWarningIndicators.Add(key, sr);
            }
        }
        else
        {
            if (cellWarningIndicators.TryGetValue(key, out SpriteRenderer sr))
            {
                if (sr != null)
                {
                    Destroy(sr.gameObject);
                }
                cellWarningIndicators.Remove(key);
            }
        }
    }

    /// <summary>
    /// Efface toutes les alertes de cases actives.
    /// </summary>
    public void ClearAllWarnings()
    {
        foreach (var pair in cellWarningIndicators)
        {
            if (pair.Value != null)
            {
                Destroy(pair.Value.gameObject);
            }
        }
        cellWarningIndicators.Clear();
    }

    private void DrawGrid()
    {
        if (gridLineMaterial == null)
        {
            gridLineMaterial = new Material(Shader.Find("Sprites/Default"));
        }

        // 1. Dessiner les cercles concentriques (r <= ringsCount pour dessiner la bordure externe fermante)
        for (int r = 0; r <= ringsCount; r++)
        {
            float radius = GetRingRadius(r);
            CreateCircleRenderer(radius);
        }

        // 2. Dessiner les rayons de division angulaire
        float angleStep = 360f / sectorsCount;
        for (int s = 0; s < sectorsCount; s++)
        {
            float angleDeg = s * angleStep;
            CreateRadialLineRenderer(angleDeg);
        }
    }

    private void CreateCircleRenderer(float radius)
    {
        GameObject circleObj = new GameObject($"GridCircle_{radius}");
        circleObj.transform.SetParent(transform);
        circleObj.transform.localPosition = Vector3.zero;

        LineRenderer lr = circleObj.AddComponent<LineRenderer>();
        lr.material = gridLineMaterial;
        lr.startColor = gridColor;
        lr.endColor = gridColor;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.useWorldSpace = true;
        lr.loop = true;

        int segments = 60;
        lr.positionCount = segments;
        for (int i = 0; i < segments; i++)
        {
            float angle = i * (2f * Mathf.PI / segments);
            Vector3 offset = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            lr.SetPosition(i, transform.position + offset + Vector3.up * 0.01f);
        }

        lineRenderers.Add(lr);
    }

    private void CreateRadialLineRenderer(float angleDegrees)
    {
        GameObject lineObj = new GameObject($"GridRadial_{angleDegrees}");
        lineObj.transform.SetParent(transform);
        lineObj.transform.localPosition = Vector3.zero;

        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.material = gridLineMaterial;
        lr.startColor = gridColor;
        lr.endColor = gridColor;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.useWorldSpace = true;

        lr.positionCount = 2;
        float rad = angleDegrees * Mathf.Deg2Rad;
        
        Vector3 startOffset = new Vector3(Mathf.Cos(rad) * innerRadius, 0f, Mathf.Sin(rad) * innerRadius);
        Vector3 endOffset = new Vector3(Mathf.Cos(rad) * outerRadius, 0f, Mathf.Sin(rad) * outerRadius);

        lr.SetPosition(0, transform.position + startOffset + Vector3.up * 0.01f);
        lr.SetPosition(1, transform.position + endOffset + Vector3.up * 0.01f);

        lineRenderers.Add(lr);
    }

    private Sprite GenerateCellWarningSprite()
    {
        // Génère un disque de warning flou rouge
        int size = 32;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] colors = new Color[size * size];
        float center = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center + 0.5f;
                float dy = y - center + 0.5f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy) / (size / 2f);
                
                float alpha = 0f;
                if (dist < 1f)
                {
                    alpha = Mathf.SmoothStep(1f, 0.4f, dist);
                }

                colors[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(colors);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private void OnDestroy()
    {
        if (BeatManager.Instance != null)
        {
            BeatManager.Instance.OnBeat -= HandleBeatPulse;
        }
    }

    // Accesseurs
    public int SectorsCount => sectorsCount;
    public int RingsCount => ringsCount;
    public float InnerRadius => innerRadius;
    public float OuterRadius => outerRadius;
}
