using TMPro;
using UnityEngine;

public class ScoreDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI victoryTextMesh;   
    [SerializeField] private TextMeshProUGUI scoreTextMesh;

    private void Awake()
    {
        victoryTextMesh.gameObject.SetActive(false);
    }

    void Start()
    {
        ScoreManager.Instance.OnAddScore += Instance_OnAddScore;
        ScoreManager.Instance.OnVictory += Instance_OnVictory;
    }

    private void Instance_OnVictory(object sender, System.EventArgs e)
    {
        victoryTextMesh.gameObject.SetActive(true);
    }

    private void Instance_OnAddScore(object sender, ScoreManager.OnAddScoreEventArgs e)
    {
        scoreTextMesh.text = e.score.ToString();
    }
}
