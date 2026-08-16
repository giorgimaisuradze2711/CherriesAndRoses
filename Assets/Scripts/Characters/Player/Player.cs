using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using static UnityEngine.Rendering.DebugUI;

public class Player : NetworkBehaviour
{
    public event EventHandler OnObjectPickUpAnimate;
    public event EventHandler OnObjectPickDownAnimate;
    public event EventHandler<OnObjectPickUpEventArgs> OnObjectPickUp;
    public class OnObjectPickUpEventArgs : EventArgs
    {
        public CollectibleSO collectibleSO;
    }

    public event EventHandler OnStunned;
    public event EventHandler OnStunRecovered;
    public event EventHandler OnBaloonTaken;
    public event EventHandler<OnBananaTakenEventArgs> OnBananaTaken;
    public class OnBananaTakenEventArgs : EventArgs
    {
        public Collectible collectible;
    }

    public event EventHandler<OnStaminaChangedEventArgs> OnStaminaChanged;
    public class OnStaminaChangedEventArgs : EventArgs
    {
        public float staminaNormalized; // 0 to 1
    }
    public event EventHandler OnStartedRunning;
    public event EventHandler OnStoppedRunning;
    public event EventHandler<OnPlayerTeamChooseEventArgs> OnPlayerTeamChoose;
    public class OnPlayerTeamChooseEventArgs : EventArgs
    {
        public string playerTeam;
    }

    // Local player of this client, and cross-player events so AI/UI that used to hold one
    // hardcoded Player reference can react to whichever player actually triggered them.
    public static Player LocalPlayer { get; private set; }
    public static event Action<Player> OnLocalPlayerSpawned;
    public static event Action<Player> OnAnyBaloonTaken;
    public static event Action<Player, Collectible> OnAnyBananaTaken;

    private const string ROPE_TAG = "Rope";

    // Assigned by the server (NetworkBootstrap.OnGameplaySceneLoaded) before this object
    // spawns, so every client - including the owner - gets the authoritative value as part
    // of the initial spawn state, rather than each client rolling its own independently.
    public NetworkVariable<CollectibleType> team = new NetworkVariable<CollectibleType>(writePerm: NetworkVariableWritePermission.Server);

    [SerializeField] private InventoryManager inventoryManager;

    [SerializeField] private PlayerAnimator playerAnimator;
    [SerializeField] private CharacterController characterController;

    // Where the camera looks/follows - roughly chest/head height. Falls back to the root
    // transform (feet level, since that's where CharacterController is centered from) if never
    // assigned, so this never breaks existing setups that haven't added the child transform yet.
    [SerializeField] private Transform cameraTarget;

    [SerializeField] private float movementSpeed = 5f;
    [SerializeField] private float runSpeed = 9f;
    [SerializeField] private float rotateSpeed = 10f;

    [Header("Stamina")]
    [SerializeField] private float maxStamina = 5f;
    [SerializeField] private float staminaDrainRate = 1f;
    [SerializeField] private float staminaRegenRate = 0.5f;
    [SerializeField] private float staminaRegenDelay = 1.5f;

    [SerializeField] private LayerMask pickableLayer;
    [SerializeField] private Vector3 boxSize = new Vector3(1f, 1f, 1f);
    [SerializeField] private float detectionDistance = 1.5f;

    Collectible currentPickable;

    private bool isPerformingInteraction = false;
    private bool hasRope = false;
    private bool isOnRope = false;

    private bool isWalking = false;
    private bool isRunning = false;

    private float currentStamina;
    private float staminaRegenDelayTimer = 0f;
    private bool isStaminaExhausted = false;

    private bool isStunned = false;
    private Coroutine stunCoroutine;

    private float speedMultiplier = 1f;
    private Coroutine speedBoostCoroutine;

    public bool IsOnRope() => isOnRope;
    public bool IsWalking() => isWalking;
    public bool IsRunning() => isRunning;

    public bool IsStunned() => isStunned;
    public float GetStaminaNormalized() => currentStamina / maxStamina;

