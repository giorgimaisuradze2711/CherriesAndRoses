using System;
using Unity.Netcode;
using UnityEngine;

public class ScoreManager : NetworkBehaviour
{
    public static ScoreManager Instance { get; private set; }

    private NetworkVariable<int> currentScore = new NetworkVariable<int>(0);
    private int victoryScore = 10;

    public event EventHandler<OnAddScoreEventArgs> OnAddScore;
    public class OnAddScoreEventArgs : EventArgs
    {
        public int score;
    }

    public event EventHandler OnVictory;

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        currentScore.OnValueChanged += OnCurrentScoreChanged;
    }

    public override void OnNetworkDespawn()
    {
        currentScore.OnValueChanged -= OnCurrentScoreChanged;
    }

    private void OnCurrentScoreChanged(int previousValue, int newValue)
    {
        OnAddScore?.Invoke(this, new OnAddScoreEventArgs
        {
            score = newValue
        });

        if (newValue >= victoryScore)
        {
            OnVictory?.Invoke(this, EventArgs.Empty);
        }

        Debug.Log($"Current Score Is {newValue}!");
    }

    public void AddScore(int addedScore)
    {
        if (!IsServer) return;

        currentScore.Value += addedScore;
    }
}
