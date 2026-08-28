using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

public class Holder : NetworkBehaviour
{
    // The holder owned by this client's own player, and an event for client-local systems
    // (score UI) that used to read a shared ScoreManager to instead react to their own
    // holder specifically - same shape as Player.LocalPlayer/OnLocalPlayerSpawned.
    public static Holder LocalHolder { get; private set; }
    public static event Action<Holder> OnLocalHolderSpawned;

    public event EventHandler<OnScoreChangedEventArgs> OnScoreChanged;
    public class OnScoreChangedEventArgs : EventArgs
    {
        public int score;
    }

    private NetworkVariable<int> score = new NetworkVariable<int>(0, writePerm: NetworkVariableWritePermission.Server);

    // Which of clothTextures this holder's basket shows - synced so every client (which each
    // instantiate their own local copy of the spawned prefab) renders the same color, not just
    // whichever the server happened to set locally. Mirrors Player.team's set-before-Spawn use.
    private NetworkVariable<int> clothColorIndex = new NetworkVariable<int>(0, writePerm: NetworkVariableWritePermission.Server);

    [SerializeField] private Basket basket;
    [SerializeField] private MeshRenderer basketClothRenderer;
    [SerializeField] private Texture2D[] clothTextures;
    [SerializeField] private GameObject wrongHolderIndicator;

    [SerializeField] private PlayerDetector climbUpInteractionArea;
    [SerializeField] private PlayerDetector climbDownInteractionArea;

    [SerializeField] private Vector3 climbUpPosition = new Vector3(0f, 1f, -1.5f);
    [SerializeField] private Vector3 climbDownPosition = new Vector3(0f, 10f, -1.5f);
    [SerializeField] private Vector3 RoofPosition = new Vector3(0f, 10.5f, 0f);

    private bool playerAtTop = false;
    private bool playerAtBottom = false;

    // Teleporting the player onto climbDownPosition/climbUpPosition to start a descent/ascent
    // can leave the CharacterController's capsule still overlapping the same trigger box that
    // auto-catches an arriving climber (their exact margin depends on collider tuning, not just
    // the target Y value) - which would immediately re-fire OnPlayerEnter and snap the player
    // right back. A time window (rather than a manually-cleared flag) marks "an enter event
    // this soon after our own teleport is that same spurious re-trigger, not a genuine
    // arrival" - it expires on its own even if no spurious re-trigger actually happens, so it
    // can never get stuck suppressing a later, real arrival (a plain latch could and did).
    private float topAutoCatchSuppressedUntil = -1f;
    private float bottomAutoCatchSuppressedUntil = -1f;
    private const float AutoCatchSuppressionWindow = 0.5f;

    void Awake()
    {
        climbUpInteractionArea.OnPlayerEnter += ClimbUpInteractionArea_OnPlayerEnter;
        climbUpInteractionArea.OnPlayerExit += ClimbUpInteractionArea_OnPlayerExit;

        climbDownInteractionArea.OnPlayerEnter += ClimbDownInteractionArea_OnPlayerEnter;
        climbDownInteractionArea.OnPlayerExit += ClimbDownInteractionArea_OnPlayerExit;
    }

    // Resolved via the singleton rather than a serialized field - unlike the single
    // scene-placed Holder this used to be, holders are now dynamically Instantiate'd from a
    // prefab (one per connecting player), and a prefab asset can't hold a valid Inspector
    // reference to a scene-only object like InputManager. Mirrors Player.cs.
    public override void OnNetworkSpawn()
    {
        InputManager.Instance.OnInteractPerformed += InputManager_OnInteractPerformed;
        score.OnValueChanged += HandleScoreChanged;
        ApplyClothColor();

        if (!IsOwner) return;

        LocalHolder = this;
        OnLocalHolderSpawned?.Invoke(this);
    }

    public void SetClothColorIndex(int index) => clothColorIndex.Value = index;

    private void ApplyClothColor()
    {
        if (basketClothRenderer == null || clothTextures == null || clothTextures.Length == 0) return;

        int index = clothColorIndex.Value % clothTextures.Length;
        basketClothRenderer.material.mainTexture = clothTextures[index];
    }

    private Coroutine wrongHolderIndicatorRoutine;
    private const float WrongHolderIndicatorDuration = 2f;

    private void ShowWrongHolderIndicator()
    {
        if (wrongHolderIndicator == null) return;

        if (wrongHolderIndicatorRoutine != null) StopCoroutine(wrongHolderIndicatorRoutine);
        wrongHolderIndicatorRoutine = StartCoroutine(WrongHolderIndicatorRoutine());
    }

    private IEnumerator WrongHolderIndicatorRoutine()
    {
        wrongHolderIndicator.SetActive(true);
        yield return new WaitForSeconds(WrongHolderIndicatorDuration);
        wrongHolderIndicator.SetActive(false);
        wrongHolderIndicatorRoutine = null;
    }

