using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class SaveSystem
{
    // -------------------------------------------------------------------------
    // Global achievements file (independent of save slots)
    // -------------------------------------------------------------------------

    private static string AchievementsPath =>
        Path.Combine(Application.persistentDataPath, "achievements.json");

    [Serializable]
    private sealed class AchievementSaveData
    {
        public List<string> unlockedIDs = new List<string>();
    }

    public static void SaveAchievements(List<string> unlockedIDs)
    {
        var wrapper = new AchievementSaveData { unlockedIDs = unlockedIDs ?? new List<string>() };
        File.WriteAllText(AchievementsPath, JsonUtility.ToJson(wrapper, true));
        Debug.Log($"SaveSystem: achievements saved ({wrapper.unlockedIDs.Count} ID(s)) to {AchievementsPath}");
    }

    public static List<string> LoadAchievements()
    {
        if (!File.Exists(AchievementsPath))
            return new List<string>();

        var wrapper = JsonUtility.FromJson<AchievementSaveData>(File.ReadAllText(AchievementsPath));
        return wrapper?.unlockedIDs ?? new List<string>();
    }

    // -------------------------------------------------------------------------
    // Single-file API (kept for backward compatibility)
    // -------------------------------------------------------------------------

    private static string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

    public static void Save(SaveData data)
    {
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePath, json);
        Debug.Log($"SaveSystem: saved to {SavePath}");
    }

    public static SaveData Load()
    {
        if (!File.Exists(SavePath))
            return null;

        string json = File.ReadAllText(SavePath);
        return JsonUtility.FromJson<SaveData>(json);
    }

    public static bool SaveExists() => File.Exists(SavePath);

    // -------------------------------------------------------------------------
    // Slot-based API (used by GameManager with multiple save slots)
    // -------------------------------------------------------------------------

    private static string SlotPath(int slot) =>
        Path.Combine(Application.persistentDataPath, $"save_slot{slot}.json");

    public static void Save(SaveData data, int slot)
    {
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SlotPath(slot), json);
        Debug.Log($"SaveSystem: slot {slot} saved to {SlotPath(slot)}");
    }

    public static SaveData Load(int slot)
    {
        string path = SlotPath(slot);
        if (!File.Exists(path))
            return null;

        string json = File.ReadAllText(path);
        return JsonUtility.FromJson<SaveData>(json);
    }

    public static bool SaveExists(int slot) => File.Exists(SlotPath(slot));

    public static void Delete(int slot)
    {
        string path = SlotPath(slot);
        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log($"SaveSystem: slot {slot} deleted.");
        }
    }
}
