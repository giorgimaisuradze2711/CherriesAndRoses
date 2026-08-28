using UnityEngine;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject hostJoinPanel;
    [SerializeField] private GameObject wardrobePanel;
    [SerializeField] private GameObject cloudImage;
    [SerializeField] private Button playButton;
    [SerializeField] private Button WardrobeButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button wardrobeBackButton;

    private void Awake()
    {
        playButton.onClick.AddListener(OnPlayClicked);
        WardrobeButton.onClick.AddListener(OnWardrobeClicked);
        quitButton.onClick.AddListener(OnQuitClicked);
        wardrobeBackButton.onClick.AddListener(OnWardrobeBackClicked);
    }

    private void OnPlayClicked()
    {
        mainMenuPanel.SetActive(false);
        hostJoinPanel.SetActive(true);
    }

    private void OnWardrobeClicked()
    {
        mainMenuPanel.SetActive(false);
        cloudImage.SetActive(false);
        wardrobePanel.SetActive(true);
    }

    private void OnQuitClicked()
    {
        Application.Quit();
    }

    private void OnWardrobeBackClicked()
    {
        wardrobePanel.SetActive(false);
        mainMenuPanel.SetActive(true);
        cloudImage.SetActive(true);
    }
}
