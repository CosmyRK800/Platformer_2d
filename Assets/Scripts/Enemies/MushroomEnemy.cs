using System.Collections;
using UnityEngine;

public class MushroomEnemy : MonoBehaviour
{
    private enum MushroomState { Patrolling, Agro, Charging, Stunned, Dead }

    private static readonly int IsRunningHash = Animator.StringToHash("isRunning");
    private static readonly int AttackHash    = Animator.StringToHash("Attack");
    private static readonly int StunHash      = Animator.StringToHash("Stun");
    private static readonly int HitHash       = Animator.StringToHash("Hit");
    private static readonly int IsDeadHash    = Animator.StringToHash("isDead");

    [Header("References")]
    [SerializeField] private Animator _animator;
    [SerializeField] private EnemyHealth _health;
    [SerializeField] private Rigidbody2D _rb;
    [SerializeField] private Transform _headHitbox;
    [SerializeField] private Collider2D _headHitboxCollider;
    [SerializeField] private MushroomAttackHitbox _attackHitbox;

    [Header("Detection")]
    [SerializeField] private float _agroRadius = 5f;
[SerializeField] private float _wallCheckDistance = 0.5f;
    [SerializeField] private float _edgeCheckDistance = 1f;
    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private LayerMask _playerLayer;

    [Header("Movement")]
    [SerializeField] private float _patrolSpeed = 2f;
    [SerializeField] private float _chargeSpeed = 6f;
    [SerializeField] private float _idlePauseDuration = 1.5f;

    [Header("Combat")]
    [SerializeField] private int _soulsDrop = 5;
#pragma warning disable CS0414
    [SerializeField] private int _contactDamage = 1;
#pragma warning restore CS0414

    private Coroutine _stateCoroutine;
    private Transform _player;
    private float _direction = 1f;
    private bool _isDead;
    private bool _isStunned;
    private Collider2D _rootCollider;
    private float _halfWidth;

    private void Awake()
    {
        _rootCollider = GetComponent<Collider2D>();
        //_rb.freezeRotation = true;
    }

    private void Start()
    {
        _halfWidth = _rootCollider.bounds.extents.x;

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
            _player = playerObj.transform;

        if (_headHitboxCollider != null)
            _headHitboxCollider.enabled = false;

        SetState(MushroomState.Patrolling);
    }

    private void OnEnable()
    {
        if (_health != null)
            _health.OnDeath += HandleDeath;
    }

    private void OnDisable()
    {
        if (_health != null)
            _health.OnDeath -= HandleDeath;
    }

    private void SetState(MushroomState newState)
    {
        if (_stateCoroutine != null)
            StopCoroutine(_stateCoroutine);
        switch (newState)
        {
            case MushroomState.Patrolling: _stateCoroutine = StartCoroutine(PatrolRoutine());  break;
            case MushroomState.Agro:       _stateCoroutine = StartCoroutine(AgroRoutine());    break;
            case MushroomState.Charging:   _stateCoroutine = StartCoroutine(ChargeRoutine());  break;
            case MushroomState.Stunned:    _stateCoroutine = StartCoroutine(StunRoutine());    break;
            case MushroomState.Dead:       EnterDeadState();                                    break;
        }
    }

    private IEnumerator PatrolRoutine()
    {
        _animator.SetBool(IsRunningHash, false);
        FaceDirection(_direction);
        yield return new WaitForSeconds(Random.Range(0f, _idlePauseDuration));
        _animator.SetBool(IsRunningHash, true);
        while (true)
        {
            Collider2D playerHit = Physics2D.OverlapCircle(transform.position, _agroRadius, _playerLayer);
            if (playerHit != null)
            {
                SetState(MushroomState.Agro);
                yield break;
            }

            if (IsEdgeAhead() || IsWallAhead())
            {
                _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
                _animator.SetBool(IsRunningHash, false);
                yield return new WaitForSeconds(_idlePauseDuration);
                _animator.SetBool(IsRunningHash, true);
                _direction = -_direction;
                FaceDirection(_direction);
            }
            else
            {
                _rb.linearVelocity = new Vector2(_direction * _patrolSpeed, _rb.linearVelocity.y);
            }

            yield return null;
        }
    }

