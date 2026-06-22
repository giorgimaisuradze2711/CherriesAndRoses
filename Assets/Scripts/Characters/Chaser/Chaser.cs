using System;
using UnityEngine;
using UnityEngine.AI;

public abstract class Chaser : MonoBehaviour
{
    [SerializeField] protected Player player;
    [SerializeField] protected NavMeshAgent agent;

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

    protected void OnTriggerEvent() => StartChase();

    private void Update()
    {
        if (_hasBeenTriggered)
            CheckPlayerBounds();

        if (_isChasingPlayer)
        {
            agent.SetDestination(player.transform.position);

            if (Vector3.Distance(agent.transform.position, player.transform.position) <= catchRadius)
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
        if (!NavMesh.SamplePosition(player.transform.position, out _, navMeshSampleDistance, navMeshAreaMask))
        {
            if (_isChasingPlayer)
                StartReturning();
        }
        else
        {
            if (_isReturning)
                StartChase();
        }
    }

    private void StartChase()
    {
        _hasBeenTriggered = true;
        _isReturning = false;
        _isChasingPlayer = true;
        agent.isStopped = false;
    }

    private void CatchPlayer()
    {
        _isChasingPlayer = false;
        _hasBeenTriggered = false;
        player.ApplyStun(4f);
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
            StartChase();
    }
}