using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CoinPickupNotificationUI : MonoBehaviour
{
    [Header("References")]
    public Image coinIcon;
    public TextMeshProUGUI coinText;
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
        GameManager.OnCoinCollected += ShowNotification;
    }

    private void OnDisable()
    {
        GameManager.OnCoinCollected -= ShowNotification;
    }

    private void ShowNotification(int _)
    {
        if (_activeCoroutine != null)
            StopCoroutine(_activeCoroutine);

        if (coinText != null)
            coinText.text = "Coin collected!";

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
