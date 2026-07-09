#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Outil éditeur pour générer automatiquement la scène "ZoneDePoche" 
/// avec les 4 héros placés en demi-cercle et la caméra intimiste configurée.
/// </summary>
public static class ZoneDePocheCreator
{
    [MenuItem("2.5D RPG/Générer la Zone de Poche")]
    public static void GenerateScene()
    {
        // 1. Crée une nouvelle scène par défaut (contient déjà la caméra principale et la lumière directionnelle)
        Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        
        // 2. Configuration de la caméra principale (rapprochée, surélevée et intimiste)
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.transform.position = new Vector3(0f, 2.2f, -3.8f);
            mainCam.transform.rotation = Quaternion.Euler(22f, 0f, 0f);
            mainCam.fieldOfView = 40f; // Zoom plus serré pour l'intimité du camp
        }

        // 3. Crée un sol sombre chaleureux
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Sol_Camp";
        ground.transform.position = new Vector3(0f, -0.5f, 0f);
        ground.transform.localScale = new Vector3(12f, 1f, 12f);

        // Attribution d'un matériel basique coloré pour le rendu URP
        Renderer groundRenderer = ground.GetComponent<Renderer>();
        if (groundRenderer != null)
        {
            Material groundMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            groundMaterial.color = new Color(0.12f, 0.09f, 0.18f); // Violet nuit très cosy
            
            // Assure-toi que le dossier de destination existe
            if (!AssetDatabase.IsValidFolder("Assets/Settings"))
            {
                AssetDatabase.CreateFolder("Assets", "Settings");
            }
            
            AssetDatabase.CreateAsset(groundMaterial, "Assets/Settings/GroundMaterial.mat");
            groundRenderer.sharedMaterial = groundMaterial;
        }

        // 4. Création des 4 héros positionnés au calme
        string[] names = { "Jake", "Lune", "Elise", "Kid" };
        Vector3[] positions = {
            new Vector3(-1.6f, 0.5f, 1f),   // Jake (à gauche)
            new Vector3(1.6f, 0.5f, 1f),    // Lune (à droite)
            new Vector3(0f, 0.5f, 1.8f),    // Elise (au fond)
            new Vector3(0f, 0.5f, -0.3f)    // Kid (devant)
        };

        // Charge les sprites de portraits comme textures de test pour les sprites 3D
        Sprite jakeSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Portraits/jake.png");
        Sprite luneSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Portraits/lune.png");
        Sprite eliseSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Portraits/elise.png");
        Sprite kidSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Portraits/kid.png");
        Sprite[] sprites = { jakeSprite, luneSprite, eliseSprite, kidSprite };

        for (int i = 0; i < names.Length; i++)
        {
            GameObject hero = new GameObject(names[i]);
            hero.transform.position = positions[i];

            // Ajout du SpriteRenderer
            SpriteRenderer sr = hero.AddComponent<SpriteRenderer>();
            if (sprites[i] != null)
            {
                sr.sprite = sprites[i];
            }
            sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            sr.receiveShadows = true;

            // Ajout du Billboard pour faire face à la caméra
            hero.AddComponent<Billboard>();
        }

        // Création du point de spawn pour le joueur
        GameObject spawnPoint = new GameObject("SpawnPoint");
        spawnPoint.transform.position = new Vector3(0f, 0.5f, -1.8f);
        spawnPoint.transform.rotation = Quaternion.identity;

        // 5. Enregistrement automatique dans Assets/Scenes
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
        {
            AssetDatabase.CreateFolder("Assets", "Scenes");
        }

        string scenePath = "Assets/Scenes/ZoneDePoche.unity";
        bool saveSuccess = EditorSceneManager.SaveScene(newScene, scenePath);

        if (saveSuccess)
        {
            Debug.Log($"<color=green><b>[2.5D RPG]</b></color> Scène '{scenePath}' créée et configurée avec succès !");
            AddSceneToBuildSettings(scenePath);
        }
        else
        {
            Debug.LogError("[2.5D RPG] Erreur lors de la sauvegarde de la scène.");
        }
    }

    private static void AddSceneToBuildSettings(string scenePath)
    {
        var scenes = EditorBuildSettings.scenes;
        foreach (var s in scenes)
        {
            if (s.path == scenePath) return; // Déjà enregistrée
        }

        var newScenes = new EditorBuildSettingsScene[scenes.Length + 1];
        System.Array.Copy(scenes, newScenes, scenes.Length);
        newScenes[scenes.Length] = new EditorBuildSettingsScene(scenePath, true);
        EditorBuildSettings.scenes = newScenes;
        
        Debug.Log($"[2.5D RPG] Scène ajoutée aux Build Settings de Unity.");
    }
}
#endif
