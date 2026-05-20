using System.Collections;
using UnityEngine;

public class BatEnemy : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator         _animator;
    [SerializeField] private EnemyHealth      _health;
    [SerializeField] private CircleCollider2D _physicsCollider;
    [SerializeField] private Transform        _attackHitboxTransform;
    [SerializeField] private Collider2D       _attackHitbox;

    [Header("Detection")]
    [SerializeField] private float     _detectionRadius = 6f;
    [SerializeField] private float     _chaseRadius     = 10f;
    [SerializeField] private float     _attackRadius    = 1.5f;
    [SerializeField] private LayerMask _playerLayer;

    [Header("Movement")]
    [SerializeField] private float     _moveSpeed         = 3f;
    [SerializeField] private float     _chaseAcceleration = 8f;
    [SerializeField] private LayerMask _groundLayer;

    [Header("Steering")]
    [SerializeField] private int   _rayCount       = 5;
    [SerializeField] private float _rayAngleSpread = 60f;
    [SerializeField] private float _rayDistance    = 1.5f;

    [Header("Combat")]
    [SerializeField] private float _attackCooldown       = 1.5f;
    [SerializeField] private int   _soulsDrop            = 5;
    [SerializeField] private float _attackHitboxDuration = 0.3f;

    private static readonly int HashIsFlying    = Animator.StringToHash("isFlying");
    private static readonly int HashIsAttacking = Animator.StringToHash("isAttacking");
    private static readonly int HashIsWaking    = Animator.StringToHash("isWaking");
    private static readonly int HashIsHurt      = Animator.StringToHash("isHurt");
    private static readonly int HashIsDead      = Animator.StringToHash("isDead");

    private Rigidbody2D _rb;
    private Collider2D  _triggerCollider;
    private Transform   _player;
    private Rigidbody2D _playerRb;
    private Coroutine   _currentStateCoroutine;

    private enum BatState { Sleeping, WakingUp, Chasing, Attacking, Dead }
    private BatState _state;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();

        foreach (var col in GetComponents<Collider2D>())
            if (col.isTrigger) { _triggerCollider = col; break; }
    }

    private void OnEnable()
    {
        if (_health != null) _health.OnDeath += HandleDeath;
    }

    private void OnDisable()
    {
        if (_health != null) _health.OnDeath -= HandleDeath;
    }

    private void Start()
    {
        var playerGo = GameObject.FindWithTag("Player");
        if (playerGo != null)
        {
            _player   = playerGo.transform;
            _playerRb = playerGo.GetComponent<Rigidbody2D>();
        }
        _attackHitbox.enabled = false;
        SetState(BatState.Sleeping);
    }

    private void Update()
    {
        if (_state == BatState.Dead) return;
        if (_player != null) FacePlayer();
    }

    private void SetState(BatState newState)
    {
        if (_currentStateCoroutine != null)
            StopCoroutine(_currentStateCoroutine);

        _state = newState;

        switch (newState)
        {
            case BatState.Sleeping:  _currentStateCoroutine = StartCoroutine(SleepingRoutine());  break;
            case BatState.WakingUp:  _currentStateCoroutine = StartCoroutine(WakingUpRoutine());  break;
            case BatState.Chasing:   _currentStateCoroutine = StartCoroutine(ChasingRoutine());   break;
            case BatState.Attacking: _currentStateCoroutine = StartCoroutine(AttackingRoutine()); break;
            case BatState.Dead:      EnterDead();                                                  break;
        }
    }

    private IEnumerator SleepingRoutine()
    {
        _animator.SetBool(HashIsFlying, false);

        while (true)
        {
            if (_player != null && Physics2D.OverlapCircle(transform.position, _detectionRadius, _playerLayer) != null)
            {
                SetState(BatState.WakingUp);
                yield break;
            }
            yield return null;
        }
    }

    private IEnumerator WakingUpRoutine()
    {
        _animator.SetTrigger(HashIsWaking);
        yield return new WaitForSeconds(GetClipDuration("WakeUp"));
        SetState(BatState.Chasing);
    }

    private IEnumerator ChasingRoutine()
    {
        _animator.SetBool(HashIsFlying, true);

        while (true)
        {
            if (_player == null) { yield return null; continue; }

            float dist = Vector2.Distance(transform.position, _player.position);

            if (dist <= _attackRadius)
            {
                SetState(BatState.Attacking);
                yield break;
            }

            if (dist > _chaseRadius)
            {
                float hover = Mathf.Sin(Time.time * 2f) * 0.3f;
                _rb.linearVelocity = Vector2.Lerp(_rb.linearVelocity, new Vector2(0f, hover), Time.deltaTime * _chaseAcceleration);
            }
            else
            {
                Vector2 predictedPos = _playerRb != null
                    ? (Vector2)_player.position + _playerRb.linearVelocity * 0.3f
                    : (Vector2)_player.position;
                Vector2 toPlayer   = (predictedPos - (Vector2)transform.position).normalized;
                Vector2 steer      = GetSteerDirection(toPlayer);
                _rb.linearVelocity = Vector2.Lerp(_rb.linearVelocity, steer * _moveSpeed, Time.deltaTime * _chaseAcceleration);
            }

            yield return null;
        }
    }

    private IEnumerator AttackingRoutine()
    {
        _rb.linearVelocity = Vector2.zero;
        _animator.SetBool(HashIsFlying,    false);
        _animator.SetBool(HashIsAttacking, true);
        _attackHitbox.enabled    = true;

        yield return new WaitForSeconds(_attackHitboxDuration);

        _attackHitbox.enabled = false;
        _animator.SetBool(HashIsAttacking, false);

        yield return new WaitForSeconds(_attackCooldown);

        if (_state != BatState.Dead)
            SetState(BatState.Chasing);
    }

    private void EnterDead()
    {
        StopAllCoroutines();
        _rb.linearVelocity       = Vector2.zero;
        _triggerCollider.enabled = false;
        _physicsCollider.enabled = false;
        _attackHitbox.enabled    = false;
        _animator.SetBool(HashIsFlying,    false);
        _animator.SetBool(HashIsAttacking, false);
        _animator.SetBool(HashIsDead,      true);
        if (CurrencyManager.Instance != null) CurrencyManager.Instance.AddSouls(_soulsDrop);
        if (SoulManager.Instance != null)     SoulManager.Instance.AddSoul(1);
    }

    private void HandleDeath() => SetState(BatState.Dead);

    private void FacePlayer()
    {
        float dir = _player.position.x - transform.position.x;
        if (Mathf.Approximately(dir, 0f)) return;
        Vector3 s = transform.localScale;
        s.x = dir > 0f ? -Mathf.Abs(s.x) : Mathf.Abs(s.x);
        transform.localScale = s;
    }

    private Vector2 GetSteerDirection(Vector2 desiredDir)
    {
        Vector2 steerSum = Vector2.zero;

        for (int i = 0; i < _rayCount; i++)
        {
            float   t      = _rayCount <= 1 ? 0.5f : (float)i / (_rayCount - 1);
            float   angle  = Mathf.Lerp(-_rayAngleSpread, _rayAngleSpread, t);
            Vector2 dir    = Rotate(desiredDir, angle);
            bool blocked   = Physics2D.Raycast(transform.position, dir, _rayDistance, _groundLayer);
            float  weight  = blocked ? 0f : Vector2.Dot(dir, desiredDir);
            steerSum      += dir * weight;
        }

        if (Physics2D.Raycast(transform.position, desiredDir, _rayDistance, _groundLayer))
        {
            Vector2 perpL = new(-desiredDir.y,  desiredDir.x);
            Vector2 perpR = new( desiredDir.y, -desiredDir.x);
            if (!Physics2D.Raycast(transform.position, perpL, _rayDistance, _groundLayer)) steerSum += perpL;
            if (!Physics2D.Raycast(transform.position, perpR, _rayDistance, _groundLayer)) steerSum += perpR;
        }

        return steerSum.sqrMagnitude > 0.001f ? steerSum.normalized : desiredDir;
    }

    private static Vector2 Rotate(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_state == BatState.Dead) return;

        bool isAttack     = other.CompareTag("Attack");
        bool isDownAttack = other.CompareTag("DownAttack");

        if (!isAttack && !isDownAttack) return;
        if (PlayerMovement.Instance == null) return;
        if (!PlayerMovement.Instance.TryRegisterHit(GetInstanceID())) return;

        _health.TakeDamage(PlayerMovement.Instance.attackDamage);
        if (SoulManager.Instance != null) SoulManager.Instance.AddSoul(1);
        PlayerMovement.Instance.ApplyAttackKnockback(transform.position);

        if (isDownAttack)
        {
            PlayerMovement pm = other.GetComponentInParent<PlayerMovement>();
            if (pm != null)
            {
                pm.rb.linearVelocity = new Vector2(pm.rb.linearVelocity.x, 15f);
                pm.RefreshJumps();
            }
        }

        if (_state != BatState.Dead)
            _animator.SetTrigger(HashIsHurt);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (_state == BatState.Dead || _state == BatState.Sleeping) return;
        if (other.GetComponentInParent<PlayerHealth>() == null) return;
        if (PlayerHealth.Instance != null) PlayerHealth.Instance.TakeDamage();
    }

    private float GetClipDuration(string clipName)
    {
        foreach (var clip in _animator.runtimeAnimatorController.animationClips)
            if (clip.name == clipName) return clip.length;

        Debug.LogWarning($"BatEnemy: clip '{clipName}' not found, using fallback 1f.");
        return 1f;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _detectionRadius);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, _chaseRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _attackRadius);
    }
}
