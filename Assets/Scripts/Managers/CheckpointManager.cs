using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton that tracks the currently active checkpoint position.
/// PlayerHealth queries this on death to determine the respawn location.
/// </summary>
[DefaultExecutionOrder(-50)]
public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance { get; private set; }

    /// <summary>The world position the player will respawn at when they die.</summary>
    public Vector3 ActiveCheckpointPosition { get; private set; }

    /// <summary>The ID of the currently active checkpoint.</summary>
    public string ActiveCheckpointID { get; private set; } = "";

    /// <summary>True once the player has activated at least one bench checkpoint.</summary>
    public bool HasCheckpoint { get; private set; }

    // Static so Checkpoint/ForcedCheckpoint can register during their Awake()
    // without depending on CheckpointManager.Instance being set first.
    private static readonly Dictionary<string, Vector3> s_registry       = new();
    private static readonly HashSet<string>             s_benchIDs       = new();
    private static readonly Dictionary<string, Vector3> s_benchSitPositions = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        // Clear stale entries from a previous scene/play session.
        s_registry.Clear();
        s_benchIDs.Clear();
        s_benchSitPositions.Clear();
    }

    /// <summary>
    /// Called by ForcedCheckpoint in Awake() to register its position.
    /// </summary>
    public static void Register(string id, Vector3 position)
    {
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogWarning("CheckpointManager.Register: empty checkpoint ID — assign a unique checkpointID in the Inspector.");
            return;
        }

        s_registry[id] = position;
    }

    /// <summary>
    /// Called by Checkpoint (bench) in Awake() to register both its world position
    /// and the exact sit position (bench position + sitOffset) for load-time restoration.
    /// </summary>
    public static void RegisterBench(string id, Vector3 position, Vector3 sitPosition)
    {
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogWarning("CheckpointManager.RegisterBench: empty checkpoint ID — assign a unique checkpointID in the Inspector.");
            return;
        }

        s_registry[id]          = position;
        s_benchIDs.Add(id);
        s_benchSitPositions[id] = sitPosition;
    }

    /// <summary>Returns true if the checkpoint with this ID is a bench (Checkpoint), not a ForcedCheckpoint.</summary>
    public bool IsBenchCheckpoint(string id) => s_benchIDs.Contains(id);

    /// <summary>Returns the sit position stored at registration time (bench position + sitOffset).</summary>
    public Vector3 GetBenchSitPosition(string id) =>
        s_benchSitPositions.TryGetValue(id, out Vector3 pos) ? pos : ActiveCheckpointPosition;

    /// <summary>
    /// Registers a new active checkpoint. Any subsequent death will respawn
    /// the player here instead of the scene start position.
    /// </summary>
    public void SetCheckpoint(string id, Vector3 position)
    {
        ActiveCheckpointID       = id;
        ActiveCheckpointPosition = position;
        HasCheckpoint            = true;
        Debug.Log($"CheckpointManager: checkpoint '{id}' set at {position}");
    }

    /// <summary>
    /// Restores a previously saved checkpoint by ID.
    /// Called by GameManager.LoadGame() after all scene Awake()s have run.
    /// </summary>
    public void RestoreCheckpoint(string id)
    {
        if (s_registry.TryGetValue(id, out Vector3 position))
        {
            SetCheckpoint(id, position);
        }
        else
        {
            Debug.LogWarning($"CheckpointManager: checkpoint ID '{id}' not found in registry. " +
                             "Ensure every Checkpoint and ForcedCheckpoint has a unique checkpointID assigned.");
        }
    }
}
