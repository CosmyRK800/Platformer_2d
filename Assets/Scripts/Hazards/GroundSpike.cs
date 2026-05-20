using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class GroundSpike : MonoBehaviour
{
    [Header("Height & Timing")]
    [SerializeField] private float riseHeight   = 2f;
    [SerializeField] private float riseTime     = 0.5f;
    [SerializeField] private float stayUpTime   = 2f;
    [SerializeField] private float stayDownTime = 1f;

    [Header("Bounce")]
    [SerializeField] private float bounceForce  = 15f;

    private Vector3 _downPosition;
    private Vector3 _upPosition;

    private const string PlayerTag     = "Player";
    private const string DownAttackTag = "DownAttack";

    private void Awake()
    {
        Rigidbody2D rb   = GetComponent<Rigidbody2D>();
        rb.bodyType      = RigidbodyType2D.Kinematic;
        rb.gravityScale  = 0f;
        rb.constraints   = RigidbodyConstraints2D.FreezeRotation;

        _downPosition = transform.position;
        _upPosition   = _downPosition + Vector3.up * riseHeight;
    }

    private void Start()
    {
        StartCoroutine(SpikeLoop());
    }

    // ── Cycle ───────────────────────────────────────────────────────────────

    private IEnumerator SpikeLoop()
    {
        while (true)
        {
            yield return StartCoroutine(MoveSpike(_downPosition, _upPosition, riseTime));
            yield return new WaitForSeconds(stayUpTime);
            yield return StartCoroutine(MoveSpike(_upPosition, _downPosition, riseTime));
            yield return new WaitForSeconds(stayDownTime);
        }
    }

    private IEnumerator MoveSpike(Vector3 from, Vector3 to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        transform.position = to;
    }

    // ── Damage & bounce ─────────────────────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(PlayerTag))
        {
            PlayerHealth.Instance?.TakeDamage();
            return;
        }

        if (other.CompareTag(DownAttackTag))
        {
            PlayerMovement movement = other.GetComponentInParent<PlayerMovement>();
            if (movement != null)
            {
                movement.rb.linearVelocity = new Vector2(movement.rb.linearVelocity.x, bounceForce);
                movement.RefreshJumps();
            }
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag(PlayerTag))
            PlayerHealth.Instance?.TakeDamage();
    }
}
