using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Persistent singleton that orchestrates save/load and slot management.
/// Place on a GameObject in the MainMenu scene (index 0).
/// Persists through all scenes via DontDestroyOnLoad.
///
/// Execution-order notes:
///   [DefaultExecutionOrder(100)] — Awake runs after all default-order (0) Awakes
///   but before any Start. Pickup Start() methods can safely call IsPickupCollected().
///   SceneManager.sceneLoaded fires after all Awakes in the new scene and before
///   any Start, so LoadGame() runs at the right moment when switching to the game scene.
/// </summary>
[DefaultExecutionOrder(100)]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private const int GameSceneIndex = 2;

    /// <summary>Index of the currently selected save slot (-1 = none selected).</summary>
    public int ActiveSlot { get; private set; } = -1;

    private readonly HashSet<string> _collectedPickupIDs = new();

    // Coin counter (session-only, not persisted)
    public int Coins { get; set; }

    public int Souls => CurrencyManager.Instance?.CurrentSouls ?? 0;

    public static event Action<int> OnCoinCollected;

    // Play-time tracking
    private float _storedPlayTime;      // seconds accumulated in previous sessions
    private float _sessionStartTime;    // Time.realtimeSinceStartup at game-scene entry

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // BUG 2 — one-time cleanup of the legacy single-file save format.
        string legacyPath = Path.Combine(Application.persistentDataPath, "save.json");
        if (File.Exists(legacyPath))
        {
            File.Delete(legacyPath);
            Debug.Log("GameManager: deleted legacy save.json.");
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// Fires after all Awakes in the newly loaded scene, before any Start.
    /// This is where LoadGame() is triggered for the normal menu → game flow.
    ///
    /// Execution-order guarantee for achievements:
    ///   Both GameManager (100) and AchievementManager (110) are DontDestroyOnLoad
    ///   and were fully initialized in the MainMenu scene before this callback ever
    ///   fires. AchievementManager.Instance is always non-null here.
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.buildIndex != GameSceneIndex)
            return;

        // Reset session timer; LoadGame() will restore _storedPlayTime if a save exists.
        _storedPlayTime    = 0f;
        _sessionStartTime  = Time.realtimeSinceStartup;

        // Strip stale charm effects carried over from the previous session, then reset
        // health to the serialized base so LoadGame starts from a known clean state.
        CharmInventory.Instance?.UnequipAll();
        PlayerHealth.Instance?.ForceReset();

        if (ActiveSlot >= 0 && SaveSystem.SaveExists(ActiveSlot))
            LoadGame();
    }

    private void Start()
    {
        // This Start() fires once, in scene 0 (MainMenu).
        // For the normal flow the game is loaded via OnSceneLoaded when scene 2 opens.
        // The condition below covers the editor edge-case of pressing Play directly in SampleScene.
        int currentScene = SceneManager.GetActiveScene().buildIndex;
        if (currentScene == GameSceneIndex && ActiveSlot >= 0 && SaveSystem.SaveExists(ActiveSlot))
            LoadGame();
    }

    public void AddCoin()
    {
        Coins++;
        OnCoinCollected?.Invoke(Coins);
        InventoryUI.NotifyChanged();
    }

    public void SpendCoin()
    {
        if (Coins <= 0) return;
        Coins--;
        InventoryUI.NotifyChanged();
    }

    // -------------------------------------------------------------------------
    // Slot management
    // -------------------------------------------------------------------------

    /// <summary>
    /// Selects the save slot that SaveGame/LoadGame will operate on.
    /// Also pre-populates collected pickup IDs so IsPickupCollected() works
    /// during the game scene's Awake phase (before OnSceneLoaded → LoadGame).
    /// </summary>
    public void SetActiveSlot(int slot)
    {
        ActiveSlot = slot;

        _collectedPickupIDs.Clear();
        if (SaveSystem.SaveExists(slot))
        {
            SaveData preload = SaveSystem.Load(slot);

            if (preload?.collectedPickupIDs != null)
                foreach (string id in preload.collectedPickupIDs)
                    _collectedPickupIDs.Add(id);
        }
    }

    /// <summary>Deletes the save file for the given slot.</summary>
    public void DeleteSlot(int slot)
    {
        SaveSystem.Delete(slot);
        if (ActiveSlot == slot)
            ActiveSlot = -1;
    }

    /// <summary>
    /// Returns lightweight preview data for the save-slot screen.
    /// Returns a SlotPreview with HasData = false when the slot is empty.
    /// </summary>
    public SlotPreview GetSlotPreview(int slot)
    {
        if (!SaveSystem.SaveExists(slot))
            return new SlotPreview { HasData = false };

        SaveData data = SaveSystem.Load(slot);
        if (data == null)
            return new SlotPreview { HasData = false };

        string name = string.IsNullOrEmpty(data.lastCheckpointName)
            ? data.activeCheckpointID
            : data.lastCheckpointName;

        return new SlotPreview
        {
            HasData           = true,
            LastCheckpointName = name,
            PlayTimeSeconds   = data.playTimeSeconds,
        };
    }

    // -------------------------------------------------------------------------
    // Pickup tracking
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns true if the pickup with this ID has already been collected.
    /// Pickups call this in Start() to decide whether to disable themselves.
    /// </summary>
    public bool IsPickupCollected(string id) =>
        !string.IsNullOrEmpty(id) && _collectedPickupIDs.Contains(id);

    /// <summary>Called by every pickup when the player collects it.</summary>
    public void RegisterCollectedPickup(string id)
    {
        if (!string.IsNullOrEmpty(id))
            _collectedPickupIDs.Add(id);
    }

    // -------------------------------------------------------------------------
    // Save / Load
    // -------------------------------------------------------------------------

    /// <summary>
    /// Snapshot all runtime state and write it to disk.
    /// Called by Checkpoint.Sit() and ForcedCheckpoint.OnTriggerEnter2D().
    /// </summary>
    public void SaveGame()
    {
        if (ActiveSlot < 0)
        {
            Debug.LogWarning("GameManager.SaveGame: no active slot selected.");
            return;
        }

        var data = new SaveData();

        data.activeCheckpointID  = CheckpointManager.Instance?.ActiveCheckpointID ?? "";
        data.lastCheckpointName  = data.activeCheckpointID;
        data.playTimeSeconds     = _storedPlayTime + (Time.realtimeSinceStartup - _sessionStartTime);

        if (AbilityRegistry.Instance != null)
        {
            data.dashUnlocked       = AbilityRegistry.Instance.DashUnlocked;
            data.doubleJumpUnlocked = AbilityRegistry.Instance.DoubleJumpUnlocked;
            data.wallJumpUnlocked   = AbilityRegistry.Instance.WallJumpUnlocked;
        }

        if (CharmInventory.Instance != null)
        {
            data.collectedCharmIDs = CharmInventory.Instance.GetCollectedIDs();
            data.equippedCharmIDs  = CharmInventory.Instance.GetEquippedIDs();
        }

        if (PlayerHealth.Instance != null)
        {
            int lives  = PlayerHealth.Instance.GetCurrentLives();
            int bonus  = CharmInventory.Instance != null ? CharmInventory.Instance.GetHealthBonus() : 0;
            data.currentHealth = Mathf.Max(0, lives - bonus);
            Debug.Log($"GameManager.SaveGame: lives={lives}, charmBonus={bonus}, savedHealth={data.currentHealth}");
        }

        data.collectedPickupIDs = new List<string>(_collectedPickupIDs);

        data.collectedSouls = CurrencyManager.Instance?.CurrentSouls ?? 0;
        data.collectedCoins = Coins;
        data.wishingWellUsed = WishingWell.Instance?.IsUsed ?? false;
        data.wishingWellCoinsThrown = WishingWell.Instance?.CoinsThrown ?? 0;

        Debug.Log($"GameManager.SaveGame: coins={Coins}, souls={data.collectedSouls}");
        SaveSystem.Save(data, ActiveSlot);
    }

    /// <summary>
    /// Read save data for the active slot and push state to all systems.
    /// Triggered automatically by OnSceneLoaded when the game scene loads.
    /// </summary>
    public void LoadGame()
    {
        if (ActiveSlot < 0)
            return;

        SaveData data = SaveSystem.Load(ActiveSlot);
        if (data == null)
            return;

        Coins = data.collectedCoins;

        Debug.Log($"GameManager.LoadGame: restoring coins={data.collectedCoins}, souls={data.collectedSouls}");

        // Restore accumulated play time and reset session start.
        _storedPlayTime   = data.playTimeSeconds;
        _sessionStartTime = Time.realtimeSinceStartup;

        // Sync the runtime pickup set.
        _collectedPickupIDs.Clear();
        if (data.collectedPickupIDs != null)
            foreach (string id in data.collectedPickupIDs)
                _collectedPickupIDs.Add(id);

        // Checkpoint must come first so the player respawns at the right position.
        if (!string.IsNullOrEmpty(data.activeCheckpointID))
        {
            CheckpointManager.Instance?.RestoreCheckpoint(data.activeCheckpointID);

            // RestoreCheckpoint only updates CheckpointManager's internal state.
            // Teleport the player now — Die() is never called on load, so nothing else does this.
            if (PlayerHealth.Instance != null &&
                CheckpointManager.Instance != null &&
                CheckpointManager.Instance.HasCheckpoint)
            {
                PlayerHealth.Instance.transform.position = CheckpointManager.Instance.ActiveCheckpointPosition;

                // If the saved checkpoint is a bench, place the player in the sitting pose.
                if (CheckpointManager.Instance.IsBenchCheckpoint(data.activeCheckpointID) &&
                    PlayerMovement.Instance != null)
                {
                    Vector3 sitPos = CheckpointManager.Instance.GetBenchSitPosition(data.activeCheckpointID);
                    PlayerMovement.Instance.SetSitting(true, sitPos);
                }
            }
        }
        else if (PlayerHealth.Instance != null)
        {
            // No checkpoint saved yet — place the player at the designated start position.
            GameObject startObj = GameObject.Find("Start_Checkpoint");
            if (startObj != null)
                PlayerHealth.Instance.transform.position = startObj.transform.position;
        }

        // Abilities first — nothing depends on them being last.
        AbilityRegistry.Instance?.Restore(data.dashUnlocked, data.doubleJumpUnlocked, data.wallJumpUnlocked);

        // Health before charms: charm effects (e.g. BlessingOfVitality) call back
        // into PlayerHealth to modify maxLives/currentLives. PlayerHealth.Instance
        // exists (Awake ran) but its internal values are at defaults until RestoreHealth
        // is called — applying charm effects before that produces incorrect results.
        Debug.Log($"GameManager.LoadGame: restoring base health={data.currentHealth} (charm bonuses will be added by RestoreState)");
        PlayerHealth.Instance?.RestoreHealth(data.currentHealth);

        // Charms after health so effects apply to a fully initialised health state.
        CharmInventory.Instance?.RestoreState(data.collectedCharmIDs, data.equippedCharmIDs);

        CurrencyManager.Instance?.RestoreSouls(data.collectedSouls);
        WishingWell.Instance?.LoadState(data.wishingWellUsed, data.wishingWellCoinsThrown);
        InventoryUI.NotifyChanged();
    }
}
