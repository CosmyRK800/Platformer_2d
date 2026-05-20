using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Singleton that handles zone-to-zone transitions within a single scene.
/// Fades the screen to black, teleports the player, then fades back in.
/// If a transition is already running when a new one arrives, the running
/// coroutine is cancelled and restarted with the new destination, so that
/// fall/jump entries that fire OnTriggerEnter2D mid-fade are never silently dropped.
/// </summary>
public class ZoneTransitionManager : MonoBehaviour
{
    public static ZoneTransitionManager Instance { get; private set; }

    [Header("Fade Settings")]
    public float fadeDuration = 0.4f;
    public float holdDuration = 0.6f;

    private Image _overlay;
    private bool _isTransitioning;
    private Coroutine _activeTransition;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        BuildOverlay();
    }

    private void BuildOverlay()
    {
        var canvasGO = new GameObject("TransitionCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        var imageGO = new GameObject("BlackOverlay");
        imageGO.transform.SetParent(canvasGO.transform, false);

        _overlay = imageGO.AddComponent<Image>();
        _overlay.color = new Color(0f, 0f, 0f, 0f);

        var rect = _overlay.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    /// <summary>
    /// Initiates a transition: fades out, teleports the player, then fades back in.
    /// Ignored if a transition is already running, preventing re-trigger loops when
    /// the player spawns near the trigger after teleport.
    /// </summary>
    /// <param name="player">The player Transform to teleport.</param>
    /// <param name="destination">World-space position to teleport the player to.</param>
    public void Transition(Transform player, Vector3 destination)
    {
        if (_isTransitioning)
            return;

        _activeTransition = StartCoroutine(RunTransition(player, destination));
    }

    private IEnumerator RunTransition(Transform player, Vector3 destination)
    {
        _isTransitioning = true;

        yield return StartCoroutine(Fade(0f, 1f));
        yield return new WaitForSeconds(holdDuration);

        // Teleport — zero velocity so the player lands cleanly; the black screen hides the snap
        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        player.position = destination;

        yield return StartCoroutine(Fade(1f, 0f));

        _isTransitioning = false;
        _activeTransition = null;
    }

    private IEnumerator Fade(float from, float to)
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            _overlay.color = new Color(0f, 0f, 0f, Mathf.Lerp(from, to, t));
            yield return null;
        }

        _overlay.color = new Color(0f, 0f, 0f, to);
    }
}
