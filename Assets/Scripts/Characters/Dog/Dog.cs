using System;
using UnityEngine;

public class Dog : Chaser
{
    protected override void SubscribeToEvent()
    {
        player.OnBananaTaken += OnBananaTaken;
    }

    protected override void UnsubscribeFromEvent()
    {
        player.OnBananaTaken -= OnBananaTaken;
    }

    private void OnBananaTaken(object sender, Player.OnBananaTakenEventArgs e)
    {
        triggerCollectible = e.collectible;
        OnTriggerEvent();
    }
}