    public override void OnNetworkSpawn()
    {
        currentStamina = maxStamina;

        if (!IsOwner) return;

        LocalPlayer = this;

        Debug.Log($"TEAM: {team.Value}");

        OnPlayerTeamChoose?.Invoke(this, new OnPlayerTeamChooseEventArgs
        {
            playerTeam = team.Value.ToString()
        });

        // Read via the static singleton, not a serialized field: Girl is a prefab spawned at
        // runtime, and a prefab asset can't hold a valid Inspector reference to a scene-only
        // object like InputManager (it would always come back null on the spawned instance).
        InputManager.Instance.EnablePlayerInputs();
        InputManager.Instance.OnInteractPerformed += InputManager_OnInteractPerformed;
        playerAnimator.OnInteractAnimationFinished += PlayerAnimator_OnInteractAnimationFinished;

        // The scene's single Cinemachine camera and CameraObstructionFade used to have their
        // Follow/LookAt/target hardcoded in the Inspector to the one placed Girl instance. That
        // instance no longer exists in multiplayer (players are spawned per connection), so point
        // them at whichever player is actually local on this client instead. The camera can be
        // momentarily inactive (loading fade, not yet enabled this frame) right when this player
        // spawns, so this retries for a short window instead of trying exactly once and silently
        // giving up. Also re-run on every future scene load, in case this same player object ever
        // persists across a scene transition without OnNetworkSpawn firing again.
        WireLocalCamera();
        NetworkManager.SceneManager.OnLoadEventCompleted += HandleSceneLoadCompleted;

        OnLocalPlayerSpawned?.Invoke(this);
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) return;

        if (LocalPlayer == this) LocalPlayer = null;

        InputManager.Instance.OnInteractPerformed -= InputManager_OnInteractPerformed;
        playerAnimator.OnInteractAnimationFinished -= PlayerAnimator_OnInteractAnimationFinished;
        NetworkManager.SceneManager.OnLoadEventCompleted -= HandleSceneLoadCompleted;

