using System;
using Unity.Netcode;
using UnityEngine;

// Score itself now lives per-player, on each player's own Holder (Holder.score) - this
// singleton no longer holds any score, it only watches for a player crossing the victory
// threshold and broadcasts the result to every client.
public class ScoreManager : NetworkBehaviour
{
    public static ScoreManager Instance { get; private set; }

    private int victoryScore = 10;

    public event EventHandler<OnVictoryEventArgs> OnVictory;
    public class OnVictoryEventArgs : EventArgs
    {
        public ulong winningClientId;
    }

    private void Awake()
    {
        Instance = this;
    }

    public void ReportScore(ulong ownerClientId, int newScore)
    {
        if (!IsServer) return;

        Debug.Log($"Client {ownerClientId} Score Is {newScore}!");

        if (newScore >= victoryScore)
        {
            AnnounceVictoryClientRpc(ownerClientId);
        }
    }

    [ClientRpc]
    private void AnnounceVictoryClientRpc(ulong winningClientId)
    {
        OnVictory?.Invoke(this, new OnVictoryEventArgs { winningClientId = winningClientId });
    }
}
