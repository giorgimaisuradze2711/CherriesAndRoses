using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

// Spawns one Holder per connected client, each owned by that client and placed at a
// predefined HolderSpawnPoint - lives in-scene (like ObjectSpawner/ScoreManager) rather than
// on the persistent NetworkBootstrap, so its holderSpawnPoints list can reference other
// objects in this same gameplay scene directly instead of needing a cross-scene reference.
public class HolderSpawner : NetworkBehaviour
{
    [SerializeField] private GameObject holderPrefab;
    [SerializeField] private List<HolderSpawnPoint> holderSpawnPoints;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        NetworkManager.SceneManager.OnLoadEventCompleted += HandleSceneLoadCompleted;
    }

    public override void OnNetworkDespawn()
    {
        if (NetworkManager != null && NetworkManager.SceneManager != null)
        {
            NetworkManager.SceneManager.OnLoadEventCompleted -= HandleSceneLoadCompleted;
        }
    }

    private void HandleSceneLoadCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        NetworkManager.SceneManager.OnLoadEventCompleted -= HandleSceneLoadCompleted;

        if (holderPrefab == null || holderSpawnPoints == null || holderSpawnPoints.Count == 0) return;

        if (holderSpawnPoints.Count < clientsCompleted.Count)
        {
            Debug.LogWarning($"[HolderSpawner] Only {holderSpawnPoints.Count} holderSpawnPoints configured for {clientsCompleted.Count} connecting clients - some holders will reuse spawn points.");
        }

        for (int i = 0; i < clientsCompleted.Count; i++)
        {
            ulong clientId = clientsCompleted[i];
            HolderSpawnPoint spawnPoint = holderSpawnPoints[i % holderSpawnPoints.Count];

            GameObject holderInstance = Instantiate(holderPrefab, spawnPoint.transform.position, spawnPoint.transform.rotation);
            holderInstance.GetComponent<Holder>().SetClothColorIndex(i);
            holderInstance.GetComponent<NetworkObject>().SpawnWithOwnership(clientId, true);
        }
    }
}
