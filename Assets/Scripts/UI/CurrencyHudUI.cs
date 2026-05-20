using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CurrencyHudUI : MonoBehaviour
{
    [Header("HUD")]
    [SerializeField] private GameObject      currencyHUDContent;
    [SerializeField] private Image           currencyIcon;
    [SerializeField] private Sprite          currencySprite;
    [SerializeField] private TextMeshProUGUI currencyCountText;
    [SerializeField] private TextMeshProUGUI currencyDeltaText;

    [Header("Sorting")]
    [SerializeField] private Canvas hudCanvas;

    private Coroutine _showRoutine;

    private void Start()
    {
        if (currencyHUDContent != null)
            currencyHUDContent.SetActive(false);

        if (currencyIcon != null && currencySprite != null)
            currencyIcon.sprite = currencySprite;

        if (hudCanvas != null)
            hudCanvas.sortingOrder = 10;

        int initial = CurrencyManager.Instance?.CurrentSouls ?? 0;
        SetCountText(initial);

        CurrencyManager.OnSoulsChanged  += OnSoulsChanged;
        InventoryUI.OnInventoryToggled  += OnInventoryToggled;
    }

    private void OnDestroy()
    {
        CurrencyManager.OnSoulsChanged  -= OnSoulsChanged;
        InventoryUI.OnInventoryToggled  -= OnInventoryToggled;
    }

    private void OnSoulsChanged(int current, int delta)
    {
        if (delta > 0)
        {
            if (_showRoutine != null) StopCoroutine(_showRoutine);
            _showRoutine = StartCoroutine(ShowRoutine(current, delta));
        }
        else
        {
            if (_showRoutine != null)
            {
                StopCoroutine(_showRoutine);
                _showRoutine = null;
            }
            if (currencyHUDContent != null)
                currencyHUDContent.SetActive(false);
        }
    }

    private void OnInventoryToggled(bool isOpen)
    {
        if (hudCanvas != null)
            hudCanvas.sortingOrder = isOpen ? 0 : 10;
    }

    private IEnumerator ShowRoutine(int current, int delta)
    {
        if (currencyHUDContent != null)
            currencyHUDContent.SetActive(true);

        if (currencyDeltaText != null)
        {
            currencyDeltaText.text  = "+" + delta;
            currencyDeltaText.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            currencyDeltaText.gameObject.SetActive(true);
        }

        // Phase 1 — count up over 0.5s
        int fromValue = current - delta;
        float elapsed = 0f;
        const float countDuration = 0.5f;
        while (elapsed < countDuration)
        {
            elapsed += Time.deltaTime;
            SetCountText(Mathf.RoundToInt(Mathf.Lerp(fromValue, current, elapsed / countDuration)));
            yield return null;
        }
        SetCountText(current);

        // Phase 2 — hold 1s
        yield return new WaitForSeconds(1f);

        // Phase 3 — fade out delta text over 0.5s
        if (currencyDeltaText != null)
        {
            elapsed = 0f;
            Color c = currencyDeltaText.color;
            while (elapsed < 0.5f)
            {
                elapsed += Time.deltaTime;
                currencyDeltaText.color = new Color(c.r, c.g, c.b, Mathf.Lerp(1f, 0f, elapsed / 0.5f));
                yield return null;
            }
            currencyDeltaText.gameObject.SetActive(false);
        }

        if (currencyHUDContent != null)
            currencyHUDContent.SetActive(false);

        _showRoutine = null;
    }

    private void SetCountText(int value)
    {
        if (currencyCountText != null)
            currencyCountText.text = value.ToString();
    }
}
