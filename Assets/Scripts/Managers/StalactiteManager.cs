using System.Collections.Generic;
using UnityEngine;

public class StalactiteManager : MonoBehaviour
{
    public static StalactiteManager Instance { get; private set; }

    private static readonly List<StalactiteRespawner> _pending = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        _pending.Clear();
    }

    public static void Register(StalactiteRespawner r)   => _pending.Add(r);
    public static void Unregister(StalactiteRespawner r) => _pending.Remove(r);

    public static void RespawnAll()
    {
        var copy = new List<StalactiteRespawner>(_pending);
        _pending.Clear();
        foreach (StalactiteRespawner r in copy)
            r?.TriggerRespawn();
    }
}
