using UnityEngine;

/// <summary>
/// Structure stockant les données d'un choix de réponse interactif.
/// </summary>
[System.Serializable]
public struct ChoiceData
{
    public string text;
    public string nextNodeID;
}

/// <summary>
/// Représente un nœud (réplique) au sein d'un graphe de dialogue.
/// </summary>
[System.Serializable]
public struct DialogueNode
{
    public string nodeID;
    public string characterName;
    public Sprite portrait;
    [TextArea(3, 5)]
    public string sentence;
    public ChoiceData[] choices;
    public string nextNodeID;
}

/// <summary>
/// Conteneur de dialogue réutilisable sous forme d'Asset dans Unity.
/// Le nom de ce fichier doit correspondre EXACTEMENT au nom de la classe (DialogueData.cs)
/// pour que Unity puisse charger l'Asset correctement sans perdre les données.
/// </summary>
[CreateAssetMenu(fileName = "NouveauDialogue", menuName = "2.5D RPG/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    public DialogueNode[] nodes;

    public bool TryGetNode(string id, out DialogueNode node)
    {
        node = default;
        if (nodes == null || string.IsNullOrEmpty(id)) return false;

        foreach (var n in nodes)
        {
            if (n.nodeID == id)
            {
                node = n;
                return true;
            }
        }
        return false;
    }

    public DialogueNode GetStartNode()
    {
        if (nodes != null && nodes.Length > 0)
        {
            return nodes[0];
        }
        return default;
    }
}
