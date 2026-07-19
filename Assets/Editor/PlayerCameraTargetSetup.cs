#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// One-off setup tool: adds a "CameraTarget" child transform to the player prefab at a sensible
// chest/head height and wires it into Player.cameraTarget, so the gameplay camera looks at that
// point instead of the root transform (which sits at ground/feet level, since that's where
// CharacterController is centered from - looking at it directly angles the camera down at the
// player's feet instead of framing them naturally).
public static class PlayerCameraTargetSetup
{
    private const string PlayerPrefabPath = "Assets/Prefabs/Characters/Player/Girl.prefab";
    private const string CameraTargetName = "CameraTarget";
    private static readonly Vector3 CameraTargetLocalPosition = new Vector3(0f, 1.4f, 0f);

    [MenuItem("Tools/NGO/Add Camera Target To Player Prefab")]
    public static void AddCameraTarget()
    {
        var prefabRoot = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
        if (prefabRoot == null)
        {
            Debug.LogError($"[PlayerCameraTargetSetup] Could not load prefab at '{PlayerPrefabPath}'.");
            return;
        }

        try
        {
            var existing = prefabRoot.transform.Find(CameraTargetName);
            Transform cameraTargetTransform;
            if (existing != null)
            {
                cameraTargetTransform = existing;
                Debug.Log($"[PlayerCameraTargetSetup] '{CameraTargetName}' already exists on the prefab; reusing it.");
            }
            else
            {
                var cameraTargetGameObject = new GameObject(CameraTargetName);
                cameraTargetGameObject.transform.SetParent(prefabRoot.transform, false);
                cameraTargetGameObject.transform.localPosition = CameraTargetLocalPosition;
                cameraTargetGameObject.transform.localRotation = Quaternion.identity;
                cameraTargetTransform = cameraTargetGameObject.transform;
                Debug.Log($"[PlayerCameraTargetSetup] Created '{CameraTargetName}' at local position {CameraTargetLocalPosition}.");
            }

            var player = prefabRoot.GetComponent<Player>();
            if (player == null)
            {
                Debug.LogError("[PlayerCameraTargetSetup] Prefab root has no Player component; cannot wire cameraTarget.");
                return;
            }

            var serializedPlayer = new SerializedObject(player);
            var cameraTargetProperty = serializedPlayer.FindProperty("cameraTarget");
            if (cameraTargetProperty == null)
            {
                Debug.LogError("[PlayerCameraTargetSetup] Player component has no serialized 'cameraTarget' field. " +
                    "Make sure Assets/Scripts/Characters/Player/Player.cs has compiled with the new field first.");
                return;
            }

            cameraTargetProperty.objectReferenceValue = cameraTargetTransform;
            serializedPlayer.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, PlayerPrefabPath);
            Debug.Log($"[PlayerCameraTargetSetup] Saved '{PlayerPrefabPath}' with cameraTarget wired to '{CameraTargetName}'.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }
}
#endif
