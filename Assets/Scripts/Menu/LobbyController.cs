using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyController : MonoBehaviour
{
    [SerializeField] private NetworkBootstrap networkBootstrap;
    [SerializeField] private TextMeshProUGUI joinCodeText;
    [SerializeField] private TextMeshProUGUI playerCountText;
    [SerializeField] private Button startButton;
    [SerializeField] private Button copyJoinCodeButton;

    [SerializeField] private string gameplaySceneName = "SampleScene";

    private void OnEnable()
    {
        bool isHost = NetworkManager.Singleton.IsHost;

        startButton.gameObject.SetActive(isHost);
        joinCodeText.gameObject.SetActive(isHost);
        copyJoinCodeButton.gameObject.SetActive(isHost);

        if (isHost)
        {
            Debug.Log($"[Lobby] networkBootstrap={networkBootstrap} LastJoinCode='{networkBootstrap.LastJoinCode}'");
            joinCodeText.text = $"Join Code: {networkBootstrap.LastJoinCode}";
            startButton.onClick.AddListener(OnStartClicked);
            copyJoinCodeButton.onClick.AddListener(OnCopyJoinCodeClicked);
        }

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectionChanged;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientConnectionChanged;

        UpdatePlayerCount();
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton == null) return;

        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectionChanged;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientConnectionChanged;

        if (NetworkManager.Singleton.IsHost)
        {
            startButton.onClick.RemoveListener(OnStartClicked);
            copyJoinCodeButton.onClick.RemoveListener(OnCopyJoinCodeClicked);
        }
    }

    private void OnCopyJoinCodeClicked()
    {
        GUIUtility.systemCopyBuffer = networkBootstrap.LastJoinCode;
    }

    private void OnClientConnectionChanged(ulong clientId) => UpdatePlayerCount();

    private void UpdatePlayerCount()
    {
        playerCountText.text = $"Players: {NetworkManager.Singleton.ConnectedClientsIds.Count}";
    }

    private void OnStartClicked()
    {
        NetworkManager.Singleton.SceneManager.LoadScene(gameplaySceneName, LoadSceneMode.Single);
    }
}
