using System.Collections;
using UnityEngine;

/// <summary>
/// A one-way platform that shakes and falls after the player stands on it for a set delay,
/// then resets to its original position after another delay.
/// Requires a BoxCollider2D with a PlatformEffector2D for jump-through behaviour.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(PlatformEffector2D))]
public class FallingPlatform : MonoBehaviour
{
    [Header("Timing")]
    [Tooltip("Seconds the player must stand on the platform before it falls.")]
    public float fallDelay = 1f;

    [Tooltip("Seconds after falling before the platform resets.")]
    public float resetDelay = 3f;

    [Header("Shake")]
    [Tooltip("Horizontal shake amplitude in world units.")]
    public float shakeAmplitude = 0.05f;

    [Tooltip("Shake oscillations per second.")]
    public float shakeFrequency = 30f;

    [Header("Visual")]
    [Tooltip("Color tint applied to the SpriteRenderer while the platform is about to fall.")]
    public Color warningColor = new Color(1f, 0.4f, 0.4f, 1f);

    // ── Internal state ──────────────────────────────────────────────────────
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Vector3 originPosition;
    private Color originalColor;

    private bool isTriggered;
    private bool isFalling;
    private int playerContactCount;

    private const string PlayerTag = "Player";

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Kinematic until we deliberately drop it.
        rb.bodyType = RigidbodyType2D.Kinematic;

        originPosition = transform.position;

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag(PlayerTag))
            return;

        // Only react when the player lands on top (contact normal pointing upward).
        if (!IsPlayerOnTop(collision))
            return;

        playerContactCount++;

        if (!isTriggered && !isFalling)
        {
            isTriggered = true;
            StartCoroutine(FallSequence());
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag(PlayerTag))
            return;

        playerContactCount = Mathf.Max(0, playerContactCount - 1);
    }

    [Header("Respawn")]
    [Tooltip("Duration of the fade-out when the platform falls.")]
    public float fadeOutDuration = 0.3f;

    [Tooltip("Duration of the fade-in when the platform respawns.")]
    public float fadeInDuration = 0.4f;

    // ── Core sequence ───────────────────────────────────────────────────────

    private IEnumerator FallSequence()
    {
        // Shake + warning color during the countdown.
        yield return StartCoroutine(ShakeAndWarn(fallDelay));

        // Drop — switch to Dynamic so gravity takes over.
        isFalling = true;
        rb.bodyType = RigidbodyType2D.Dynamic;

        // Fade out while the platform is falling.
        yield return StartCoroutine(Fade(originalColor, ToTransparent(originalColor), fadeOutDuration));

        // Wait the remainder of the reset delay hidden, then snap back.
        float timeAlreadyFalling = fadeOutDuration;
        float remainingWait = Mathf.Max(0f, resetDelay - timeAlreadyFalling);
        yield return new WaitForSeconds(remainingWait);

        SnapToOrigin();

        // Fade back in at the origin.
        yield return StartCoroutine(Fade(ToTransparent(originalColor), originalColor, fadeInDuration));

        FinishReset();
    }

    private IEnumerator ShakeAndWarn(float duration)
    {
        if (spriteRenderer != null)
            spriteRenderer.color = warningColor;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            float offset = Mathf.Sin(elapsed * shakeFrequency * Mathf.PI * 2f) * shakeAmplitude;
            transform.position = originPosition + new Vector3(offset, 0f, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Snap back to origin before falling straight down.
        transform.position = originPosition;
    }

    private IEnumerator Fade(Color from, Color to, float duration)
    {
        if (spriteRenderer == null)
        {
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            spriteRenderer.color = Color.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        spriteRenderer.color = to;
    }

    /// <summary>Snaps the platform back to its spawn position without any visual change.</summary>
    private void SnapToOrigin()
    {
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        transform.position = originPosition;
    }

    /// <summary>Clears all triggered state after the respawn fade completes.</summary>
    private void FinishReset()
    {
        isTriggered = false;
        isFalling = false;
        playerContactCount = 0;
    }

    private void ResetPlatform()
    {
        StopAllCoroutines();
        SnapToOrigin();

        isTriggered = false;
        isFalling = false;
        playerContactCount = 0;

        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;
    }

    private static Color ToTransparent(Color c) => new Color(c.r, c.g, c.b, 0f);

    // ── Helpers ─────────────────────────────────────────────────────────────

    /// <summary>Returns true if any contact point on the collision has an upward-facing normal,
    /// meaning the player is above the platform surface.</summary>
    private static bool IsPlayerOnTop(Collision2D collision)
    {
        foreach (ContactPoint2D contact in collision.contacts)
        {
            // Normal points from platform toward player, so an upward normal means player is on top.
            if (contact.normal.y < -0.5f)
                return true;
        }
        return false;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Show the origin the platform will reset to in the editor.
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(Application.isPlaying ? originPosition : transform.position,
            GetComponent<BoxCollider2D>() != null
                ? (Vector3)GetComponent<BoxCollider2D>().size
                : Vector3.one);
    }
#endif
}
