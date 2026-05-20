using UnityEngine;

/// <summary>
/// Charm effect: grants 2 extra max lives while equipped.
/// </summary>
[CreateAssetMenu(fileName = "BlessingOfVitalityEffect", menuName = "Charms/Effects/Blessing Of Vitality")]
public class BlessingOfVitalityEffect : CharmEffect
{
    private const int ExtraLives = 2;

    public override int HealthBonus => ExtraLives;

    public override void Apply(PlayerHealth health)
    {
        health.AddMaxLives(ExtraLives);
    }

    public override void Remove(PlayerHealth health)
    {
        health.AddMaxLives(-ExtraLives);
    }
}