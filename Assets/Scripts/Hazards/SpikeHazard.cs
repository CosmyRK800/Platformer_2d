using UnityEngine;

/// <summary>
/// Attach to the Spike Tilemap GameObject.
/// Deals damage when the player body touches spikes,
/// and triggers a bounce when the player's down-attack hitbox overlaps.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class SpikeHazard : MonoBehaviour
{
    private const string PlayerTag = "Player";
    private const string DownAttackTag = "DownAttack";

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Player body walks/falls into spikes → take damage
        if (other.CompareTag(PlayerTag))
        {
            PlayerHealth health = other.GetComponent<PlayerHealth>();
            if (health != null)
                health.TakeDamage();

            return;
        }

        // Down-attack hitbox hits spikes → bounce
        if (other.CompareTag(DownAttackTag))
        {
            PlayerMovement movement = other.GetComponentInParent<PlayerMovement>();
            if (movement != null)
                movement.BounceOffSpike();
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // Continuously deal damage only for the player body (not the hitbox)
        // Invincibility frames on PlayerHealth prevent spam
        if (other.CompareTag(PlayerTag))
        {
            PlayerHealth health = other.GetComponent<PlayerHealth>();
            if (health != null)
                health.TakeDamage();
        }
    }
}
