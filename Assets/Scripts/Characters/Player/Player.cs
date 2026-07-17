using System;
using System.Collections;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class Player : MonoBehaviour
{
    public event EventHandler OnObjectPickUpAnimate;
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

    private const string ROPE_TAG = "Rope";

    [SerializeField] public CollectibleType team;

    [SerializeField] private InputManager inputManager;
    [SerializeField] private InventoryManager inventoryManager;

    [SerializeField] private PlayerAnimator playerAnimator;
    [SerializeField] private CharacterController characterController;

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

    public bool IsOnRope() => isOnRope;
    public bool IsWalking() => isWalking;
    public bool IsRunning() => isRunning;

    public bool IsStunned() => isStunned;
    public float GetStaminaNormalized() => currentStamina / maxStamina;

    void Start()
    {
        currentStamina = maxStamina;

        int teamCount = Enum.GetValues(typeof(CollectibleType)).Length;
        team = (CollectibleType)UnityEngine.Random.Range(0, teamCount);

        Debug.Log($"TEAM: {team}");

        OnPlayerTeamChoose?.Invoke(this, new OnPlayerTeamChooseEventArgs
        {
            playerTeam = team.ToString()
        });

        inputManager.EnablePlayerInputs();
        inputManager.OnInteractPerformed += InputManager_OnInteractPerformed;
        playerAnimator.OnInteractAnimationFinished += PlayerAnimator_OnInteractAnimationFinished;
    }

    private void PlayerAnimator_OnInteractAnimationFinished(object sender, EventArgs e)
    {
        Debug.Log($"CanPickUp: {inventoryManager.CanPickUp(currentPickable.collectibleSO, team)}"); // add this

        if (inventoryManager.CanPickUp(currentPickable.collectibleSO, team))
        {
            OnObjectPickUp?.Invoke(this, new OnObjectPickUpEventArgs
            {
                collectibleSO = currentPickable.collectibleSO
            });

            currentPickable.PickUp();

            if (currentPickable.collectibleSO.flowerName == FlowerName.BalloonFlower)
            {
                OnBaloonTaken?.Invoke(this, EventArgs.Empty);
            }

            if (currentPickable.collectibleSO.fruitName == FruitName.BananaBread)
            {
                OnBananaTaken?.Invoke(this, new OnBananaTakenEventArgs { collectible = currentPickable });
            }
        }

        currentPickable = null;
        isPerformingInteraction = false;
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
                OnObjectPickUpAnimate?.Invoke(this, EventArgs.Empty);
                currentPickable = pickable;
            }
        }
    }

    void Update()
    {
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
        bool wantsToRun = inputManager.IsRunHeld() && !isOnRope;

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
        Vector2 inputVector = inputManager.GetInputVectorNormalized();
        float speed = isRunning ? runSpeed : movementSpeed;
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

    public void ApplyStun(float duration)
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
}