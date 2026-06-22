using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    public event EventHandler OnObjectPickUp;

    [SerializeField] private Player player;
    [SerializeField] private List<InventoryItem> inventory = new List<InventoryItem>();

    private int maxCollectible = 9;
    private int maxRareCollectible = 3;

    public List<InventoryItem> GetInventory()
    {
        return inventory;
    }

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        player.OnObjectPickUp += Player_OnObjectPickUp;
    }

    private void Player_OnObjectPickUp(object sender, Player.OnObjectPickUpEventArgs e)
    {
        InventoryItem existingItem = inventory.Find(item => item.collectibleSO.name == e.collectibleSO.name);
        bool isRare = (e.collectibleSO.name == "Cherry" || e.collectibleSO.name == "Rose");

        if (existingItem != null)
        {
            if (isRare)
            {
                if (existingItem.count < maxRareCollectible)
                {
                    existingItem.count++;
                    OnObjectPickUp?.Invoke(this, EventArgs.Empty);
                }
                return;
            }
            else
            {
                if (existingItem.count < maxCollectible)
                {
                    existingItem.count++;
                    OnObjectPickUp?.Invoke(this, EventArgs.Empty);
                }
                return;
            }
        }

        if (inventory.Count < 4 && existingItem == null)
        {
            inventory.Add(new InventoryItem(e.collectibleSO, 1));
            OnObjectPickUp?.Invoke(this, EventArgs.Empty);
            return;
        }
    }

    public int ExtractInventoryScore()
    {
        int scoreToAdd = 0;

        foreach (InventoryItem inventoryItem in inventory)
        {
            scoreToAdd += inventoryItem.collectibleSO.score * inventoryItem.count;
        }

        inventory.Clear();
        OnObjectPickUp?.Invoke(this, EventArgs.Empty);
        return scoreToAdd;
    }

    public bool CanPickUp(CollectibleSO collectibleSO, CollectibleType team)
    {
        InventoryItem existingItem = inventory.Find(item => item.collectibleSO.name == collectibleSO.name);
        bool isTeamMatch = (team == collectibleSO.collectibleType);
        Debug.Log($"TEAM: {team}");
        Debug.Log($"COLLECTIBLE TYPE: {collectibleSO.collectibleType}");
        Debug.Log(isTeamMatch);
        bool isRare = (collectibleSO.name == "Cherry" || collectibleSO.name == "Rose");
        Debug.Log(maxRareCollectible);
        Debug.Log(existingItem != null && isRare && existingItem.count < maxRareCollectible);

        if (existingItem != null)
        {
            if (isRare)
                return existingItem.count < maxRareCollectible;
            else
                return existingItem.count < maxCollectible;
        }

        if (!isTeamMatch)
        {
            return false;
        }

        return inventory.Count < 4;
    }
}

[System.Serializable]
public class InventoryItem
{
    public CollectibleSO collectibleSO;
    public int count;

    public InventoryItem(CollectibleSO collectible, int count)
    {
        this.collectibleSO = collectible;
        this.count = count;
    }
}
