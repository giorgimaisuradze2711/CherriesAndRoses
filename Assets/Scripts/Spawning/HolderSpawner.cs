using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// Spawns one Holder per connected client, each owned by that client and placed at a
// predefined HolderSpawnPoint - lives in-scene (like ObjectSpawner/ScoreManager) rather than
// on the persistent NetworkBootstrap, so its holderSpawnPoints list can reference other
// objects in this same gameplay scene directly instead of needing a cross-scene reference.
//
// Exposed as a singleton (mirrors ScoreManager/InputManager) rather than self-subscribing to
// OnLoadEventCompleted, so NetworkBootstrap can call SpawnHolders directly and synchronously
// before spawning players - two independent subscribers to the same scene-load event would
// fire in an unguaranteed order, which matters now that player spawn position depends on
// each client's holder already existing.
public class HolderSpawner : NetworkBehaviour
{
    public static HolderSpawner Instance { get; private set; }

    [SerializeField] private GameObject holderPrefab;
    [SerializeField] private List<HolderSpawnPoint> holderSpawnPoints;

    private void Awake()
    {
        Instance = this;
    }

    public Dictionary<ulong, Vector3> SpawnHolders(List<ulong> clientsCompleted)
    {
        var tipPositionsByClientId = new Dictionary<ulong, Vector3>();

        if (!IsServer) return tipPositionsByClientId;
        if (holderPrefab == null || holderSpawnPoints == null || holderSpawnPoints.Count == 0) return tipPositionsByClientId;

        if (holderSpawnPoints.Count < clientsCompleted.Count)
        {
            Debug.LogWarning($"[HolderSpawner] Only {holderSpawnPoints.Count} holderSpawnPoints configured for {clientsCompleted.Count} connecting clients - some holders will reuse spawn points.");
        }

        for (int i = 0; i < clientsCompleted.Count; i++)
        {
            ulong clientId = clientsCompleted[i];
            HolderSpawnPoint spawnPoint = holderSpawnPoints[i % holderSpawnPoints.Count];

            GameObject holderInstance = Instantiate(holderPrefab, spawnPoint.transform.position, spawnPoint.transform.rotation);
            Holder holder = holderInstance.GetComponent<Holder>();
            holder.SetClothColorIndex(i);
            holderInstance.GetComponent<NetworkObject>().SpawnWithOwnership(clientId, true);

            tipPositionsByClientId[clientId] = holder.GetPlayerSpawnWorldPosition();
        }

        return tipPositionsByClientId;
    }
}
