using UnityEngine;

/// <summary>
/// Automatically sets itself as the active checkpoint the moment the player
/// enters its trigger zone. No interaction required — the player just has to
/// pass through the area.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ForcedCheckpoint : MonoBehaviour
{
    [Header("Save System")]
    [Tooltip("Unique identifier for this forced checkpoint. Must match across save/load.")]
    [SerializeField] private string checkpointID = "";

    private const string PlayerTag = "Player";

    private bool _activated;

    private void Awake()
    {
        // Make sure the collider is always a trigger.
        Collider2D col = GetComponent<Collider2D>();
        col.isTrigger = true;

        CheckpointManager.Register(checkpointID, transform.position);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_activated || !other.CompareTag(PlayerTag))
            return;

        if (CheckpointManager.Instance == null)
        {
            Debug.LogWarning("ForcedCheckpoint: CheckpointManager not found in scene.");
            return;
        }

        CheckpointManager.Instance.SetCheckpoint(checkpointID, transform.position);
        _activated = true;
        Debug.Log($"ForcedCheckpoint '{checkpointID}' activated at {transform.position}");

        GameManager.Instance?.SaveGame();
    }
}
