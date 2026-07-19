using System;
using Unity.Netcode;
using UnityEngine;

public class CycleManager : NetworkBehaviour
{
    public static CycleManager Instance { get; private set; }

    float cycleTime;
    [SerializeField] float cycleTimeMax;

    private NetworkVariable<int> cycleIndex = new NetworkVariable<int>(0);

    public event EventHandler OnCycleChange;

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        cycleIndex.OnValueChanged += OnCycleIndexChanged;
    }

    public override void OnNetworkDespawn()
    {
        cycleIndex.OnValueChanged -= OnCycleIndexChanged;
    }

    private void OnCycleIndexChanged(int previousValue, int newValue)
    {
        OnCycleChange?.Invoke(this, EventArgs.Empty);
    }

    void Update()
    {
        if (!IsServer) return;

        cycleTime += Time.deltaTime;

        if (cycleTime >= cycleTimeMax)
        {
            cycleTime = 0f;
            cycleIndex.Value++;
        }
    }
}
