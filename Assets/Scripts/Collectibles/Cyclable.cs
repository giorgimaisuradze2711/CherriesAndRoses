using UnityEngine;
using System.Collections;

public class Cyclable : Collectible
{
    [SerializeField] CollectibleSO cycleCollectibleSO;
    [SerializeField] GameObject cycleGameObject;
    [SerializeField] Animation cyclableAnimation;

    void Start()
    {
        CycleManager.Instance.OnCycleChange += OnCycleChange;
        cycleGameObject.SetActive(false);
    }

    private void OnCycleChange(object sender, System.EventArgs e)
    {
        CollectibleSO currentCollectibleSo = collectibleSO;
        collectibleSO = cycleCollectibleSO;
        cycleCollectibleSO = currentCollectibleSo;

        bool wasActive = cycleGameObject.activeSelf;

        if (!wasActive)
        {
            cycleGameObject.SetActive(true);

            cyclableAnimation["Popup"].speed = 1f;
            cyclableAnimation["Popup"].time = 0f;
            cyclableAnimation.Play("Popup");
        }
        else
        {
            StartCoroutine(PlayReverseThenDisable());
        }
    }

    private IEnumerator PlayReverseThenDisable()
    {
        AnimationState anim = cyclableAnimation["Popup"];

        anim.speed = -1f;
        anim.time = anim.length;

        cyclableAnimation.Play("Popup");

        yield return new WaitForSeconds(anim.length);

        cycleGameObject.SetActive(false);
    }
}