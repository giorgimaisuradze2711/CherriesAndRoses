using System;
using UnityEngine;

public class CycleManager : MonoBehaviour
{
    public static CycleManager Instance { get; private set; }

    float cycleTime;
    [SerializeField] float cycleTimeMax;

    public event EventHandler OnCycleChange;

    private void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        cycleTime += Time.deltaTime;

        if (cycleTime >= cycleTimeMax )
        {
            cycleTime = 0f;
            OnCycleChange?.Invoke(this, EventArgs.Empty);
        }
    }
}