    public override void OnNetworkDespawn()
    {
        InputManager.Instance.OnInteractPerformed -= InputManager_OnInteractPerformed;
        score.OnValueChanged -= HandleScoreChanged;

        if (IsOwner && LocalHolder == this) LocalHolder = null;
    }

    private void HandleScoreChanged(int previousValue, int newValue)
    {
        OnScoreChanged?.Invoke(this, new OnScoreChangedEventArgs { score = newValue });
    }

    private void InputManager_OnInteractPerformed(object sender, System.EventArgs e)
    {
        Player player = Player.LocalPlayer;
        if (player == null) return;

        if (playerAtTop && !player.IsOnRope())
        {
            topAutoCatchSuppressedUntil = Time.time + AutoCatchSuppressionWindow;
            MovePlayerTo(player, climbDownPosition, !player.IsOnRope());
            playerAtTop = false;
            player.transform.eulerAngles = new Vector3(player.transform.eulerAngles.x, 0f, player.transform.eulerAngles.z);
            climbDownInteractionArea.InvokePLayerExit(player);
            Debug.Log($"Player On Climb Down Position! {climbDownPosition}");
        }
        else if (playerAtBottom && !player.IsOnRope())
        {
            bottomAutoCatchSuppressedUntil = Time.time + AutoCatchSuppressionWindow;
            MovePlayerTo(player, climbUpPosition, !player.IsOnRope());
            playerAtBottom = false;
            player.transform.eulerAngles = new Vector3(player.transform.eulerAngles.x, 0f, player.transform.eulerAngles.z);
            climbUpInteractionArea.InvokePLayerExit(player);
            Debug.Log($"Player On Climb Up Position! {climbUpPosition}");
        }
    }

    private void ClimbDownInteractionArea_OnPlayerExit(object sender, Player player)
    {
        if (player != Player.LocalPlayer) return;

        playerAtTop = false;
        Debug.Log("Player Exited Climb Down Area!");
    }

    private void ClimbDownInteractionArea_OnPlayerEnter(object sender, Player player)
    {
        if (player != Player.LocalPlayer) return;

        playerAtTop = true;

        if (Time.time < topAutoCatchSuppressedUntil) return;

        Debug.Log("Player Entered Climb Up Area!");

        if (player.IsOnRope())
        {
            MovePlayerTo(player, RoofPosition, !player.IsOnRope());
            DepositInventory(player);
            Debug.Log("Player On Roof Top!");
        }
    }

    private void ClimbUpInteractionArea_OnPlayerExit(object sender, Player player)
    {
        if (player != Player.LocalPlayer) return;

        playerAtBottom = false;
        Debug.Log("Player Exited Climb Up Area!");
    }

    private void ClimbUpInteractionArea_OnPlayerEnter(object sender, Player player)
    {
        if (player != Player.LocalPlayer) return;

        playerAtBottom = true;

        if (Time.time < bottomAutoCatchSuppressedUntil) return;

        Debug.Log("Player Entered Climb Down Area!");

        if (player.IsOnRope())
        {
            MovePlayerTo(player, climbUpPosition, !player.IsOnRope());
            Debug.Log("Player On CLimb Up Top!");
        }
    }

    // Only the owning player's deposits count - climbing someone else's holder still works
    // (traversal isn't restricted), it just never scores.
    private void DepositInventory(Player player)
    {
        if (player.OwnerClientId != OwnerClientId)
        {
            ShowWrongHolderIndicator();
            return;
        }

        InventoryManager inventoryManager = player.GetComponent<InventoryManager>();
        int scoreToAdd = inventoryManager.ExtractInventoryScore();
        RequestAddScoreServerRpc(scoreToAdd);
    }

    // RequireOwnership is false because the requesting client isn't necessarily this
    // NetworkObject's owner in the Netcode sense during the RPC call itself - but the sender
    // is re-checked against OwnerClientId here, server-side, so a modified client can't forge
    // a deposit into someone else's holder even though the client-side check above also exists.
    [ServerRpc(RequireOwnership = false)]
    private void RequestAddScoreServerRpc(int scoreToAdd, ServerRpcParams rpcParams = default)
    {
        if (rpcParams.Receive.SenderClientId != OwnerClientId) return;

        score.Value += scoreToAdd;
        ScoreManager.Instance.ReportScore(OwnerClientId, score.Value);
    }

    private void MovePlayerTo(Player player, Vector3 position, bool isOnRope)
    {
        Vector3 worldPosition = transform.TransformPoint(position);

        CharacterController characterController = player.GetComponent<CharacterController>();

        if (characterController != null)
        {
            player.SetOnRope(isOnRope);
            characterController.enabled = false;
            player.transform.position = worldPosition;
            characterController.enabled = true;
            Debug.Log($"Player Moved To Position {worldPosition}");
        }
    }
}
