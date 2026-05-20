using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton that manages collected and equipped charms.
/// Persists across deaths via DontDestroyOnLoad.
/// </summary>
public class CharmInventory : MonoBehaviour
{
    public static CharmInventory Instance { get; private set; }

    private const int SlotCount = 3;

    [Header("Save System")]
    [Tooltip("Assign every CharmData ScriptableObject here so RestoreState can look them up by ID.")]
    [SerializeField] private List<CharmData> _allCharmsRegistry = new List<CharmData>();

    private readonly List<CharmData> _collectedCharms = new List<CharmData>();
    private readonly CharmData[] _equippedCharms = new CharmData[SlotCount];

    private bool _isRestoringState;

    public IReadOnlyList<CharmData> CollectedCharms => _collectedCharms;
    public CharmData[] EquippedCharms => _equippedCharms;

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

private void Start()
{
    if (PlayerHealth.Instance != null)
        PlayerHealth.Instance.OnRespawn += OnPlayerRespawn;
    else
        Debug.LogWarning("CharmInventory: PlayerHealth.Instance e null in Start!");
}

public void EquipCharm(CharmData charm, int slotIndex)
{
    if (charm == null || slotIndex < 0 || slotIndex >= SlotCount)
        return;

    if (!_collectedCharms.Contains(charm))
        return;

    for (int i = 0; i < SlotCount; i++)
    {
        if (_equippedCharms[i] == charm)
            UnequipCharm(i);
    }

    UnequipCharm(slotIndex);

    _equippedCharms[slotIndex] = charm;
    
    Debug.Log($"PlayerHealth.Instance este: {PlayerHealth.Instance}");
    Debug.Log($"charm.effect este: {charm.effect}");
    
    charm.effect?.Apply(PlayerHealth.Instance);

    Debug.Log($"CharmInventory: equipped {charm.charmName} in slot {slotIndex}");
}

    private void OnDisable()
    {
        if (PlayerHealth.Instance != null)
            PlayerHealth.Instance.OnRespawn -= OnPlayerRespawn;
    }

    /// <summary>Adds a charm to the collected list if not already present.</summary>
    public void AddCharm(CharmData charm)
    {
        if (charm == null || _collectedCharms.Contains(charm))
            return;

        _collectedCharms.Add(charm);
        Debug.Log($"CharmInventory: collected {charm.charmName}");

        if (!_isRestoringState)
        {
            if (_collectedCharms.Count == 1)
                AchievementManager.Instance?.UnlockAchievement("charmed");

            if (_allCharmsRegistry.Count > 0 && _collectedCharms.Count == _allCharmsRegistry.Count)
                AchievementManager.Instance?.UnlockAchievement("enchanted");
        }
    }

    /// <summary>Equips a charm into the given slot. Unequips whatever was there before.</summary>
    // public void EquipCharm(CharmData charm, int slotIndex)
    // {
    //     if (charm == null || slotIndex < 0 || slotIndex >= SlotCount)
    //         return;

    //     if (!_collectedCharms.Contains(charm))
    //         return;

    //     // If this charm is already equipped in another slot, remove it first.
    //     for (int i = 0; i < SlotCount; i++)
    //     {
    //         if (_equippedCharms[i] == charm)
    //             UnequipCharm(i);
    //     }

    //     // Unequip whatever is currently in the target slot.
    //     UnequipCharm(slotIndex);

    //     _equippedCharms[slotIndex] = charm;
    //     charm.effect?.Apply(PlayerHealth.Instance);

    //     Debug.Log($"CharmInventory: equipped {charm.charmName} in slot {slotIndex}");
    // }

    /// <summary>Unequips all charm slots, removing their effects from PlayerHealth.</summary>
    public void UnequipAll()
    {
        for (int i = 0; i < SlotCount; i++)
            UnequipCharm(i);
    }

    /// <summary>Unequips the charm in the given slot.</summary>
    public void UnequipCharm(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= SlotCount)
            return;

