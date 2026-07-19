using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HostJoinMenuController : MonoBehaviour
{
    [SerializeField] private NetworkBootstrap networkBootstrap;

    [SerializeField] private GameObject hostJoinPanel;
    [SerializeField] private GameObject lobbyPanel;

    [SerializeField] private Button hostButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private TMP_InputField joinCodeInputField;
    [SerializeField] private TextMeshProUGUI statusText;

    private void Awake()
    {
        hostButton.onClick.AddListener(OnHostClicked);
        joinButton.onClick.AddListener(OnJoinClicked);
        joinCodeInputField.onValueChanged.AddListener(OnJoinCodeChanged);
        networkBootstrap.OnConnectionFailed += OnConnectionFailed;

        joinButton.interactable = !string.IsNullOrWhiteSpace(joinCodeInputField.text);
    }

    private void OnDestroy()
    {
        networkBootstrap.OnConnectionFailed -= OnConnectionFailed;
    }

    private void OnJoinCodeChanged(string value)
    {
        joinButton.interactable = !string.IsNullOrWhiteSpace(value);
    }

    private void OnConnectionFailed(string message)
    {
        SetInteractable(true);
        statusText.text = message;
    }

    private async void OnHostClicked()
    {
        SetInteractable(false);
        statusText.text = "Starting host...";

        string joinCode = await networkBootstrap.HostAsync();
        if (string.IsNullOrEmpty(joinCode))
        {
            SetInteractable(true);
            return;
        }

        hostJoinPanel.SetActive(false);
        lobbyPanel.SetActive(true);
    }

    private async void OnJoinClicked()
    {
        SetInteractable(false);
        statusText.text = "Joining...";

        bool joined = await networkBootstrap.JoinAsync(joinCodeInputField.text.Trim());
        if (!joined)
        {
            SetInteractable(true);
            return;
        }

        hostJoinPanel.SetActive(false);
        lobbyPanel.SetActive(true);
    }

    private void SetInteractable(bool interactable)
    {
        hostButton.interactable = interactable;
        joinButton.interactable = interactable && !string.IsNullOrWhiteSpace(joinCodeInputField.text);
    }
}
