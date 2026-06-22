using System;
using Unity.VisualScripting;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }
    private int currentScore = 0;
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

    public void AddScore(int addedScore)
    {
        currentScore += addedScore;

        OnAddScore?.Invoke(this, new OnAddScoreEventArgs
        {
            score = currentScore
        });

        if(currentScore >= victoryScore)
        {
            OnVictory?.Invoke(this, EventArgs.Empty);
        }

        Debug.Log($"Current Score Is {currentScore}!");
    }
}
