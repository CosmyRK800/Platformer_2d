using UnityEngine;

/// <summary>
/// Singleton that acts as the authoritative source for unlocked abilities.
/// Ability pickups call Unlock() here instead of touching PlayerMovement directly.
/// GameManager calls Restore() on load.
/// </summary>
public class AbilityRegistry : MonoBehaviour
{
    public static AbilityRegistry Instance { get; private set; }

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

    public bool DashUnlocked       => PlayerMovement.Instance != null && PlayerMovement.Instance.dashUnlocked;
    public bool DoubleJumpUnlocked => PlayerMovement.Instance != null && PlayerMovement.Instance.doubleJumpUnlocked;
    public bool WallJumpUnlocked   => PlayerMovement.Instance != null && PlayerMovement.Instance.wallJumpUnlocked;

    /// <summary>Unlock a named ability on PlayerMovement. Names: "Dash", "DoubleJump", "WallJump".</summary>
    public void Unlock(string abilityName)
    {
        if (PlayerMovement.Instance == null)
        {
            Debug.LogWarning($"AbilityRegistry: PlayerMovement.Instance is null, cannot unlock '{abilityName}'.");
            return;
        }

        switch (abilityName)
        {
            case "Dash":
                PlayerMovement.Instance.dashUnlocked = true;
                InventoryUI.NotifyChanged();
                break;
            case "DoubleJump":
                PlayerMovement.Instance.doubleJumpUnlocked = true;
                PlayerMovement.Instance.RefreshJumps();
                InventoryUI.NotifyChanged();
                break;
            case "WallJump":
                PlayerMovement.Instance.wallJumpUnlocked = true;
                InventoryUI.NotifyChanged();
                break;
            default:
                Debug.LogWarning($"AbilityRegistry: unknown ability '{abilityName}'.");
                break;
        }
    }

    /// <summary>Called by GameManager.LoadGame() to restore saved ability state.</summary>
    public void Restore(bool dash, bool doubleJump, bool wallJump)
    {
        if (dash)       Unlock("Dash");
        if (doubleJump) Unlock("DoubleJump");
        if (wallJump)   Unlock("WallJump");
    }
}
