// using UnityEngine;
// using UnityEngine.UI;

// public class HealthUI : MonoBehaviour
// {
//     [Header("References")]
//     public PlayerHealth playerHealth;

//     [Header("Heart Setup")]
//     public Sprite heartSprite;
//     public Sprite emptyHeartSprite;

//     [Header("Layout")]
//     public Vector2 heartSize = new Vector2(48f, 48f);
//     public float heartSpacing = 8f;
//     // Negative X = inset from right edge, negative Y = inset from top edge
//     public Vector2 startAnchoredPosition = new Vector2(24f, -24f);

//     private Image[] heartImages;

//     private void Start()
//     {
//         if (playerHealth == null)
//             playerHealth = PlayerHealth.Instance;

//         BuildHearts();
//         playerHealth.OnLivesChanged += RefreshHearts;
//     }

//     private void OnDestroy()
//     {
//         if (playerHealth != null)
//             playerHealth.OnLivesChanged -= RefreshHearts;
//     }

//     private void BuildHearts()
//     {
//         int count = playerHealth.maxLives;
//         heartImages = new Image[count];

//         // Anchor the panel itself to the top-right corner of the Canvas
//         RectTransform panelRT = GetComponent<RectTransform>();
//         if (panelRT != null)
//         {
//             panelRT.anchorMin = new Vector2(0f, 1f);
//             panelRT.anchorMax = new Vector2(0f, 1f);
//             panelRT.pivot = new Vector2(0f, 1f);
//             panelRT.anchoredPosition = Vector2.zero;
//             panelRT.sizeDelta = Vector2.zero;
//         }

//         for (int i = 0; i < count; i++)
//         {
//             GameObject heartGO = new GameObject($"Heart_{i}");
//             heartGO.transform.SetParent(transform, false);

//             RectTransform rt = heartGO.AddComponent<RectTransform>();
//             rt.sizeDelta = heartSize;

//             // Anchor each heart to the top-left; lay them out going right
//             rt.anchorMin = new Vector2(0f, 1f);
//             rt.anchorMax = new Vector2(0f, 1f);
//             rt.pivot = new Vector2(0f, 1f);

//             float offsetX = startAnchoredPosition.x + i * (heartSize.x + heartSpacing);
//             rt.anchoredPosition = new Vector2(offsetX, startAnchoredPosition.y);

//             Image img = heartGO.AddComponent<Image>();
//             img.sprite = heartSprite;
//             img.preserveAspect = true;

//             heartImages[i] = img;
//         }
//     }

//     /// <summary>Refreshes heart visuals to reflect current lives count.</summary>
//     public void RefreshHearts(int currentLives)
//     {
//         for (int i = 0; i < heartImages.Length; i++)
//         {
//             bool filled = i < currentLives;
//             heartImages[i].sprite = filled ? heartSprite : emptyHeartSprite;

//             if (emptyHeartSprite == null)
//                 heartImages[i].color = filled ? Color.white : new Color(1f, 1f, 1f, 0.25f);
//         }
//     }
// }
using UnityEngine;
using UnityEngine.UI;

public class HealthUI : MonoBehaviour
{
    [Header("References")]
    public PlayerHealth playerHealth;

    [Header("Heart Setup")]
    public Sprite heartSprite;
    public Sprite emptyHeartSprite;

    [Header("Layout")]
    public Vector2 heartSize = new Vector2(48f, 48f);
    public float heartSpacing = 8f;
    [SerializeField] private Vector2 heartsOffset = new Vector2(80f, -22.09998f);

    [Header("Sorting")]
    [SerializeField] private Canvas _canvas;

    private Image[] _heartImages;

    private void Start()
    {
        if (playerHealth == null)
            playerHealth = PlayerHealth.Instance;

        if (_canvas == null) _canvas = GetComponent<Canvas>();
        if (_canvas != null) _canvas.sortingOrder = 10;
        InventoryUI.OnInventoryToggled += OnInventoryToggled;

        BuildHearts(playerHealth.maxLives);
        playerHealth.OnLivesChanged += RefreshHearts;
        playerHealth.OnMaxLivesChanged += OnMaxLivesChanged;
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.OnLivesChanged -= RefreshHearts;
            playerHealth.OnMaxLivesChanged -= OnMaxLivesChanged;
        }
        InventoryUI.OnInventoryToggled -= OnInventoryToggled;
    }

    private void OnInventoryToggled(bool isOpen)
    {
        if (_canvas != null)
            _canvas.sortingOrder = isOpen ? 0 : 10;
    }

    private void OnMaxLivesChanged(int newMaxLives)
    {
        BuildHearts(newMaxLives);
        RefreshHearts(playerHealth.GetCurrentLives());
    }

    private void BuildHearts(int count)
    {
        // Sterge inimile vechi
        foreach (Transform child in transform)
            Destroy(child.gameObject);

        _heartImages = new Image[count];

        RectTransform panelRT = GetComponent<RectTransform>();
        if (panelRT != null)
        {
            panelRT.anchorMin = new Vector2(0f, 1f);
            panelRT.anchorMax = new Vector2(0f, 1f);
            panelRT.pivot     = new Vector2(0f, 1f);
            panelRT.anchoredPosition = Vector2.zero;
            panelRT.sizeDelta = Vector2.zero;
        }

        for (int i = 0; i < count; i++)
        {
            GameObject heartGO = new GameObject($"Heart_{i}");
            heartGO.transform.SetParent(transform, false);

            RectTransform rt = heartGO.AddComponent<RectTransform>();
            rt.sizeDelta = heartSize;

            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot     = new Vector2(0f, 1f);

            float offsetX = heartsOffset.x + i * (heartSize.x + heartSpacing);
            rt.anchoredPosition = new Vector2(offsetX, heartsOffset.y);

            Image img = heartGO.AddComponent<Image>();
            img.sprite = heartSprite;
            img.preserveAspect = true;

            _heartImages[i] = img;
        }
    }

    public void RefreshHearts(int currentLives)
    {
        for (int i = 0; i < _heartImages.Length; i++)
        {
            bool filled = i < currentLives;
            _heartImages[i].sprite = filled ? heartSprite : emptyHeartSprite;

            if (emptyHeartSprite == null)
                _heartImages[i].color = filled ? Color.white : new Color(1f, 1f, 1f, 0.25f);
        }
    }
}