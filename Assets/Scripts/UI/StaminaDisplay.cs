using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StaminaDisplay : MonoBehaviour
{
    [SerializeField] private Image staminaBarFill;
    [SerializeField] private TextMeshProUGUI teamTextMesh;

    [Header("Visibility")]
    [SerializeField] private float fadeInSpeed = 5f;
    [SerializeField] private float fadeOutSpeed = 2f;
    [SerializeField] private float hideDelay = 1.5f;

    [Header("Rainbow Gradient")]
    [SerializeField] private Gradient staminaGradient;

    private Player player;

    private void Start()
    {
        if (Player.LocalPlayer != null)
            HookPlayer(Player.LocalPlayer);
        else
            Player.OnLocalPlayerSpawned += HookPlayer;
    }

    private void OnDestroy()
    {
        Player.OnLocalPlayerSpawned -= HookPlayer;

        if (player != null)
        {
            player.OnStaminaChanged -= Player_OnStaminaChanged;
            player.OnPlayerTeamChoose -= Player_OnPlayerTeamChoose;
        }
    }

    private void HookPlayer(Player localPlayer)
    {
        Player.OnLocalPlayerSpawned -= HookPlayer;

        player = localPlayer;
        player.OnStaminaChanged += Player_OnStaminaChanged;
        player.OnPlayerTeamChoose += Player_OnPlayerTeamChoose;

        // OnPlayerTeamChoose already fired once during OnNetworkSpawn, before we could
        // subscribe, so set the initial text directly from the player's current team.
        teamTextMesh.text = $"Team: {player.team.Value}";
    }

    private void Player_OnPlayerTeamChoose(object sender, Player.OnPlayerTeamChooseEventArgs e)
    {
        teamTextMesh.text = $"Team: {e.playerTeam}"; ;
    }

    private void Player_OnStaminaChanged(object sender, Player.OnStaminaChangedEventArgs e)
    {
        staminaBarFill.fillAmount = e.staminaNormalized;
        staminaBarFill.color = staminaGradient.Evaluate(e.staminaNormalized);
    }
}
