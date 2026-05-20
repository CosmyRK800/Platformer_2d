using UnityEngine;

/// <summary>
/// Attach to a Spike Tilemap GameObject that should be lethal even to a down-attack.
/// Deals damage on any contact — the player cannot bounce off these spikes.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class DeadSpikeHazard : MonoBehaviour
{
    private const string PlayerTag = "Player";
    private const string DownAttackTag = "DownAttack";

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(PlayerTag))
        {
            TryDamage(other);
            return;
        }

        // Down-attack hitbox hits these spikes → damage instead of bounce
        if (other.CompareTag(DownAttackTag))
        {
            PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
            if (health != null)
                health.TakeDamage();
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag(PlayerTag))
            TryDamage(other);
    }

    private void TryDamage(Collider2D other)
    {
        PlayerHealth health = other.GetComponent<PlayerHealth>();
        if (health != null)
            health.TakeDamage();
    }
}
