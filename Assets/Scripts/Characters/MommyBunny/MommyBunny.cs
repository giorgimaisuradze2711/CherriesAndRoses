using System;
using UnityEngine;

public class MommyBunny : Chaser
{
    protected override void SubscribeToEvent()
    {
        player.OnBaloonTaken += OnBaloonTaken;
    }

    protected override void UnsubscribeFromEvent()
    {
        player.OnBaloonTaken -= OnBaloonTaken;
    }

    private void OnBaloonTaken(object sender, EventArgs e) => OnTriggerEvent();
}