using UnityEngine;

[CreateAssetMenu(fileName = "DamageBoostEffect", menuName = "Charms/Effects/Damage Boost")]
public class DamageBoostEffect : CharmEffect 
{
    public int bonusDamage = 1;

    public override void Apply(PlayerHealth health)
    {
        if (PlayerMovement.Instance != null)
            PlayerMovement.Instance.attackDamage += bonusDamage;
    }

    public override void Remove(PlayerHealth health)
    {
        if (PlayerMovement.Instance != null)
            PlayerMovement.Instance.attackDamage -= bonusDamage;
    }
}