    private IEnumerator AgroRoutine()
    {
        _animator.SetBool(IsRunningHash, true);
        while (true)
        {
            if (_player == null)
            {
                SetState(MushroomState.Patrolling);
                yield break;
            }

            FacePlayer();

            float dist = Mathf.Abs(_player.position.x - transform.position.x);
            if (dist < 1.5f)
            {
                SetState(MushroomState.Charging);
                yield break;
            }

            Collider2D playerHit = Physics2D.OverlapCircle(transform.position, _agroRadius, _playerLayer);
            if (playerHit == null)
            {
                SetState(MushroomState.Patrolling);
                yield break;
            }

            if (!IsEdgeAhead() && !IsWallAhead())
                _rb.linearVelocity = new Vector2(_direction * _chargeSpeed, _rb.linearVelocity.y);
            else
                _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);

            yield return null;
        }
    }

    private IEnumerator ChargeRoutine()
    {
        _rb.linearVelocity = Vector2.zero;
        _animator.SetTrigger(AttackHash);

        if (_headHitboxCollider != null)
            _headHitboxCollider.enabled = true;

        float duration = GetClipDuration("Attack");
        yield return new WaitForSeconds(duration);

        if (_headHitboxCollider != null)
            _headHitboxCollider.enabled = false;

        _attackHitbox?.SetActive(false);

        if (_isDead) yield break;

        Collider2D playerHit = Physics2D.OverlapCircle(transform.position, _agroRadius, _playerLayer);
        SetState(playerHit != null ? MushroomState.Agro : MushroomState.Patrolling);
    }

    private IEnumerator StunRoutine()
    {
        _isStunned = true;
        _rb.linearVelocity = Vector2.zero;
        _animator.SetBool(IsRunningHash, false);
        _animator.SetTrigger(StunHash);

        float duration = GetClipDuration("Stun");
        yield return new WaitForSeconds(duration);

        _isStunned = false;
        if (!_isDead)
            SetState(MushroomState.Patrolling);
    }

    private void EnterDeadState()
    {
        _isDead    = true;
        _isStunned = false;
        StopAllCoroutines();
        _rb.linearVelocity = Vector2.zero;
        _rb.bodyType = RigidbodyType2D.Kinematic;
        _animator.SetBool(IsDeadHash, true);

        if (_rootCollider != null)
            _rootCollider.enabled = false;

        CurrencyManager.Instance?.AddSouls(_soulsDrop);
        SoulManager.Instance?.AddSoul(1);
    }

    private void HandleDeath()
    {
        SetState(MushroomState.Dead);
    }

    public void AttackHitboxOn()  => _attackHitbox.SetActive(true);
    public void AttackHitboxOff() => _attackHitbox.SetActive(false);

    public void TriggerStun()
    {
        if (_isDead || _isStunned) return;
        SetState(MushroomState.Stunned);
    }

    private void OnCollisionStay2D(Collision2D other)
    {
        if (_isDead) return;
        if (other.collider.GetComponentInParent<PlayerHealth>() == null) return;
        PlayerHealth.Instance?.TakeDamage();
        PlayerHealth.Instance?.StartInvincibilityIgnore(_rootCollider);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_isDead) return;

        if (other.CompareTag("DownAttack"))
        {
            PlayerMovement pm = PlayerMovement.Instance;
            if (pm == null || !pm.TryRegisterHit(GetInstanceID())) return;
            _health.TakeDamage(pm.attackDamage);
            SoulManager.Instance?.AddSoul(1);
            pm.ApplyAttackKnockback(transform.position);
            pm.rb.linearVelocity = new Vector2(pm.rb.linearVelocity.x, 15f);
            pm.RefreshJumps();
            if (!_isStunned)
                _animator.SetTrigger(HitHash);
            return;
        }

        if (other.CompareTag("Attack"))
        {
            PlayerMovement pm = PlayerMovement.Instance;
            if (pm == null || !pm.TryRegisterHit(GetInstanceID())) return;
            _health.TakeDamage(pm.attackDamage);
            SoulManager.Instance?.AddSoul(1);
            pm.ApplyAttackKnockback(transform.position);
            if (!_isStunned)
                _animator.SetTrigger(HitHash);
        }
    }

    private void FacePlayer()
    {
        if (_player == null) return;
        FaceDirection(Mathf.Sign(_player.position.x - transform.position.x));
    }

    private void FaceDirection(float dir)
    {
        _direction = dir >= 0f ? 1f : -1f;
        Vector3 ls = transform.localScale;
        ls.x = Mathf.Abs(ls.x) * -_direction;
        transform.localScale = ls;
    }

    private float GetClipDuration(string clipName)
    {
        if (_animator == null || _animator.runtimeAnimatorController == null)
            return 1f;
        foreach (AnimationClip clip in _animator.runtimeAnimatorController.animationClips)
            if (clip.name == clipName)
                return clip.length;
        return 1f;
    }

private bool IsWallAhead()
    {
        RaycastHit2D hit = Physics2D.Raycast(
            (Vector2)_rootCollider.bounds.center,
            new Vector2(_direction, 0f),
            _wallCheckDistance,
            _groundLayer);
        return hit.collider != null;
    }

    private bool IsEdgeAhead()
    {
        Vector2 origin = new Vector2(
            _rootCollider.bounds.center.x + _direction * (_halfWidth + 0.05f),
            _rootCollider.bounds.min.y + 0.05f);
        return !Physics2D.Raycast(origin, Vector2.down, _edgeCheckDistance, _groundLayer);
    }
}
