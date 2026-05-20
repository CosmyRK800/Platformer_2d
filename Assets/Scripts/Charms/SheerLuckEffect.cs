using UnityEngine;

[CreateAssetMenu(fileName = "SheerLuckEffect", menuName = "Charms/Effects/Sheer Luck")]
public class SheerLuckEffect : CharmEffect
{
    public float dodgeChance = 0.3f;

    public override void Apply(PlayerHealth health)
    {
        if (PlayerMovement.Instance == null) return;
        var monitor = PlayerMovement.Instance.gameObject.AddComponent<SheerLuckMonitor>();
        monitor.dodgeChance = dodgeChance;
    }

    public override void Remove(PlayerHealth health)
    {
        if (PlayerMovement.Instance == null) return;
        var monitor = PlayerMovement.Instance.GetComponent<SheerLuckMonitor>();
        if (monitor != null)
            Object.Destroy(monitor);
    }
}

public class SheerLuckMonitor : MonoBehaviour
{
    public float dodgeChance = 0.3f;

    private void OnEnable()  { PlayerHealth.OnDamageCheck += CheckLuck; }
    private void OnDisable() { PlayerHealth.OnDamageCheck -= CheckLuck; }

    private bool CheckLuck()
    {
        bool dodged = Random.value < dodgeChance;
        Debug.Log("[SheerLuck] " + (dodged ? "DODGED!" : "hit") + " chance=" + dodgeChance);
        if (dodged)
            PlayerHealth.Instance?.TriggerDodgeRespawn();
        return dodged;
    }
}
