using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public abstract class Chaser : NetworkBehaviour
{
    protected Player targetPlayer;
    [SerializeField] protected NavMeshAgent agent;
    [SerializeField] protected Collectible triggerCollectible;

    [Header("Chase")]
    [SerializeField] private float catchRadius = 1.5f;

    [Header("Return")]
    [SerializeField] private float returnRadius = 0.2f;

    [Header("Bounds")]
    [SerializeField] private float navMeshSampleDistance = 2f;
    [SerializeField] protected int navMeshAreaMask = NavMesh.AllAreas;

    private Vector3 _agentOriginPosition;
    private Quaternion _agentOriginRotation;

    private bool _hasBeenTriggered;
    private bool _isChasingPlayer;
    private bool _isReturning;
    private bool _isIdleAtOrigin;

    public bool IsChasing => _isChasingPlayer;
    public event EventHandler OnChaseRequested;

    private void Awake()
    {
        _agentOriginPosition = agent.transform.position;
        _agentOriginRotation = agent.transform.rotation;
        agent.areaMask = navMeshAreaMask;
    }

    private void OnEnable() => SubscribeToEvent();
    private void OnDisable() => UnsubscribeFromEvent();

    protected abstract void SubscribeToEvent();
    protected abstract void UnsubscribeFromEvent();

    protected void OnTriggerEvent(Player player)
    {
        if (!IsServer) return;

        targetPlayer = player;
        RequestChase();
    }

    private void Update()
    {
        if (!IsServer) return;

        if (_hasBeenTriggered)
            CheckPlayerBounds();

        if (_isChasingPlayer)
        {
            agent.SetDestination(targetPlayer.transform.position);

            if (Vector3.Distance(agent.transform.position, targetPlayer.transform.position) <= catchRadius)
                CatchPlayer();
        }
        else if (_isReturning)
        {
            if (Vector3.Distance(agent.transform.position, _agentOriginPosition) <= returnRadius)
                ArriveAtOrigin();
        }
    }

    private void CheckPlayerBounds()
    {
        if (!NavMesh.SamplePosition(targetPlayer.transform.position, out _, navMeshSampleDistance, navMeshAreaMask))
        {
            if (_isChasingPlayer)
                StartReturning();
        }
        else
        {
            if (_isReturning || _isIdleAtOrigin)
                RequestChase();
        }
    }

    private void RequestChase()
    {
        _hasBeenTriggered = true;
        _isReturning = false;
        _isIdleAtOrigin = false;
        agent.isStopped = true;
        triggerCollectible?.CancelAutoRegrow();

        if (OnChaseRequested != null)
        {
            _isChasingPlayer = false;
            OnChaseRequested.Invoke(this, EventArgs.Empty);
        }
        else
        {
            ExecuteChase();
        }
    }

    public void ExecuteChase()
    {
        _isChasingPlayer = true;
        agent.isStopped = false;
    }

    private void CatchPlayer()
    {
        _isChasingPlayer = false;
        _hasBeenTriggered = false;
        targetPlayer.ApplyStun(4f);
        StartReturning();
    }

    private void StartReturning()
    {
        _isChasingPlayer = false;
        _isReturning = true;
        agent.isStopped = false;
        agent.SetDestination(_agentOriginPosition);
    }

    private void ArriveAtOrigin()
    {
        _isReturning = false;
        agent.isStopped = true;

        agent.enabled = false;
        transform.SetPositionAndRotation(_agentOriginPosition, _agentOriginRotation);
        agent.enabled = true;

        if (_hasBeenTriggered)
        {
            if (NavMesh.SamplePosition(targetPlayer.transform.position, out _, navMeshSampleDistance, navMeshAreaMask))
                RequestChase();
            else
                _isIdleAtOrigin = true;
        }
        else
        {
            triggerCollectible?.RegrowNow();
        }
    }
}
