using UnityEngine;

[CreateAssetMenu(fileName = "Collectibles", menuName = "Scriptable Objects/Collectibles")]
public class CollectibleSO : ScriptableObject
{
    public CollectibleType collectibleType;
    public Texture2D collectibleImage;
    public string collectibleName;
    public int score;

    public FlowerName flowerName;
    public FruitName fruitName;
}
