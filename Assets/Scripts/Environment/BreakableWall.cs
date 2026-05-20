using UnityEngine;

/// <summary>
/// A solid wall that permanently breaks when hit by the player's attack.
/// Broken state is persisted via GameManager's pickup-ID system.
/// Requires a trigger Collider2D for attack detection (add a second
/// non-trigger Collider2D for solid physics — see Inspector setup notes).
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class BreakableWall : MonoBehaviour
{
    [Header("Identity")]
    [Tooltip("Unique ID used to persist broken state. Must be unique across all breakable walls and pickups.")]
    [SerializeField] private string wallID = "";

    [Header("Breaking")]
    [Tooltip("Number of hits required to break the wall.")]
    [SerializeField] private int hitsToBreak = 1;

    [Header("Particles")]
    [SerializeField] private Color particleColor = Color.gray;

    private const string AttackTag     = "Attack";
    private const string DownAttackTag = "DownAttack";

    private int  _hitsRemaining;
    private bool _isBroken;

    private void Start()
    {
        if (string.IsNullOrEmpty(wallID))
        {
            Debug.LogWarning($"BreakableWall '{gameObject.name}': wallID is empty — assign a unique ID in the Inspector.");
            return;
        }

        // Same pattern as pickups: if already collected in a previous session, disable immediately.
        if (GameManager.Instance != null && GameManager.Instance.IsPickupCollected(wallID))
        {
            gameObject.SetActive(false);
            return;
        }

        _hitsRemaining = hitsToBreak;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_isBroken || string.IsNullOrEmpty(wallID))
            return;

        if (!other.CompareTag(AttackTag) && !other.CompareTag(DownAttackTag))
            return;

        if (!(PlayerMovement.Instance?.TryRegisterHit(GetInstanceID()) ?? false)) return;

        _hitsRemaining--;
        if (_hitsRemaining <= 0)
        {
            _isBroken = true;
            Break();
        }
    }

    private void Break()
    {
        SpawnBreakParticles();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterCollectedPickup(wallID);
            GameManager.Instance.SaveGame();
        }

        PlayerMovement.Instance?.ApplyAttackKnockback(transform.position);
        Destroy(gameObject);
    }

    private void SpawnBreakParticles()
    {
        GameObject particleGO = new GameObject("BreakWall_Particles");
        particleGO.transform.position = transform.position;

        ParticleSystem ps = particleGO.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration        = 0.5f;
        main.loop            = false;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(2f, 7f);
        main.startSize       = new ParticleSystem.MinMaxCurve(0.05f, 0.18f);
        main.startColor      = new ParticleSystem.MinMaxGradient(particleColor);
        main.gravityModifier = 0.4f;
        main.maxParticles    = 20;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.enabled      = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 18) });

        var shape = ps.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius    = 0.3f;

        ps.Play();
        Destroy(particleGO, 1f);
    }
}
