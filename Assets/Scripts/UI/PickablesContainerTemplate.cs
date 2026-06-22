using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PickablesContainerTemplate : MonoBehaviour
{
    [SerializeField] private RawImage collectiblesImage;
    [SerializeField] private TextMeshProUGUI collectiblesCountTextMesh;

    private string collectibleName;

    public void SetCollectible(InventoryItem inventoryItem)
    {
        collectibleName = inventoryItem.collectibleSO.name;
        collectiblesImage.texture = inventoryItem.collectibleSO.collectibleImage;
        collectiblesCountTextMesh.text = inventoryItem.count.ToString();
    }
}
