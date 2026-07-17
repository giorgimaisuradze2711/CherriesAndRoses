using System.Collections;
using UnityEngine;

public class Collectible : MonoBehaviour
{
    [SerializeField] public CollectibleSO collectibleSO;
    private float regrowTime = 10f;
    [SerializeField] private float scaleUpDuration = 0.5f;

    private bool isPicked = false;
    private Vector3 _originalScale;

    private void Awake()
    {
        _originalScale = transform.localScale;
    }

    public void PickUp()
    {
        if (isPicked) return;

        Debug.Log($"Player Has Picked Up {collectibleSO.collectibleName}!");
        isPicked = true;
        gameObject.SetActive(false);
        Invoke(nameof(Regrow), regrowTime);
    }

    public void CancelAutoRegrow() => CancelInvoke(nameof(Regrow));

    public void RegrowNow()
    {
        if (!isPicked) return;
        CancelInvoke(nameof(Regrow));
        Regrow();
    }

    private void Regrow()
    {
        isPicked = false;
        gameObject.SetActive(true);
        StartCoroutine(ScaleUpRoutine());
    }

    private IEnumerator ScaleUpRoutine()
    {
        transform.localScale = Vector3.zero;
        float elapsed = 0f;

        while (elapsed < scaleUpDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / scaleUpDuration);
            transform.localScale = Vector3.Lerp(Vector3.zero, _originalScale, t);
            yield return null;
        }

        transform.localScale = _originalScale;
    }
}