        CharmData charm = _equippedCharms[slotIndex];
        if (charm == null)
            return;

        charm.effect?.Remove(PlayerHealth.Instance);
        _equippedCharms[slotIndex] = null;

        Debug.Log($"CharmInventory: unequipped {charm.charmName} from slot {slotIndex}");
    }

    /// <summary>Re-applies all equipped charm effects after the player respawns.</summary>
    private void OnPlayerRespawn()
    {
        if (PlayerHealth.Instance == null)
            return;

        for (int i = 0; i < SlotCount; i++)
        {
            if (_equippedCharms[i] == null)
                continue;

            _equippedCharms[i].effect?.Remove(PlayerHealth.Instance);
            _equippedCharms[i].effect?.Apply(PlayerHealth.Instance);
        }
    }

    // -------------------------------------------------------------------------
    // Save / Load helpers
    // -------------------------------------------------------------------------

    /// <summary>Returns the total HP bonus currently added to max/current lives by equipped charms.</summary>
    public int GetHealthBonus()
    {
        int bonus = 0;
        for (int i = 0; i < SlotCount; i++)
        {
            if (_equippedCharms[i] != null && _equippedCharms[i].effect != null)
                bonus += _equippedCharms[i].effect.HealthBonus;
        }
        return bonus;
    }

    /// <summary>Returns the asset name of every collected charm, in collection order.</summary>
    public List<string> GetCollectedIDs()
    {
        var ids = new List<string>(_collectedCharms.Count);
        foreach (CharmData charm in _collectedCharms)
            ids.Add(charm.name);
        return ids;
    }

    /// <summary>
    /// Returns a list of exactly <see cref="SlotCount"/> entries.
    /// Empty slots are represented by an empty string.
    /// </summary>
    public List<string> GetEquippedIDs()
    {
        var ids = new List<string>(SlotCount);
        for (int i = 0; i < SlotCount; i++)
            ids.Add(_equippedCharms[i] != null ? _equippedCharms[i].name : "");
        return ids;
    }

    /// <summary>
    /// Clears current inventory and restores it from saved ID lists.
    /// Charm effects are re-applied via the normal EquipCharm path.
    /// </summary>
    public void RestoreState(List<string> collectedIDs, List<string> equippedIDs)
    {
        _isRestoringState = true;

        // Unequip cleanly so effects are removed before we clear the list.
        for (int i = 0; i < SlotCount; i++)
            UnequipCharm(i);

        _collectedCharms.Clear();

        if (collectedIDs != null)
        {
            foreach (string id in collectedIDs)
            {
                CharmData charm = FindCharmByID(id);
                if (charm != null)
                    AddCharm(charm);
            }
        }

        if (equippedIDs != null)
        {
            for (int i = 0; i < equippedIDs.Count && i < SlotCount; i++)
            {
                if (string.IsNullOrEmpty(equippedIDs[i]))
                    continue;

                CharmData charm = FindCharmByID(equippedIDs[i]);
                if (charm == null)
                    continue;

                Debug.Log($"CharmInventory.RestoreState: equipping '{charm.charmName}' into slot {i} (PlayerHealth={PlayerHealth.Instance}).");
                EquipCharm(charm, i);

                if (_equippedCharms[i] != charm)
                    Debug.LogWarning($"CharmInventory.RestoreState: EquipCharm('{charm.charmName}', {i}) failed — charm was not in _collectedCharms at equip time.");
            }
        }

        _isRestoringState = false;
    }

    private CharmData FindCharmByID(string id)
    {
        foreach (CharmData charm in _allCharmsRegistry)
        {
            if (charm != null && charm.name == id)
                return charm;
        }

        Debug.LogWarning($"CharmInventory: charm with ID '{id}' not found in _allCharmsRegistry. " +
                         "Assign all CharmData assets to CharmInventory._allCharmsRegistry in the Inspector.");
        return null;
    }
}