using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

// Host()/Join() go through Unity Relay only (no Lobby service) - the join code is shared
// out-of-band (read aloud, copy/paste) rather than looked up from a room list.
public class NetworkBootstrap : MonoBehaviour
{
    private const int MaxConnections = 4;

    // Every player was spawning at the exact same point (Instantiate with no position), so their
    // CharacterControllers overlapped the instant a second player spawned, shoving both of them
    // apart and visibly jolting their cameras. Spread each new spawn out along X instead.
    private const float PlayerSpawnSpacing = 2f;

    [SerializeField] private NetworkManager networkManager;
    [SerializeField] private UnityTransport transport;
    [SerializeField] private string gameplaySceneName = "Yard";
    [SerializeField] private GameObject girlPlayerPrefab;
    [SerializeField] private GameObject boyPlayerPrefab;

    public event Action<string> OnConnectionFailed;
    public string LastJoinCode { get; private set; }

    private async void Awake()
    {
        // By default NGO auto-spawns a player object for every approved connection right
        // away - including the host's own local connection during StartHost(), while still
        // in the MainMenu scene. That's too early: the gameplay scene (with ScoreManager,
        // CycleManager, Holder, collectibles, etc.) hasn't loaded yet. So connections are
        // approved without a player object, and players are spawned manually once the
        // gameplay scene actually finishes loading for everyone (see SubscribeToSceneEvents,
        // called after StartHost()/StartClient() below - NetworkManager.SceneManager isn't
        // valid to touch until the network has actually started).
        // Enforced here rather than relying solely on the Inspector checkbox staying
        // checked - without it, NGO ignores OnConnectionApproval below and auto-spawns
        // the player immediately on approval instead of waiting for SubscribeToSceneEvents.
        networkManager.NetworkConfig.ConnectionApproval = true;
        networkManager.ConnectionApprovalCallback += OnConnectionApproval;

        try
        {
            await EnsureSignedInAsync();
        }
        catch (Exception exception)
        {
            Debug.LogError($"UGS sign-in failed: {exception}");
            OnConnectionFailed?.Invoke("Couldn't sign in to Unity services.");
        }
    }

    // Each client's wardrobe pick is sent as connection payload (set on NetworkConfig.ConnectionData
    // before StartHost()/StartClient()) since it has to reach the server before any player object -
    // for this client - exists to carry it as a NetworkVariable.
    private readonly Dictionary<ulong, CharacterChoice> clientCharacterChoices = new();

    private void OnConnectionApproval(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        CharacterChoice choice = request.Payload != null && request.Payload.Length > 0
            ? (CharacterChoice)request.Payload[0]
            : CharacterChoice.Girl;
        clientCharacterChoices[request.ClientNetworkId] = choice;

        response.Approved = true;
        response.CreatePlayerObject = false;
    }

    private bool sceneEventsSubscribed;

    private void SubscribeToSceneEvents()
    {
        if (sceneEventsSubscribed) return;
        sceneEventsSubscribed = true;

        networkManager.SceneManager.OnLoadEventCompleted += OnGameplaySceneLoaded;
    }

    // Round-robin rather than random so two players are never on the same team unless the
    // lobby size exceeds the number of CollectibleType values (currently 2) - e.g. with 4
    // players: Fruit, Flower, Fruit, Flower.
    private int nextTeamIndex;

    private void OnGameplaySceneLoaded(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (!networkManager.IsServer) return;
        if (sceneName != gameplaySceneName) return;

        int teamCount = Enum.GetValues(typeof(CollectibleType)).Length;

        foreach (ulong clientId in clientsCompleted)
        {
            GameObject playerPrefab = clientCharacterChoices.TryGetValue(clientId, out CharacterChoice choice) && choice == CharacterChoice.Boy
                ? boyPlayerPrefab
                : girlPlayerPrefab;

            Vector3 spawnPosition = new Vector3(nextTeamIndex * PlayerSpawnSpacing, 0f, 0f);
            GameObject playerInstance = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
            playerInstance.GetComponent<Player>().team.Value = (CollectibleType)(nextTeamIndex % teamCount);
            playerInstance.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);

            nextTeamIndex++;
        }
    }

    private async Task EnsureSignedInAsync()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
            await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    public async Task<string> HostAsync()
    {
        try
        {
            await EnsureSignedInAsync();

            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(MaxConnections);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            LastJoinCode = joinCode;
            Debug.Log($"[NetworkBootstrap] Join code: {joinCode}");

            transport.SetRelayServerData(new RelayServerData(allocation, "dtls"));
            networkManager.NetworkConfig.ConnectionData = new byte[] { (byte)CharacterSelection.Local };
            networkManager.StartHost();
            SubscribeToSceneEvents();

            return joinCode;
        }
        catch (Exception exception)
        {
            Debug.LogError($"Hosting failed: {exception}");
            OnConnectionFailed?.Invoke("Couldn't host a game. Check your connection and try again.");
            return null;
        }
    }

    public async Task<bool> JoinAsync(string joinCode)
    {
        try
        {
            await EnsureSignedInAsync();

            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            transport.SetRelayServerData(new RelayServerData(joinAllocation, "dtls"));
            networkManager.NetworkConfig.ConnectionData = new byte[] { (byte)CharacterSelection.Local };
            networkManager.StartClient();

            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError($"Joining failed: {exception}");
            OnConnectionFailed?.Invoke("Couldn't join with that code. Double check it and try again.");
            return false;
        }
    }
}
