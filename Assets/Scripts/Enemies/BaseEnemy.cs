using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(EnemyHealth))]
[RequireComponent(typeof(SpriteRenderer))]
public abstract class BaseEnemy : MonoBehaviour
{
    [Header("Combat")]
    [SerializeField] protected float bounceForce = 15f;
    [SerializeField] protected float knockbackForce = 5f;
    [SerializeField] protected float knockbackDuration = 0.1f;
    [SerializeField] protected int soulsDrop = 5;

    [Header("Particles")]
    [SerializeField] private Color particleColor = Color.red;

    [Header("Physics")]
    [SerializeField] private Collider2D _physicsCollider;

    protected Rigidbody2D _rb;
    protected SpriteRenderer _sprite;
    protected EnemyHealth _health;

    protected bool _isDead;
    protected bool _waiting;

    protected float _halfWidth;
    protected float _halfHeight;

    private const string PlayerTag     = "Player";
    private const string AttackTag     = "Attack";
    private const string DownAttackTag = "DownAttack";

    protected virtual void Awake()
    {
        _rb     = GetComponent<Rigidbody2D>();
        _sprite = GetComponent<SpriteRenderer>();
        _health = GetComponent<EnemyHealth>();

        _rb.freezeRotation = true;
        _rb.gravityScale   = 3f;
        GetComponent<Collider2D>().isTrigger = true;
    }

    protected virtual void Start()
    {
        Bounds b = GetComponent<Collider2D>().bounds;
        _halfWidth  = b.extents.x;
        _halfHeight = b.extents.y;

        _health.OnDeath += HandleDeath;
    }

    protected virtual void OnDestroy()
    {
        if (_health != null)
            _health.OnDeath -= HandleDeath;
    }

    public Collider2D GetPhysicsCollider() => _physicsCollider;

    private void OnTriggerStay2D(Collider2D other)
    {
        if (_isDead) return;
        if (other.GetComponentInParent<PlayerHealth>() == null) return;
        PlayerHealth.Instance?.TakeDamage();
        if (_physicsCollider != null)
            PlayerHealth.Instance?.StartInvincibilityIgnore(_physicsCollider);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_isDead) return;

        if (other.CompareTag(DownAttackTag))
        {
            if (PlayerMovement.Instance == null || !PlayerMovement.Instance.TryRegisterHit(GetInstanceID())) return;
            int dmg = PlayerMovement.Instance.attackDamage;
            _health.TakeDamage(dmg);
            SoulManager.Instance?.AddSoul(1);

            if (!_isDead)
                StartCoroutine(KnockbackRoutine());

            PlayerMovement movement = other.GetComponentInParent<PlayerMovement>();
            if (movement != null)
            {
                movement.rb.linearVelocity = new Vector2(movement.rb.linearVelocity.x, bounceForce);
                movement.RefreshJumps();
            }
            return;
        }

        if (other.CompareTag(AttackTag))
        {
            if (PlayerMovement.Instance == null || !PlayerMovement.Instance.TryRegisterHit(GetInstanceID())) return;
            int dmg = PlayerMovement.Instance.attackDamage;
            _health.TakeDamage(dmg);
            SoulManager.Instance?.AddSoul(1);

            if (!_isDead)
                StartCoroutine(KnockbackRoutine());

            PlayerMovement.Instance?.ApplyAttackKnockback(transform.position);
        }
    }

    private IEnumerator KnockbackRoutine()
    {
        _waiting = true;
        float dir = PlayerMovement.Instance != null
            ? Mathf.Sign(transform.position.x - PlayerMovement.Instance.transform.position.x)
            : 1f;
        _rb.linearVelocity = new Vector2(dir * knockbackForce, _rb.linearVelocity.y);

        yield return new WaitForSeconds(knockbackDuration);

        _waiting = false;
    }

    protected virtual void HandleDeath()
    {
        _isDead = true;
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        _rb.linearVelocity = Vector2.zero;
        CurrencyManager.Instance?.AddSouls(soulsDrop);
        SpawnDeathParticles();
    }

    private void SpawnDeathParticles()
    {
        var go = new GameObject("Enemy_DeathParticles");
        go.transform.position = transform.position;

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration        = 0.5f;
        main.loop            = false;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(2f, 8f);
        main.startSize       = new ParticleSystem.MinMaxCurve(0.05f, 0.2f);
        main.startColor      = new ParticleSystem.MinMaxGradient(particleColor);
        main.gravityModifier = 0.4f;
        main.maxParticles    = 25;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.enabled      = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 20) });

        var shape = ps.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius    = 0.3f;

        ps.Play();
        Destroy(go, 1.5f);
    }
}
