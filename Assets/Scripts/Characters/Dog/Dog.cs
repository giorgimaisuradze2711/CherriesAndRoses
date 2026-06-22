using System;
using UnityEngine;

public class NewMonoBehaviourScript : Chaser
{
    protected override void SubscribeToEvent()
    {
        player.OnBananaTaken += OnBananaTaken;
    }

    protected override void UnsubscribeFromEvent()
    {
        player.OnBananaTaken -= OnBananaTaken;
    }

    private void OnBananaTaken(object sender, EventArgs e) => OnTriggerEvent();
}
