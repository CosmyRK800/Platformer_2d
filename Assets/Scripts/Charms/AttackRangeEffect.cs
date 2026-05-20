using UnityEngine;

[CreateAssetMenu(fileName = "AttackRangeEffect", menuName = "Charms/Effects/Attack Range")]
public class AttackRangeEffect : CharmEffect
{
    public float bonusRange = 0.2f;

    public override void Apply(PlayerHealth health)
    {
        if (PlayerMovement.Instance == null) return;

        BoxCollider2D hitbox = PlayerMovement.Instance.attackHitbox as BoxCollider2D;
        if (hitbox != null)
            hitbox.size += new Vector2(bonusRange, 0f);
    }

    public override void Remove(PlayerHealth health)
    {
        if (PlayerMovement.Instance == null) return;

        BoxCollider2D hitbox = PlayerMovement.Instance.attackHitbox as BoxCollider2D;
        if (hitbox != null)
            hitbox.size -= new Vector2(bonusRange, 0f);
    }
}