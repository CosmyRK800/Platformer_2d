using UnityEngine;

[CreateAssetMenu(fileName = "AttackRateEffect", menuName = "Charms/Effects/Attack Rate")]
public class AttackRateEffect : CharmEffect
{
    public float reductionAmount = 0.15f;

    public override void Apply(PlayerHealth health)
    {
        if (PlayerMovement.Instance != null)
        {
            PlayerMovement.Instance.attackDuration -= reductionAmount;
            PlayerMovement.Instance.downAttackDuration -= reductionAmount;
        }
    }

    public override void Remove(PlayerHealth health)
    {
        if (PlayerMovement.Instance != null)
        {
            PlayerMovement.Instance.attackDuration += reductionAmount;
            PlayerMovement.Instance.downAttackDuration += reductionAmount;
        }
    }
}