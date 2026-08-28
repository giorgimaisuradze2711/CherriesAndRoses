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
        if (Holder.LocalHolder != null)
            HookHolder(Holder.LocalHolder);
        else
            Holder.OnLocalHolderSpawned += HookHolder;

        ScoreManager.Instance.OnVictory += Instance_OnVictory;
    }

    private void OnDestroy()
    {
        Holder.OnLocalHolderSpawned -= HookHolder;

        if (holder != null)
            holder.OnScoreChanged -= Holder_OnScoreChanged;
    }

    private Holder holder;

    private void HookHolder(Holder holder)
    {
        Holder.OnLocalHolderSpawned -= HookHolder;

        this.holder = holder;
        holder.OnScoreChanged += Holder_OnScoreChanged;
    }

    private void Holder_OnScoreChanged(object sender, Holder.OnScoreChangedEventArgs e)
    {
        scoreTextMesh.text = e.score.ToString();
    }

    private void Instance_OnVictory(object sender, ScoreManager.OnVictoryEventArgs e)
    {
        victoryTextMesh.gameObject.SetActive(true);
    }
}
