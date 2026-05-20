using UnityEngine;

/// <summary>
/// Scene-local singleton that counts how many collectible coins exist at load time.
/// Not DontDestroyOnLoad — resets fresh every scene.
/// CoinPickup.Start() calls RegisterCoin() for every coin that is not already collected.
/// </summary>
public class CoinManager : MonoBehaviour
{
    public static CoinManager Instance { get; private set; }

    private int _totalCoins;
    public int TotalCoins => _totalCoins;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    /// <summary>Called by each active (not yet collected) CoinPickup in Start().</summary>
    public void RegisterCoin()
    {
        _totalCoins++;
    }
}
