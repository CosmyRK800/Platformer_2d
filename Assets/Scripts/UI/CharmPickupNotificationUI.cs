using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Shows a brief notification in the bottom-right corner when a charm is collected.
/// Attach to a Canvas GameObject in the UI hierarchy.
/// </summary>
public class CharmPickupNotificationUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Image component that displays the charm icon.")]
    public Image charmIcon;

    [Tooltip("TextMeshPro component that displays the charm name.")]
    public TextMeshProUGUI charmNameText;

    [Tooltip("The panel that contains the icon and text.")]
    public GameObject notificationPanel;

    [Header("Timing")]
    [SerializeField] private float _displayDuration = 3f;
    [SerializeField] private float _fadeDuration = 0.5f;

    private CanvasGroup _canvasGroup;
    private Coroutine _activeCoroutine;

    private void Awake()
    {
        _canvasGroup = notificationPanel.GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = notificationPanel.AddComponent<CanvasGroup>();

        notificationPanel.SetActive(false);
    }

    private void OnEnable()
    {
        CharmPickup.OnCharmCollected += ShowNotification;
    }

    private void OnDisable()
    {
        CharmPickup.OnCharmCollected -= ShowNotification;
    }

    private void ShowNotification(CharmData charm)
    {
        if (_activeCoroutine != null)
            StopCoroutine(_activeCoroutine);

        if (charmIcon != null)
            charmIcon.sprite = charm.icon;

        if (charmNameText != null)
            charmNameText.text = charm.charmName;

        _activeCoroutine = StartCoroutine(NotificationRoutine());
    }

    private IEnumerator NotificationRoutine()
    {
        notificationPanel.SetActive(true);
        _canvasGroup.alpha = 1f;

        yield return new WaitForSeconds(_displayDuration);

        float elapsed = 0f;
        while (elapsed < _fadeDuration)
        {
            elapsed += Time.deltaTime;
            _canvasGroup.alpha = 1f - (elapsed / _fadeDuration);
            yield return null;
        }

        _canvasGroup.alpha = 0f;
        notificationPanel.SetActive(false);
    }
}