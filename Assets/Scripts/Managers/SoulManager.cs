using System;
using UnityEngine;

public class SoulManager : MonoBehaviour
{
    public static SoulManager Instance { get; private set; }

    [Header("Soul")]
    public int maxSoul     = 18;
    public int soulPerHit  = 1;
    public int soulPerHeal = 6;

    private int _currentSoul;

    public static event Action<int, int> OnSoulChanged;

    public int CurrentSoul => _currentSoul;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    public void AddSoul(int amount)
    {
        _currentSoul = Mathf.Min(_currentSoul + amount, maxSoul);
        OnSoulChanged?.Invoke(_currentSoul, maxSoul);
    }

    public bool CanHeal() => _currentSoul >= soulPerHeal;

    public void ConsumeSoul()
    {
        _currentSoul = Mathf.Max(0, _currentSoul - soulPerHeal);
        OnSoulChanged?.Invoke(_currentSoul, maxSoul);
    }
}
