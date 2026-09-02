using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stocke l'état d'avancement des flags et des quêtes du jeu.
/// </summary>
public class StoryStateManager : MonoBehaviour
{
    private static StoryStateManager instance;
    public static StoryStateManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("StoryStateManager");
                instance = go.AddComponent<StoryStateManager>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    private Dictionary<string, bool> flags = new Dictionary<string, bool>();

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void SetFlag(string name, bool value)
    {
        if (string.IsNullOrEmpty(name)) return;
        flags[name.Trim().ToLower()] = value;
    }

    public bool GetFlag(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        return flags.TryGetValue(name.Trim().ToLower(), out bool val) && val;
    }
}
