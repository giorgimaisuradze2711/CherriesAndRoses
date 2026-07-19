using System;
using UnityEngine;

public class PlayerDetector : MonoBehaviour
{
    public event EventHandler<Player> OnPlayerEnter;
    public event EventHandler<Player> OnPlayerExit;

    private void OnTriggerEnter(Collider other)
    {
        Player player = other.GetComponent<Player>();
        if (player != null)
        {
            Debug.Log($"Player Entered On {name}!");
            OnPlayerEnter?.Invoke(this, player);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Player player = other.GetComponent<Player>();

        if (player != null)
        {
            Debug.Log($"Player Exited On {name}!");
            OnPlayerExit?.Invoke(this, player);
        }
    }

    public void InvokePLayerExit(Player player)
    {
        OnPlayerExit?.Invoke(this, player);
    }
}
