using UnityEngine;

/// <summary>
/// Place this on any transition GameObject between two zones.
/// Requires a BoxCollider2D set to "Is Trigger".
/// Resize the BoxCollider2D in the Inspector to match the doorway or passage opening —
/// the gizmo line will automatically match it.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class ZoneTransitionTrigger : MonoBehaviour
{
    public enum TransitionAxis
    {
        /// <summary>Zones side by side — player walks left or right through a vertical opening.</summary>
        Horizontal,
        /// <summary>Zones stacked vertically — player falls down or jumps up through a horizontal opening.</summary>
        Vertical
    }

    [Header("Axis")]
    [Tooltip("Horizontal: player crosses a vertical wall. Vertical: player passes through a floor or ceiling.")]
    public TransitionAxis axis = TransitionAxis.Horizontal;

    [Header("Spawn Points")]
    [Tooltip("Where the player arrives when coming from Side A (left side for Horizontal, top for Vertical).")]
    public Transform spawnA;

    [Tooltip("Where the player arrives when coming from Side B (right side for Horizontal, bottom for Vertical).")]
    public Transform spawnB;

    private const string PlayerTag = "Player";

    private void Reset()
    {
        var col = GetComponent<BoxCollider2D>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(PlayerTag))
            return;

        if (ZoneTransitionManager.Instance == null)
        {
            Debug.LogWarning("[ZoneTransitionTrigger] ZoneTransitionManager not found in scene.");
            return;
        }

        bool comingFromA;
        if (axis == TransitionAxis.Horizontal)
        {
            // Player walks left or right — position is reliable since movement is slow.
            comingFromA = other.transform.position.x < transform.position.x;
        }
        else
        {
            // Player falls or jumps — position at trigger entry is unreliable because fast
            // vertical movement may have already carried the player past the trigger center
            // by the time OnTriggerEnter2D fires. Use velocity direction instead.
            var rb = other.GetComponent<Rigidbody2D>();
            if (rb != null)
                comingFromA = rb.linearVelocity.y < 0f; // falling down → coming from above (Side A)
            else
                comingFromA = other.transform.position.y > transform.position.y;
        }

        Transform destination = comingFromA ? spawnB : spawnA;

        if (destination == null)
        {
            Debug.LogWarning($"[ZoneTransitionTrigger] Spawn point not assigned on '{gameObject.name}'.");
            return;
        }

        ZoneTransitionManager.Instance.Transition(other.transform, destination.position);
    }

    private void OnDrawGizmos()
    {
        var col = GetComponent<BoxCollider2D>();
        Vector2 colSize = col != null ? col.size : Vector2.one;
        Vector2 colOffset = col != null ? col.offset : Vector2.zero;
        Vector3 center = transform.position + (Vector3)colOffset;
        float halfW = colSize.x * 0.5f;
        float halfH = colSize.y * 0.5f;

        // Filled collider preview
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.15f);
        Gizmos.DrawCube(center, new Vector3(colSize.x, colSize.y, 0.05f));

        // Boundary line that matches the collider height (Horizontal) or width (Vertical)
        Gizmos.color = Color.red;
        if (axis == TransitionAxis.Horizontal)
        {
            Gizmos.DrawLine(
                new Vector3(center.x, center.y - halfH, 0f),
                new Vector3(center.x, center.y + halfH, 0f));
        }
        else
        {
            Gizmos.DrawLine(
                new Vector3(center.x - halfW, center.y, 0f),
                new Vector3(center.x + halfW, center.y, 0f));
        }

        // Spawn point indicators
        if (spawnA != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(spawnA.position, 0.3f);
            Gizmos.DrawLine(center, spawnA.position);
        }

        if (spawnB != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(spawnB.position, 0.3f);
            Gizmos.DrawLine(center, spawnB.position);
        }
    }
}
