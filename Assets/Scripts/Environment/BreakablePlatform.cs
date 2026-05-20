using System.Collections;
using UnityEngine;

/// <summary>
/// A solid platform that shakes and permanently disappears after the player
/// stands on it for a set delay.
/// Requires a BoxCollider2D and a SpriteRenderer.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class BreakablePlatform : MonoBehaviour
{
    [Header("Timing")]
    [Tooltip("Seconds of player contact before the platform breaks.")]
    public float breakDelay = 1f;

    [Header("Shake")]
    [Tooltip("Horizontal shake amplitude in world units.")]
    public float shakeAmplitude = 0.05f;

    [Tooltip("Shake oscillations per second.")]
    public float shakeFrequency = 30f;

    [Header("Visual")]
    [Tooltip("Color tint applied to the SpriteRenderer while the platform is about to break.")]
    public Color warningColor = new Color(1f, 0.4f, 0.4f, 1f);

    [Tooltip("Duration of the fade-out when the platform breaks.")]
    public float fadeOutDuration = 0.2f;

    // ── Internal state ──────────────────────────────────────────────────────
    private BoxCollider2D col;
    private SpriteRenderer spriteRenderer;
    private Vector3 originPosition;
    private Color originalColor;
    private bool isTriggered;

    private const string PlayerTag = "Player";

    private void Awake()
    {
        col = GetComponent<BoxCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        originPosition = transform.position;
        originalColor = spriteRenderer.color;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isTriggered)
            return;

        if (!collision.gameObject.CompareTag(PlayerTag))
            return;

        if (!IsPlayerOnTop(collision))
            return;

        isTriggered = true;
        StartCoroutine(BreakSequence());
    }

    // ── Core sequence ───────────────────────────────────────────────────────

    private IEnumerator BreakSequence()
    {
        yield return StartCoroutine(ShakeAndWarn(breakDelay));

        col.enabled = false;

        yield return StartCoroutine(FadeOut());

        gameObject.SetActive(false);
    }

    private IEnumerator ShakeAndWarn(float duration)
    {
        spriteRenderer.color = warningColor;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float offset = Mathf.Sin(elapsed * shakeFrequency * Mathf.PI * 2f) * shakeAmplitude;
            transform.position = originPosition + new Vector3(offset, 0f, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = originPosition;
    }

    private IEnumerator FadeOut()
    {
        Color transparent = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
        float elapsed = 0f;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            spriteRenderer.color = Color.Lerp(originalColor, transparent, elapsed / fadeOutDuration);
            yield return null;
        }
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    /// <summary>Returns true if the player is above the platform surface.</summary>
    private static bool IsPlayerOnTop(Collision2D collision)
    {
        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (contact.normal.y < -0.5f)
                return true;
        }
        return false;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        BoxCollider2D b = GetComponent<BoxCollider2D>();
        Gizmos.DrawWireCube(
            Application.isPlaying ? originPosition : transform.position,
            b != null ? (Vector3)b.size : Vector3.one);
    }
#endif
}
