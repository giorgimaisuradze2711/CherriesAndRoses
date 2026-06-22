using UnityEngine;

public class InteractionPrompt : MonoBehaviour
{
    [SerializeField] private PlayerDetector playerDetector;
    [SerializeField] private GameObject button;

    private void Awake()
    {
        playerDetector.OnPlayerEnter += PlayerDetector_OnPlayerEnter;
        playerDetector.OnPlayerExit += PlayerDetector_OnPlayerExit;
    }

    void Start()
    {
        button.SetActive(false);
    }

    private void PlayerDetector_OnPlayerExit(object sender, System.EventArgs e)
    {
        Debug.Log("PLAYER LEAVE TRIGGER SET!");
        button.SetActive(false);
    }

    private void PlayerDetector_OnPlayerEnter(object sender, System.EventArgs e)
    {
        Debug.Log("PLAYER ENTER TRIGGER SET!");
        button.SetActive(true);
    }
}
