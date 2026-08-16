using UnityEngine;

public class SpeedBoostTrigger : MonoBehaviour
{
    [SerializeField] private PlayerDetector playerDetector;
    [SerializeField] private float speedMultiplier = 2f;
    [SerializeField] private float duration = 5f;

    private void Awake()
    {
        playerDetector.OnPlayerEnter += PlayerDetector_OnPlayerEnter;
    }

    private void PlayerDetector_OnPlayerEnter(object sender, Player player)
    {
        if (player != Player.LocalPlayer) return;

        player.ApplySpeedBoost(speedMultiplier, duration);
    }
}
