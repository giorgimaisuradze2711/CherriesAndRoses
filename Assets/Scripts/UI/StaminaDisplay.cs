using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StaminaDisplay : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private Image staminaBarFill;
    [SerializeField] private TextMeshProUGUI teamTextMesh;

    [Header("Visibility")]
    [SerializeField] private float fadeInSpeed = 5f;
    [SerializeField] private float fadeOutSpeed = 2f;
    [SerializeField] private float hideDelay = 1.5f;

    [Header("Rainbow Gradient")]
    [SerializeField] private Gradient staminaGradient;

    private void Start()
    {
        player.OnStaminaChanged += Player_OnStaminaChanged;
        player.OnPlayerTeamChoose += Player_OnPlayerTeamChoose; ;
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
