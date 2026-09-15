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
    public enum GridShapeType
    {
        FullCircle, // Cercle complet (360°)
        PartialArc  // Arc de cercle frontal délimité sur les côtés (ex: 3 ou 5 cases de large)
    }

    [Header("Forme & Dimensions de la Grille")]
    [Tooltip("Forme de la grille : Cercle complet (360°) ou Arc de cercle (délimité).")]
    [SerializeField] private GridShapeType gridShape = GridShapeType.FullCircle;

    [Tooltip("Rayon du cercle intérieur (première ligne).")]
    [SerializeField] private float innerRadius = 2.5f;

    [Tooltip("Rayon du cercle extérieur (deuxième ligne).")]
    [SerializeField] private float outerRadius = 5.0f;

    [Tooltip("Nombre de secteurs angulaires / cases en largeur (ex: 3, 5, 8).")]
    [SerializeField] private int sectorsCount = 8;

    [Tooltip("Nombre de cercles concentriques / rangées en profondeur (ex: 2, 3).")]
    [SerializeField] private int ringsCount = 2;

    [Tooltip("Angle d'ouverture total de l'arc en degrés si GridShape est PartialArc (ex: 60° pour 3 cases, 90° pour 5 cases).")]
    [SerializeField] private float arcAngleDegrees = 90f;

    [Tooltip("Orientation du centre de l'arc en degrés (270° = en face du boss / vers le bas écran).")]
    [SerializeField] private float arcCenterAngle = 270f;

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
    private Dictionary<GridCell, SpriteRenderer> cellWarningIndicators = new Dictionary<GridCell, SpriteRenderer>();
    private Sprite warningCellSprite;

    private bool isGridActive = false;

    public bool IsLooping => gridShape == GridShapeType.FullCircle;
    public GridShapeType GridShape => gridShape;
    public float ArcAngleDegrees => arcAngleDegrees;
    public float ArcCenterAngle => arcCenterAngle;
    public float InnerRadius => innerRadius;
    public float OuterRadius => outerRadius;
    public int SectorsCount => sectorsCount;
    public int RingsCount => ringsCount;

    private void Start()
    {
        // Générer le sprite pour les alertes de caisses si nécessaire
        warningCellSprite = GenerateCellWarningSprite();
    }

    /// <summary>
    /// Reconfigure la forme, le nombre de secteurs et de rangées de la grille.
    /// </summary>
    public void Configure(GridShapeType shape, int sectors, int rings, float arcAngle = 90f, float centerAngle = 270f)
    {
        gridShape = shape;
        sectorsCount = Mathf.Max(1, sectors);
        ringsCount = Mathf.Max(1, rings);
        arcAngleDegrees = Mathf.Clamp(arcAngle, 10f, 360f);
        arcCenterAngle = centerAngle;

        ClearGridLines();
        if (isGridActive)
        {
            DrawGrid();
        }
    }

    /// <summary>
    /// Active ou désactive l'affichage de la grille au sol et l'écoute des pulsations de rythme.
    /// </summary>
    public void SetGridActive(bool active)
    {
        isGridActive = active;

        // Générer la grille si elle n'existe pas encore ou la rafraîchir
        if (active)
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
    public Vector3 GetCellPosition(GridCell cell)
    {
        return GetCellPosition(cell.Ring, cell.Sector);
    }

    /// <summary>
    /// Calcule la position 3D d'une case sur la grille polaire, alignée sur le sol.
    /// </summary>
    public Vector3 GetCellPosition(int ringIndex, int sectorIndex)
    {
        ringIndex = Mathf.Clamp(ringIndex, 0, ringsCount - 1);

        float angleDeg;
        if (IsLooping)
        {
            sectorIndex = (sectorIndex % sectorsCount + sectorsCount) % sectorsCount;
            float angleStep = 360f / sectorsCount;
            angleDeg = (sectorIndex * angleStep) + (angleStep / 2f);
        }
        else
        {
            sectorIndex = Mathf.Clamp(sectorIndex, 0, sectorsCount - 1);
            float angleStep = arcAngleDegrees / sectorsCount;
            float startAngle = arcCenterAngle - (arcAngleDegrees / 2f);
            angleDeg = startAngle + (sectorIndex * angleStep) + (angleStep / 2f);
        }

        float angleRad = angleDeg * Mathf.Deg2Rad;

        // Déterminer le rayon moyen de la case entre le cercle de début et de fin du couloir
        float rStart = GetRingRadius(ringIndex);
        float rEnd = GetRingRadius(ringIndex + 1);
        float meanRadius = (rStart + rEnd) / 2f;

        Vector3 offset = new Vector3(Mathf.Cos(angleRad) * meanRadius, 0.02f, Mathf.Sin(angleRad) * meanRadius);
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
    /// Affiche un indicateur d'alerte rouge sur une case donnée.
    /// </summary>
    public void SetCellWarning(GridCell cell, bool active, Color? customColor = null)
    {
        SetCellWarning(cell.Ring, cell.Sector, active, customColor);
    }

    /// <summary>
    /// Affiche un indicateur d'alerte rouge sur une case donnée.
    /// </summary>
    public void SetCellWarning(int ringIndex, int sectorIndex, bool active, Color? customColor = null)
    {
        GridCell cell = new GridCell(ringIndex, sectorIndex);

        if (active)
        {
            if (!cellWarningIndicators.ContainsKey(cell))
            {
                GameObject indicator = new GameObject($"Warning_{cell.Ring}_{cell.Sector}");
                indicator.transform.position = GetCellPosition(cell) + Vector3.up * 0.05f;
                indicator.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

                SpriteRenderer sr = indicator.AddComponent<SpriteRenderer>();
                sr.sprite = warningCellSprite;
                sr.color = customColor ?? warningColor;
                sr.sortingOrder = -2;

                float ringWidth = (outerRadius - innerRadius) / Mathf.Max(1, ringsCount);
                indicator.transform.localScale = new Vector3(ringWidth * 0.8f, ringWidth * 0.8f, 1f);

                cellWarningIndicators.Add(cell, sr);
            }
        }
        else
        {
            if (cellWarningIndicators.TryGetValue(cell, out SpriteRenderer sr))
            {
                if (sr != null)
                {
                    Destroy(sr.gameObject);
                }
                cellWarningIndicators.Remove(cell);
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

    public void ClearGridLines()
    {
        foreach (var lr in lineRenderers)
        {
            if (lr != null)
            {
                Destroy(lr.gameObject);
            }
        }
        lineRenderers.Clear();
    }

    private void DrawGrid()
    {
        ClearGridLines();

        if (gridLineMaterial == null)
        {
            gridLineMaterial = new Material(Shader.Find("Sprites/Default"));
        }

        if (IsLooping)
        {
            // 1. Cercle complet : cercles concentriques 360°
            for (int r = 0; r <= ringsCount; r++)
            {
                float radius = GetRingRadius(r);
                CreateCircleRenderer(radius);
            }

            // 2. Rayons de division angulaire 360°
            float angleStep = 360f / sectorsCount;
            for (int s = 0; s < sectorsCount; s++)
            {
                float angleDeg = s * angleStep;
                CreateRadialLineRenderer(angleDeg);
            }
        }
        else
        {
            // Arc de cercle (PartialArc)
            float startAngle = arcCenterAngle - (arcAngleDegrees / 2f);
            float endAngle = arcCenterAngle + (arcAngleDegrees / 2f);

            // 1. Arcs concentriques
            for (int r = 0; r <= ringsCount; r++)
            {
                float radius = GetRingRadius(r);
                CreateArcRenderer(radius, startAngle, endAngle);
            }

            // 2. Lignes radiales de séparation (sectorsCount + 1 pour inclure les bordures latérales gauche et droite)
            float angleStep = arcAngleDegrees / sectorsCount;
            for (int s = 0; s <= sectorsCount; s++)
            {
                float angleDeg = startAngle + (s * angleStep);
                CreateRadialLineRenderer(angleDeg);
            }
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
        lr.useWorldSpace = false;
        lr.loop = true;
        lr.sortingOrder = -5;

        int segments = 60;
        lr.positionCount = segments;
        for (int i = 0; i < segments; i++)
        {
            float angle = i * (2f * Mathf.PI / segments);
            Vector3 offset = new Vector3(Mathf.Cos(angle) * radius, 0.01f, Mathf.Sin(angle) * radius);
            lr.SetPosition(i, offset);
        }

        lineRenderers.Add(lr);
    }

    private void CreateArcRenderer(float radius, float startAngleDeg, float endAngleDeg)
    {
        GameObject arcObj = new GameObject($"GridArc_{radius}");
        arcObj.transform.SetParent(transform);
        arcObj.transform.localPosition = Vector3.zero;

        LineRenderer lr = arcObj.AddComponent<LineRenderer>();
        lr.material = gridLineMaterial;
        lr.startColor = gridColor;
        lr.endColor = gridColor;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.useWorldSpace = false;
        lr.loop = false;
        lr.sortingOrder = -5;

        int segments = Mathf.Max(10, Mathf.RoundToInt((endAngleDeg - startAngleDeg) / 3f));
        lr.positionCount = segments + 1;
        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float angleDeg = Mathf.Lerp(startAngleDeg, endAngleDeg, t);
            float angleRad = angleDeg * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angleRad) * radius, 0.01f, Mathf.Sin(angleRad) * radius);
            lr.SetPosition(i, offset);
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
        lr.useWorldSpace = false;
        lr.sortingOrder = -5;

        lr.positionCount = 2;
        float rad = angleDegrees * Mathf.Deg2Rad;
        
        Vector3 startOffset = new Vector3(Mathf.Cos(rad) * innerRadius, 0.01f, Mathf.Sin(rad) * innerRadius);
        Vector3 endOffset = new Vector3(Mathf.Cos(rad) * outerRadius, 0.01f, Mathf.Sin(rad) * outerRadius);

        lr.SetPosition(0, startOffset);
        lr.SetPosition(1, endOffset);

        lineRenderers.Add(lr);
    }

    private Sprite GenerateCellWarningSprite()
    {
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
}
