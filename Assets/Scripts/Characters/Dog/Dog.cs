using System;
using UnityEngine;

public class Dog : Chaser
{
    protected override void SubscribeToEvent()
    {
        Player.OnAnyBananaTaken += OnBananaTaken;
    }

    protected override void UnsubscribeFromEvent()
    {
        Player.OnAnyBananaTaken -= OnBananaTaken;
    }

    private void OnBananaTaken(Player player, Collectible collectible)
    {
        triggerCollectible = collectible;
        OnTriggerEvent(player);
    }
}
