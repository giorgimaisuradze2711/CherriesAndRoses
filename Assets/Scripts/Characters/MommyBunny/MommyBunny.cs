using System;
using UnityEngine;

public class MommyBunny : Chaser
{
    protected override void SubscribeToEvent()
    {
        Player.OnAnyBaloonTaken += OnBaloonTaken;
    }

    protected override void UnsubscribeFromEvent()
    {
        Player.OnAnyBaloonTaken -= OnBaloonTaken;
    }

    private void OnBaloonTaken(Player player) => OnTriggerEvent(player);
}
