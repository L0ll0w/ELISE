using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Représente une entrée d'objet dans l'inventaire (objet + quantité).
/// </summary>
[System.Serializable]
public class InventoryEntry
{
    public ItemData item;
    public int quantity;

    public InventoryEntry(ItemData item, int quantity)
    {
        this.item = item;
        this.quantity = quantity;
    }
}

/// <summary>
/// Gestionnaire central de l'inventaire persistant.
/// </summary>
[AddComponentMenu("2.5D RPG/Inventory Manager")]
public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("Contenu de l'Inventaire")]
    [SerializeField] private List<InventoryEntry> inventory = new List<InventoryEntry>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Ajoute un objet à l'inventaire.
    /// </summary>
    public void AddItem(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0) return;

        if (item.itemType == ItemType.Equipment)
        {
            // Equipments do NOT stack, add as separate slots
            for (int i = 0; i < amount; i++)
            {
                inventory.Add(new InventoryEntry(item, 1));
            }
        }
        else
        {
            InventoryEntry entry = FindEntry(item);
            if (entry != null)
            {
                entry.quantity += amount;
            }
            else
            {
                inventory.Add(new InventoryEntry(item, amount));
            }
        }

        Debug.Log($"Ajouté à l'inventaire : {item.itemName} x{amount}. Total possédés : {GetItemQuantity(item)}");

        // Si le MenuManager est actif, on actualise l'UI
        if (MenuManager.Instance != null && MenuManager.Instance.IsMenuOpen)
        {
            MenuManager.Instance.PopulateInventory();
        }
    }

    /// <summary>
    /// Retire un objet de l'inventaire.
    /// </summary>
    public void RemoveItem(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0) return;

        if (item.itemType == ItemType.Equipment)
        {
            // Remove separate entries one by one
            for (int i = 0; i < amount; i++)
            {
                InventoryEntry entry = FindEntry(item);
                if (entry != null)
                {
                    inventory.Remove(entry);
                }
                else
                {
                    break;
                }
            }
        }
        else
        {
            InventoryEntry entry = FindEntry(item);
            if (entry != null)
            {
                entry.quantity -= amount;
                if (entry.quantity <= 0)
                {
                    inventory.Remove(entry);
                }
            }
        }

        Debug.Log($"Retiré de l'inventaire : {item.itemName} x{amount}.");

        // Si le MenuManager est actif, on actualise l'UI
        if (MenuManager.Instance != null && MenuManager.Instance.IsMenuOpen)
        {
            MenuManager.Instance.PopulateInventory();
        }
    }

    /// <summary>
    /// Récupère la quantité possédée d'un objet.
    /// </summary>
    public int GetItemQuantity(ItemData item)
    {
        if (item == null) return 0;
        int total = 0;
        foreach (var entry in inventory)
        {
            if (entry != null && entry.item != null && entry.item.itemID == item.itemID)
            {
                total += entry.quantity;
            }
        }
        return total;
    }

    /// <summary>
    /// Récupère la liste filtrée d'entrées d'inventaire par type d'objet.
    /// </summary>
    public List<InventoryEntry> GetItemsByType(ItemType type)
    {
        List<InventoryEntry> filteredList = new List<InventoryEntry>();
        foreach (var entry in inventory)
        {
            if (entry.item != null && entry.item.itemType == type)
            {
                filteredList.Add(entry);
            }
        }
        return filteredList;
    }

    /// <summary>
    /// Recherche une entrée existante pour un objet donné.
    /// </summary>
    private InventoryEntry FindEntry(ItemData item)
    {
        return inventory.Find(entry => entry.item != null && entry.item.itemID == item.itemID);
    }
}
