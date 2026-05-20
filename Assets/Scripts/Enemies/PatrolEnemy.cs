using System.Collections;
using UnityEngine;

public class PatrolEnemy : BaseEnemy
{
    [Header("Patrol")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float patrolDistance = 5f;
    [SerializeField] private float waitTime = 0.5f;

    [Header("Edge / Wall Detection")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float edgeDetectionDistance = 0.5f;

    private Vector2 _startPos;
    private int _direction = 1;

    protected override void Start()
    {
        base.Start();
        _startPos = transform.position;
    }

    private void FixedUpdate()
    {
        if (_isDead || _waiting) return;

        float distFromStart = (_rb.position.x - _startPos.x) * _direction;
        if (distFromStart >= patrolDistance)
        {
            StartCoroutine(TurnAround());
            return;
        }

        if (!IsGroundAhead())
        {
            StartCoroutine(TurnAround());
            return;
        }

        if (IsWallAhead())
        {
            StartCoroutine(TurnAround());
            return;
        }

        _rb.linearVelocity = new Vector2(_direction * moveSpeed, _rb.linearVelocity.y);
    }

    private bool IsGroundAhead()
    {
        var origin = new Vector2(
            transform.position.x + _direction * (_halfWidth + 0.05f),
            transform.position.y - _halfHeight
        );
        return Physics2D.Raycast(origin, Vector2.down, edgeDetectionDistance, groundLayer);
    }

    private bool IsWallAhead()
    {
        var origin = (Vector2)transform.position;
        RaycastHit2D hit = Physics2D.Raycast(origin, new Vector2(_direction, 0f), _halfWidth + 0.15f, groundLayer);
        return hit.collider != null;
    }

    private IEnumerator TurnAround()
    {
        _waiting = true;
        _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);

        yield return new WaitForSeconds(waitTime);

        _direction    = -_direction;
        _sprite.flipX = _direction > 0;
        _waiting      = false;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = Application.isPlaying ? (Vector3)_startPos : transform.position;
        float hw = Application.isPlaying ? _halfWidth  : 0.25f;
        float hh = Application.isPlaying ? _halfHeight : 0.5f;

        Gizmos.color = Color.yellow;
        Vector3 left  = origin + Vector3.left  * patrolDistance;
        Vector3 right = origin + Vector3.right * patrolDistance;
        Gizmos.DrawLine(left, right);
        Gizmos.DrawWireSphere(left,  0.12f);
        Gizmos.DrawWireSphere(right, 0.12f);

        Gizmos.color = Color.red;
        int dir = Application.isPlaying ? _direction : 1;
        Vector3 edgeOrigin = transform.position + new Vector3(dir * (hw + 0.05f), -hh, 0f);
        Gizmos.DrawLine(edgeOrigin, edgeOrigin + Vector3.down * edgeDetectionDistance);

        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawLine(transform.position,
            transform.position + new Vector3(dir * (hw + 0.15f), 0f, 0f));
    }
}
