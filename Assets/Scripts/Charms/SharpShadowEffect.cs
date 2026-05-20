using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SharpShadowEffect", menuName = "Charms/Effects/Sharp Shadow")]
public class SharpShadowEffect : CharmEffect
{
    public override void Apply(PlayerHealth health)
    {
        if (PlayerMovement.Instance == null) return;
        PlayerMovement.Instance.gameObject.AddComponent<SharpShadowMonitor>();
    }

    public override void Remove(PlayerHealth health)
    {
        if (PlayerMovement.Instance == null) return;
        var monitor = PlayerMovement.Instance.GetComponent<SharpShadowMonitor>();
        if (monitor != null)
            Object.Destroy(monitor);
    }
}

public class SharpShadowMonitor : MonoBehaviour
{
    [SerializeField] private int dashDamage = 1;

    private bool _isDashing;
    private readonly HashSet<int> _hitEnemiesThisDash = new HashSet<int>();

    private Collider2D _playerCollider;
    private readonly List<Collider2D> _ignoredColliders = new List<Collider2D>();

    private void Awake()
    {
        _playerCollider = GetComponent<Collider2D>();
        if (_playerCollider == null)
            _playerCollider = GetComponentInChildren<Collider2D>();
    }

    private void Start()
    {
        StartCoroutine(WatchDash());
    }

    private IEnumerator WatchDash()
    {
        while (true)
        {
            bool pmDashing = PlayerMovement.Instance != null && PlayerMovement.Instance.IsDashing;

            if (pmDashing && !_isDashing)
                OnDashStart();
            else if (!pmDashing && _isDashing)
                OnDashEnd();

            yield return null;
        }
    }

    private void OnDashStart()
    {
        _isDashing = true;
        _hitEnemiesThisDash.Clear();
        PlayerHealth.Instance?.SetInvincible(true);

        if (_playerCollider == null) return;
        foreach (var enemy in FindObjectsByType<BaseEnemy>(FindObjectsSortMode.None))
        {
            var col = enemy.GetPhysicsCollider();
            if (col == null) continue;
            Physics2D.IgnoreCollision(_playerCollider, col, true);
            _ignoredColliders.Add(col);
        }
    }

    private void OnDashEnd()
    {
        _isDashing = false;
        PlayerHealth.Instance?.SetInvincible(false);

        foreach (var col in _ignoredColliders)
            if (col != null && _playerCollider != null)
                Physics2D.IgnoreCollision(_playerCollider, col, false);
        _ignoredColliders.Clear();
    }

    private void OnTriggerEnter2D(Collider2D other) => HandleOverlap(other);
    private void OnTriggerStay2D(Collider2D other)  => HandleOverlap(other);

    private void HandleOverlap(Collider2D other)
    {
        if (!_isDashing) return;
        if (!other.CompareTag("Enemy")) return;

        int id = other.GetInstanceID();
        if (_hitEnemiesThisDash.Contains(id)) return;
        _hitEnemiesThisDash.Add(id);

        other.GetComponentInParent<EnemyHealth>()?.TakeDamage(dashDamage);
    }

    private void OnDestroy()
    {
        if (_isDashing)
        {
            PlayerHealth.Instance?.SetInvincible(false);
            foreach (var col in _ignoredColliders)
                if (col != null && _playerCollider != null)
                    Physics2D.IgnoreCollision(_playerCollider, col, false);
            _ignoredColliders.Clear();
        }
    }
}
