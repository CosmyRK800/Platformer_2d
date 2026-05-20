using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SoulUI : MonoBehaviour
{
    [Header("Images")]
    [SerializeField] private Image backgroundCircle;
    [SerializeField] private Image fillCircle;
    [SerializeField] private Image glowCircle;

    [Header("Colors")]
    [SerializeField] private Color canHealColor    = Color.white;
    [SerializeField] private Color cannotHealColor = new Color(0.3f, 0.3f, 0.3f, 1f);

    [Header("Fill")]
    [SerializeField] private float lerpSpeed = 5f;

    [Header("Glow")]
    [SerializeField] private float glowMinAlpha = 0.2f;
    [SerializeField] private float glowMaxAlpha = 0.8f;
    [SerializeField] private float glowPeriod   = 1f;

    [Header("Position")]
    [SerializeField] private Vector2 soulOffset  = new Vector2(20f, -30f);
    [SerializeField] private Vector2 widgetSize  = new Vector2(50f, 50f);
    [SerializeField] private RectTransform backgroundCircleRect;
    [SerializeField] private RectTransform fillCircleRect;
    [SerializeField] private RectTransform glowCircleRect;

    [Header("Sorting")]
    [SerializeField] private Canvas _canvas;

    private float     _targetFill;
    private Coroutine _glowRoutine;

    private void Awake()
    {
        RectTransform rt = GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin        = new Vector2(0f, 1f);
            rt.anchorMax        = new Vector2(0f, 1f);
            rt.pivot            = new Vector2(0f, 1f);
            rt.anchoredPosition = soulOffset;
            rt.sizeDelta        = widgetSize;
        }

        if (backgroundCircleRect != null)
            backgroundCircleRect.anchoredPosition = new Vector2(31.9f, -12.4f);

        if (fillCircleRect != null)
            fillCircleRect.anchoredPosition = new Vector2(31.90002f, -12.3999f);

        if (glowCircleRect != null)
            glowCircleRect.anchoredPosition = new Vector2(31.9f, -12.4f);
    }

    private void OnEnable()
    {
        SoulManager.OnSoulChanged += OnSoulChanged;
    }

    private void OnDisable()
    {
        SoulManager.OnSoulChanged -= OnSoulChanged;
    }

    private void Start()
    {
        if (_canvas == null) _canvas = GetComponent<Canvas>();
        if (_canvas != null) _canvas.sortingOrder = 10;
        InventoryUI.OnInventoryToggled += OnInventoryToggled;

        if (SoulManager.Instance != null)
            Refresh(SoulManager.Instance.CurrentSoul, SoulManager.Instance.maxSoul);
        else
            Refresh(0, 1);
    }

    private void OnDestroy()
    {
        InventoryUI.OnInventoryToggled -= OnInventoryToggled;
    }

    private void OnInventoryToggled(bool isOpen)
    {
        if (_canvas != null)
            _canvas.sortingOrder = isOpen ? 0 : 10;
    }

    private void Update()
    {
        if (fillCircle == null) return;
        fillCircle.fillAmount = Mathf.Lerp(fillCircle.fillAmount, _targetFill, Time.deltaTime * lerpSpeed);
        if (glowCircle != null)
            glowCircle.fillAmount = fillCircle.fillAmount;
    }

    private void OnSoulChanged(int current, int max)
    {
        _targetFill = max > 0 ? current / (float)max : 0f;
        UpdateVisuals(SoulManager.Instance != null && SoulManager.Instance.CanHeal());
    }

    private void Refresh(int current, int max)
    {
        _targetFill = max > 0 ? current / (float)max : 0f;

        if (fillCircle != null)
            fillCircle.fillAmount = _targetFill;

        UpdateVisuals(SoulManager.Instance != null && SoulManager.Instance.CanHeal());
    }

    private void UpdateVisuals(bool canHeal)
    {
        if (fillCircle != null)
            fillCircle.color = canHeal ? canHealColor : cannotHealColor;

        if (canHeal)
        {
            if (_glowRoutine == null)
                _glowRoutine = StartCoroutine(GlowPulse());
        }
        else
        {
            if (_glowRoutine != null)
            {
                StopCoroutine(_glowRoutine);
                _glowRoutine = null;
            }

            if (glowCircle != null)
            {
                Color c = glowCircle.color;
                c.a = 0f;
                glowCircle.color = c;
            }
        }
    }

    private IEnumerator GlowPulse()
    {
        float t = 0f;
        while (true)
        {
            t += Time.deltaTime;
            float normalized = (Mathf.Sin(t * Mathf.PI * 2f / glowPeriod) + 1f) * 0.5f;
            float alpha = Mathf.Lerp(glowMinAlpha, glowMaxAlpha, normalized);

            if (glowCircle != null)
            {
                Color c = glowCircle.color;
                c.a = alpha;
                glowCircle.color = c;
            }

            yield return null;
        }
    }
}
