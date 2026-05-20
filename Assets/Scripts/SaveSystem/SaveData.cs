using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    public string activeCheckpointID = "";

    public bool dashUnlocked;
    public bool doubleJumpUnlocked;
    public bool wallJumpUnlocked;

    public List<string> collectedCharmIDs = new List<string>();

    // Exactly 3 entries (one per slot); empty string means the slot is empty.
    public List<string> equippedCharmIDs = new List<string>();

    public int currentHealth;

    // Human-readable checkpoint name shown in the save-slot screen.
    public string lastCheckpointName = "";

    // Accumulated play time in seconds (not counting the current session).
    public float playTimeSeconds;

    // IDs of every pickup (ability or charm) that has been collected.
    // Populated by GameManager.RegisterCollectedPickup(); used to hide already-collected pickups on load.
    public List<string> collectedPickupIDs = new List<string>();

    public int totalCoinsInScene = 0;

    public int collectedSouls = 0;

    public int collectedCoins = 0;

    public bool wishingWellUsed = false;
    public int wishingWellCoinsThrown = 0;
}
