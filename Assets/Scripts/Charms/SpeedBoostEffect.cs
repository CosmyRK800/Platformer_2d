using UnityEngine;

[CreateAssetMenu(fileName = "SpeedBoostEffect", menuName = "Charms/Effects/Speed Boost")]
public class SpeedBoostEffect : CharmEffect
{
    public float bonusSpeed = 3f;

    public override void Apply(PlayerHealth health)
    {
        if (PlayerMovement.Instance != null)
            PlayerMovement.Instance.moveSpeed += bonusSpeed;
    }

    public override void Remove(PlayerHealth health)
    {
        if (PlayerMovement.Instance != null)
            PlayerMovement.Instance.moveSpeed -= bonusSpeed;
    }
}