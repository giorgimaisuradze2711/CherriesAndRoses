using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

// Spawns a fixed, manually-configured number of prefabs into designated SpawnZones once,
// when the (in-scene placed) NetworkObject this sits on comes online for the server -
// same "runs once on the server, replicates to everyone" pattern as CycleManager/ScoreManager.
public class ObjectSpawner : NetworkBehaviour
{
    [Serializable]
    private class SpawnEntry
    {
        public GameObject prefab;
        public List<SpawnZone> zones;
        public int count = 1;
    }

    [SerializeField] private List<SpawnEntry> spawnEntries;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        // Spawning dynamic NetworkObjects directly from this in-scene object's own OnNetworkSpawn
        // races the scene-load handshake: it fires while the scene transition is still finalizing
        // for remote clients, so the resulting spawn message can arrive mid-transition and get
        // destroyed by the client's own scene unload - throwing "Invalid Destroy" on the client and
        // a MissingReferenceException back on the host. Waiting for OnLoadEventCompleted (same
        // signal NetworkBootstrap uses for player spawning) guarantees every client has fully
        // finished loading the scene first.
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

        foreach (SpawnEntry entry in spawnEntries)
        {
            if (entry.prefab == null || entry.zones == null || entry.zones.Count == 0) continue;

            for (int i = 0; i < entry.count; i++)
            {
                // Round-robin so count is split as evenly as possible across the listed
                // zones instead of drifting unevenly the way independent random picks would.
                SpawnZone zone = entry.zones[i % entry.zones.Count];
                Vector3 position = zone.GetRandomPoint();
                GameObject instance = Instantiate(entry.prefab, position, entry.prefab.transform.rotation);
                instance.GetComponent<NetworkObject>().Spawn(true);
            }
        }
    }
}
