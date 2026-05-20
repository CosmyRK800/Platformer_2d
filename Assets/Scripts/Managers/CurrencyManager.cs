using System;
using UnityEngine;

/// <summary>
/// Persistent singleton tracking the player's soul currency.
/// Souls accumulate by killing enemies and are lost on death.
/// Separate from SoulManager (the healing-orb gauge system).
/// </summary>
public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }

    public int CurrentSouls { get; private set; }

    /// <summary>Fires with (currentTotal, delta). delta == 0 when souls are lost.</summary>
    public static event Action<int, int> OnSoulsChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>Adds souls (enemy drop). Fires event and refreshes inventory display.</summary>
    public void AddSouls(int amount)
    {
        CurrentSouls += amount;
        Debug.Log($"[CurrencyManager] AddSouls: +{amount} → total={CurrentSouls}");
        OnSoulsChanged?.Invoke(CurrentSouls, amount);
        InventoryUI.NotifyChanged();
    }

    /// <summary>Wipes all souls on player death. Fires event with delta=0 (no gain popup).</summary>
    public void LoseSouls()
    {
        Debug.Log($"[CurrencyManager] LoseSouls: resetting {CurrentSouls} → 0");
        CurrentSouls = 0;
        OnSoulsChanged?.Invoke(0, 0);
        InventoryUI.NotifyChanged();
    }

    /// <summary>Silently restores souls from save data. No event, no popup.</summary>
    public void RestoreSouls(int amount)
    {
        CurrentSouls = amount;
    }
}