        if (wireCameraRoutine != null)
        {
            StopCoroutine(wireCameraRoutine);
            wireCameraRoutine = null;
        }
    }

    private void HandleSceneLoadCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        WireLocalCamera();
    }

    private Coroutine wireCameraRoutine;

    private void WireLocalCamera()
    {
        if (wireCameraRoutine != null)
        {
            StopCoroutine(wireCameraRoutine);
        }
        wireCameraRoutine = StartCoroutine(WireLocalCameraRoutine());
    }

    // Retries for a short window instead of trying once and silently giving up - the camera can
    // be momentarily inactive (FindFirstObjectByType only matches active objects by default) or
    // not yet present at the exact instant this player object spawns.
    private IEnumerator WireLocalCameraRoutine()
    {
        const int maxAttempts = 60; // roughly one second at 60fps

        // Both Follow and LookAt target the root transform directly - camera position stays at
        // exactly the configured FollowOffset relative to the player, and the camera looks
        // straight at the player root with no elevation.
        Transform lookAtTarget = transform;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            CinemachineCamera virtualCamera = FindFirstObjectByType<CinemachineCamera>(FindObjectsInactive.Include);
            CameraObstructionFade obstructionFade = FindFirstObjectByType<CameraObstructionFade>(FindObjectsInactive.Include);

            // Guard against reassigning a target the camera is already pointed at: Cinemachine
            // treats any assignment to Follow/LookAt as a target CHANGE and resets its internal
            // damping/tracking state, causing a momentary recenter/pop even when the value is
            // unchanged. This matters because HandleSceneLoadCompleted re-invokes this whole
            // routine on OnLoadEventCompleted, which - on the host - also fires when a SECOND
            // client finishes its own scene synchronization, even though nothing changed for this
            // player. Without this guard, every other player connecting would visibly jolt everyone
            // else's camera for no reason.
            if (virtualCamera != null && virtualCamera.Follow != transform)
            {
                virtualCamera.Follow = transform;
            }

            if (virtualCamera != null && virtualCamera.LookAt != lookAtTarget)
            {
                virtualCamera.LookAt = lookAtTarget;
            }

            if (obstructionFade != null && obstructionFade.target != lookAtTarget)
            {
                obstructionFade.target = lookAtTarget;
            }

            if (virtualCamera != null && obstructionFade != null)
            {
                wireCameraRoutine = null;
                yield break;
            }

            yield return null;
        }

        Debug.LogWarning("[Player] Could not find the CinemachineCamera/CameraObstructionFade to wire to the local player after 1 second.");
        wireCameraRoutine = null;
    }

    private void PlayerAnimator_OnInteractAnimationFinished(object sender, EventArgs e)
    {
        Debug.Log($"CanPickUp: {inventoryManager.CanPickUp(currentPickable.collectibleSO, team.Value)}"); // add this

        if (inventoryManager.CanPickUp(currentPickable.collectibleSO, team.Value))
        {
            Collectible requestedPickable = currentPickable;
            requestedPickable.OnPickedUpConfirmed += Collectible_OnPickedUpConfirmed;
            requestedPickable.RequestPickUpServerRpc();
        }

        currentPickable = null;
        isPerformingInteraction = false;
    }

    // Runs only on the requesting client (the confirmation ClientRpc is targeted), so this
    // handles client-local concerns only: crediting this client's own inventory. Chase
    // triggering (Player.NotifyPickedUp, below) has to happen server-side instead, since
    // Chaser's AI only runs on the server and this callback wouldn't reach it when a
    // non-host client is the one picking the item up.
    private void Collectible_OnPickedUpConfirmed(Collectible collectible)
    {
        collectible.OnPickedUpConfirmed -= Collectible_OnPickedUpConfirmed;

        OnObjectPickUp?.Invoke(this, new OnObjectPickUpEventArgs
        {
            collectibleSO = collectible.collectibleSO
        });

        if (collectible.collectibleSO.flowerName == FlowerName.BalloonFlower)
            OnBaloonTaken?.Invoke(this, EventArgs.Empty);

        if (collectible.collectibleSO.fruitName == FruitName.BananaBread)
            OnBananaTaken?.Invoke(this, new OnBananaTakenEventArgs { collectible = collectible });
    }

    // Called by Collectible.RequestPickUpServerRpc, which always runs on the server -
    // this is where Chaser (server-authoritative) needs to observe the pickup from.
    public static void NotifyPickedUp(Player player, Collectible collectible)
    {
        if (collectible.collectibleSO.flowerName == FlowerName.BalloonFlower)
            OnAnyBaloonTaken?.Invoke(player);

        if (collectible.collectibleSO.fruitName == FruitName.BananaBread)
            OnAnyBananaTaken?.Invoke(player, collectible);
    }

    private void InputManager_OnInteractPerformed(object sender, EventArgs e)
    {
        if (!isPerformingInteraction)
        {
            isOnRope = hasRope;

            Collectible pickable = DetectClosestPickableObject();

            if (pickable)
            {
                isPerformingInteraction = true;

                if (pickable.collectibleSO.collectibleName == "Apple" || pickable.collectibleSO.collectibleName == "Cherry" || pickable.collectibleSO.collectibleName == "Pomegranate")
                {
                    OnObjectPickDownAnimate?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    OnObjectPickUpAnimate?.Invoke(this, EventArgs.Empty);
                }

                currentPickable = pickable;
            }
        }
    }

    void Update()
    {
        if (!IsOwner) return;

        if (!isPerformingInteraction && !isStunned)
        {
            HandleStamina();
            HandleMovement();

            if (!isOnRope)
                HandleGravity();
        }
        else if (isStunned)
        {
            HandleGravity();
        }
    }

    public void SetOnRope(bool value)
    {
        isOnRope = value;
    }

    private void HandleStamina()
    {
        bool wantsToRun = InputManager.Instance.IsRunHeld() && !isOnRope;

        if (wantsToRun && !isStaminaExhausted && currentStamina > 0f)
        {
            // Drain stamina
            currentStamina -= staminaDrainRate * Time.deltaTime;
            staminaRegenDelayTimer = staminaRegenDelay;

            if (currentStamina <= 0f)
            {
                currentStamina = 0f;
                isStaminaExhausted = true;
            }

            SetRunning(true);
        }
        else
        {
            // Regen stamina
            if (staminaRegenDelayTimer > 0f)
            {
                staminaRegenDelayTimer -= Time.deltaTime;
            }
            else if (currentStamina < maxStamina)
            {
                currentStamina += staminaRegenRate * Time.deltaTime;
                currentStamina = Mathf.Min(currentStamina, maxStamina);

                // Lift exhaustion once stamina partially recovers (e.g. 20%)
                if (isStaminaExhausted && currentStamina >= maxStamina * 0.2f)
                {
                    isStaminaExhausted = false;
                }
            }

            SetRunning(false);
        }

        OnStaminaChanged?.Invoke(this, new OnStaminaChangedEventArgs
        {
            staminaNormalized = GetStaminaNormalized()
        });
    }

    private void SetRunning(bool value)
    {
        if (isRunning == value) return;

        isRunning = value;

        if (isRunning)
            OnStartedRunning?.Invoke(this, EventArgs.Empty);
        else
            OnStoppedRunning?.Invoke(this, EventArgs.Empty);
    }

    private void HandleMovement()
    {
        Vector2 inputVector = InputManager.Instance.GetInputVectorNormalized();
        float speed = (isRunning ? runSpeed : movementSpeed) * speedMultiplier;
        float moveDistance = speed * Time.deltaTime;

        Vector3 moveDir = Vector3.zero;

        if (isOnRope)
        {
            moveDir = new Vector3(0f, inputVector.y, 0f);
        }
        else
        {
            moveDir = new Vector3(inputVector.x, 0f, inputVector.y);
        }

        characterController.Move(moveDir * moveDistance);

        isWalking = moveDir != Vector3.zero;

        if (!isOnRope)
        {
            transform.forward = Vector3.Slerp(transform.forward, moveDir, Time.deltaTime * rotateSpeed);
        }
    }

    private void HandleGravity()
    {
        float moveDistance = movementSpeed * Time.deltaTime;
        characterController.Move(new Vector3(0f, -1f, 0f) * moveDistance);
    }

    private Collectible DetectClosestPickableObject()
    {
        Vector3 center = transform.position + transform.forward * detectionDistance;
        Collider[] hits = Physics.OverlapBox(center, boxSize * 0.5f, transform.rotation, pickableLayer);

        float closestDistance = Mathf.Infinity;
        Collectible closestPickable = null;

        foreach (Collider col in hits)
        {
            Collectible pickable = col.GetComponent<Collectible>();
            if (pickable != null)
            {
                float distance = Vector3.Distance(transform.position, col.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPickable = pickable;
                }
            }
        }

        return closestPickable;
    }

    private void DrawPickupBox()
    {
        Vector3 center = transform.position + transform.forward * detectionDistance;
        Collider[] hits = Physics.OverlapBox(center, boxSize * 0.5f, transform.rotation, pickableLayer);
        Gizmos.color = hits.Length > 0 ? Color.green : Color.yellow;
        Gizmos.matrix = Matrix4x4.TRS(center, transform.rotation, boxSize);
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
    }

    private void OnDrawGizmos() => DrawPickupBox();

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(ROPE_TAG))
        {
            Debug.Log("ROPE ENTERED!");
            hasRope = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(ROPE_TAG))
        {
            Debug.Log("ROPE EXIT!");
            hasRope = false;
            isOnRope = false;
        }
    }

    // Called from Chaser on the server only; relayed to every client (including this
    // player's own owner) via ClientRpc so the stun actually freezes their movement and
    // plays consistently everywhere, not just on the server's mirrored copy.
    public void ApplyStun(float duration) => ApplyStunClientRpc(duration);

    [ClientRpc]
    private void ApplyStunClientRpc(float duration)
    {
        if (stunCoroutine != null)
            StopCoroutine(stunCoroutine);

        stunCoroutine = StartCoroutine(StunRoutine(duration));
    }

    private IEnumerator StunRoutine(float duration)
    {
        isStunned = true;
        isRunning = false;
        isWalking = false;

        OnStunned?.Invoke(this, EventArgs.Empty);

        yield return new WaitForSeconds(duration);

        isStunned = false;
        stunCoroutine = null;

        OnStunRecovered?.Invoke(this, EventArgs.Empty);
    }

    // Triggered by a client-local PlayerDetector (e.g. SpeedBoostTrigger), not server-side AI,
    // so - unlike ApplyStun - this can call straight into the coroutine with no ClientRpc hop.
    public void ApplySpeedBoost(float multiplier, float duration)
    {
        if (speedBoostCoroutine != null)
            StopCoroutine(speedBoostCoroutine);

        speedBoostCoroutine = StartCoroutine(SpeedBoostRoutine(multiplier, duration));
    }

    private IEnumerator SpeedBoostRoutine(float multiplier, float duration)
    {
        speedMultiplier = multiplier;

        yield return new WaitForSeconds(duration);

        speedMultiplier = 1f;
        speedBoostCoroutine = null;
    }
}
