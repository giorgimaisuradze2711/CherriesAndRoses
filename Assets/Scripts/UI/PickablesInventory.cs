using UnityEngine;

public class PickablesInventory : MonoBehaviour
{
    [SerializeField] private InventoryManager inventoryManager;

    [SerializeField] private Transform pickablesGrid;
    [SerializeField] private Transform pickableContainerTemplate;

    private void Awake()
    {
        inventoryManager.OnObjectPickUp += InventoryManager_OnObjectPickUp;
    }

    private void InventoryManager_OnObjectPickUp(object sender, System.EventArgs e)
    {
        UpdateVisual();
    }

    void Start()
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

        foreach (InventoryItem inventoryItem in InventoryManager.Instance.GetInventory())
        {
            Transform recipeTransform = Instantiate(pickableContainerTemplate, pickablesGrid);
            recipeTransform.gameObject.SetActive(true);
            recipeTransform.GetComponent<PickablesContainerTemplate>().SetCollectible(inventoryItem);
        }
    }
}
