using System;
using UnityEngine;

public class PlayerDetector : MonoBehaviour
{
    public event EventHandler OnPlayerEnter;
    public event EventHandler OnPlayerExit;

    private void OnTriggerEnter(Collider other)
    {
        Player player = other.GetComponent<Player>();
        if (player != null)
        {
            Debug.Log($"Player Entered On {name}!");
            OnPlayerEnter?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Player player = other.GetComponent<Player>();

        if (player != null)
        {
            Debug.Log($"Player Exited On {name}!");
            OnPlayerExit?.Invoke(this, EventArgs.Empty);
        }
    }

    public void InvokePLayerExit()
    {
        OnPlayerExit?.Invoke(this, EventArgs.Empty);
    }
}
