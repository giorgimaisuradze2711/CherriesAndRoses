using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class Collectible : NetworkBehaviour
{
    [SerializeField] public CollectibleSO collectibleSO;
    private float regrowTime = 10f;
    [SerializeField] private float scaleUpDuration = 0.5f;

    private NetworkVariable<bool> isPicked = new NetworkVariable<bool>(false);
    private Vector3 _originalScale;

    public event Action<Collectible> OnPickedUpConfirmed;

    private void Awake()
    {
        _originalScale = transform.localScale;
    }

    public override void OnNetworkSpawn()
    {
        isPicked.OnValueChanged += OnIsPickedChanged;

        if (isPicked.Value)
            gameObject.SetActive(false);
    }

    public override void OnNetworkDespawn()
    {
        isPicked.OnValueChanged -= OnIsPickedChanged;
    }

    private void OnIsPickedChanged(bool previousValue, bool newValue)
    {
        if (newValue)
        {
            Debug.Log($"Player Has Picked Up {collectibleSO.collectibleName}!");
            gameObject.SetActive(false);
        }
        else
        {
            gameObject.SetActive(true);
            StartCoroutine(ScaleUpRoutine());
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestPickUpServerRpc(ServerRpcParams rpcParams = default)
    {
        if (isPicked.Value) return;

        isPicked.Value = true;
        Invoke(nameof(Regrow), regrowTime);

        ulong senderClientId = rpcParams.Receive.SenderClientId;

        // Fire the cross-player chase-trigger notification here, server-side, rather than
        // from the client-targeted confirmation below - Chaser's AI only runs on the
        // server, so it needs this raised on the server regardless of which client (host
        // or a remote client) requested the pickup.
        if (NetworkManager.ConnectedClients.TryGetValue(senderClientId, out NetworkClient client) && client.PlayerObject != null)
        {
            Player requestingPlayer = client.PlayerObject.GetComponent<Player>();
            Player.NotifyPickedUp(requestingPlayer, this);
        }

        ConfirmPickUpClientRpc(new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { senderClientId } }
        });
    }

    [ClientRpc]
    private void ConfirmPickUpClientRpc(ClientRpcParams rpcParams = default)
    {
        OnPickedUpConfirmed?.Invoke(this);
    }

    public void CancelAutoRegrow() => CancelInvoke(nameof(Regrow));

    public void RegrowNow()
    {
        if (!isPicked.Value) return;
        CancelInvoke(nameof(Regrow));
        Regrow();
    }

    private void Regrow()
    {
        isPicked.Value = false;
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
