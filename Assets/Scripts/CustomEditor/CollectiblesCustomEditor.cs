using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CollectibleSO))]
public class CollectibleEditor : Editor
{
    public override void OnInspectorGUI()
    {
        CollectibleSO collectible = (CollectibleSO)target;
        collectible.collectibleType = (CollectibleType)EditorGUILayout.EnumPopup("Collectible Type", collectible.collectibleType);
        collectible.collectibleImage = (Texture2D)EditorGUILayout.ObjectField("Collectible Image", collectible.collectibleImage, typeof(Texture2D), false);

        if (collectible.collectibleType == CollectibleType.Flower)
        {
            collectible.flowerName = (FlowerName)EditorGUILayout.EnumPopup("Flower Name", collectible.flowerName);
        }
        else if (collectible.collectibleType == CollectibleType.Fruit)
        {
            collectible.fruitName = (FruitName)EditorGUILayout.EnumPopup("Fruit Name", collectible.fruitName);
        }

        if (collectible.collectibleType == CollectibleType.Flower)
        {
            collectible.collectibleName = collectible.flowerName.ToString();
        }
        else if (collectible.collectibleType == CollectibleType.Fruit)
        {
            collectible.collectibleName = collectible.fruitName.ToString();
        }

        collectible.score = EditorGUILayout.IntField("Score", collectible.score);

        EditorUtility.SetDirty(collectible);
    }
}
