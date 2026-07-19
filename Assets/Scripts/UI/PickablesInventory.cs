using UnityEngine;

public class PickablesInventory : MonoBehaviour
{
    [SerializeField] private Transform pickablesGrid;
    [SerializeField] private Transform pickableContainerTemplate;

    private InventoryManager inventoryManager;

    private void Start()
    {
        if (Player.LocalPlayer != null)
            HookPlayer(Player.LocalPlayer);
        else
            Player.OnLocalPlayerSpawned += HookPlayer;
    }

    private void OnDestroy()
    {
        Player.OnLocalPlayerSpawned -= HookPlayer;

        if (inventoryManager != null)
            inventoryManager.OnObjectPickUp -= InventoryManager_OnObjectPickUp;
    }

    private void HookPlayer(Player player)
    {
        Player.OnLocalPlayerSpawned -= HookPlayer;

        inventoryManager = player.GetComponent<InventoryManager>();
        inventoryManager.OnObjectPickUp += InventoryManager_OnObjectPickUp;
        UpdateVisual();
    }

    private void InventoryManager_OnObjectPickUp(object sender, System.EventArgs e)
    {
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        foreach (Transform child in pickablesGrid)
        {
            if (child == pickableContainerTemplate) continue;
            Destroy(child.gameObject);
        }

        foreach (InventoryItem inventoryItem in inventoryManager.GetInventory())
        {
            Transform recipeTransform = Instantiate(pickableContainerTemplate, pickablesGrid);
            recipeTransform.gameObject.SetActive(true);
            recipeTransform.GetComponent<PickablesContainerTemplate>().SetCollectible(inventoryItem);
        }
    }
}
