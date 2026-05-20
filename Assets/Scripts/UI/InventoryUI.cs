using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class InventoryUI : MonoBehaviour
{
    public static event Action OnInventoryChanged;
    public static event Action<bool> OnInventoryToggled;

    // ── Existing fields ───────────────────────────────────────────────────────

    [Header("Panel")]
    [SerializeField] GameObject inventoryPanel;

    [Header("Left Zone")]
    [SerializeField] Transform abilityRow;
    [SerializeField] Transform itemGrid;
    [SerializeField] GameObject itemButtonPrefab;
    [SerializeField] GameObject separator;

    [Header("Right Zone")]
    [SerializeField] Image detailIcon;
    [SerializeField] TextMeshProUGUI detailName;
    [SerializeField] TextMeshProUGUI detailDescription;

    [Header("Ability Sprites")]
    [SerializeField] Sprite dashIcon;
    [SerializeField] Sprite doubleJumpIcon;
    [SerializeField] Sprite wallJumpIcon;

    [Header("Ability Descriptions")]
    [SerializeField] string dashDescription       = "Quickly dash forward.";
    [SerializeField] string doubleJumpDescription = "Jump a second time while airborne.";
    [SerializeField] string wallJumpDescription   = "Push off walls to gain height.";

    [Header("Coin")]
    [SerializeField] Sprite coinSprite;

    [Header("Currency Grid")]
    [SerializeField] private Transform currencyGrid;
    [SerializeField] private Sprite    currencySprite;

    [Header("Colors")]
    [SerializeField] Color itemNormalColor   = new Color(1f, 1f, 1f, 0.8f);
    [SerializeField] Color itemSelectedColor = Color.yellow;

    // ── New fields ────────────────────────────────────────────────────────────

    [Header("Charm Preview Button")]
    [SerializeField] Button charmPreviewButton;
    [SerializeField] Image  charmPreviewButtonImage;

    [Header("Charm Preview Panel")]
    [SerializeField] GameObject        charmPreviewPanel;
    [SerializeField] Button            charmPreviewBackButton;
    [SerializeField] Image             slot1Image;
    [SerializeField] Image             slot2Image;
    [SerializeField] Image             slot3Image;
    [SerializeField] Transform         charmGrid;
    [SerializeField] GameObject        charmButtonPrefab;
    [SerializeField] Image             charmDetailIcon;
    [SerializeField] TextMeshProUGUI   charmDetailName;
    [SerializeField] TextMeshProUGUI   charmDetailDescription;
    [SerializeField] TextMeshProUGUI   benchMessage;

    // ── Public state ──────────────────────────────────────────────────────────

    public static InventoryUI Instance { get; private set; }
    public bool IsOpen { get; private set; }

    // ── Private types ─────────────────────────────────────────────────────────

    private struct ItemEntry
    {
        public string id;
        public string displayName;
        public string description;
        public Sprite icon;
        public bool   isAbility;
    }

    // ── Existing private fields ───────────────────────────────────────────────

    private readonly List<ItemEntry> _items      = new();
    private readonly List<Image>     _itemImages = new();
    private int _selectedIndex;

    // ── New private fields ────────────────────────────────────────────────────

    private int  _currentCategory    = 0;   // 0 = Abilities, 1 = Items
    private int  _categoryIndex      = 0;   // index within the current category
    private bool _arrowFocused       = false;
    private int  _charmSelectedIndex = 0;
    private bool _charmPreviewOpen   = false;
    private bool _charmBackFocused   = false;

    private readonly List<Image>    _charmImages       = new();
    private Coroutine               _benchCoroutine;
    private static readonly WaitForSeconds _benchWait = new(2f);

    private Action<int, int> _onCurrencyChanged;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        Instance = this;

        inventoryPanel.SetActive(false);
        if (charmPreviewPanel != null) charmPreviewPanel.SetActive(false);
        if (benchMessage      != null) benchMessage.gameObject.SetActive(false);

        if (charmPreviewButton != null)
            charmPreviewButton.onClick.AddListener(OpenCharmPreview);

        charmPreviewBackButton?.onClick.AddListener(CloseCharmPreview);
    }

    public static void NotifyChanged() => OnInventoryChanged?.Invoke();

    private void OnEnable()
    {
        OnInventoryChanged             += HandleInventoryChanged;
        _onCurrencyChanged              = (current, delta) => RefreshCurrencyDisplay();
        CurrencyManager.OnSoulsChanged += _onCurrencyChanged;
    }

    private void OnDisable()
    {
        OnInventoryChanged             -= HandleInventoryChanged;
        CurrencyManager.OnSoulsChanged -= _onCurrencyChanged;
    }

    // ── Update / input ────────────────────────────────────────────────────────

    private void Update()
    {
        if (Keyboard.current == null) return;

        // Tab: close charm preview first, otherwise toggle main inventory
        if (Keyboard.current.tabKey.wasPressedThisFrame)
        {
            if (_charmPreviewOpen) { CloseCharmPreview(); return; }
            if (IsOpen) CloseInventory();
            else        TryOpenInventory();
            return;
        }

        if (!IsOpen) return;

        // Charm preview consumes all remaining input while open
        if (_charmPreviewOpen)
        {
            HandleCharmPreviewInput();
            return;
        }

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CloseInventory();
            return;
        }

        // Left arrow
        if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
        {
            if (_arrowFocused)
            {
                // Return focus from arrow to the last item in category 1
                _arrowFocused    = false;
                _currentCategory = 1;
                _categoryIndex   = Mathf.Max(0, GetCategoryItems(1).Count - 1);
                UpdateArrowHighlight();
                RefreshSelection();
            }
            else if (_categoryIndex > 0)
            {
                _categoryIndex--;
                RefreshSelection();
            }
        }

        // Right arrow
        if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
        {
            if (_arrowFocused) return; // arrow already focused, nowhere to go right

            var catItems = GetCategoryItems(_currentCategory);
            bool atLastInCategory = catItems.Count > 0 && _categoryIndex >= catItems.Count - 1;

            if (_items.Count == 0 || atLastInCategory)
            {
                _arrowFocused = true;
                UpdateArrowHighlight();
            }
            else if (_categoryIndex < catItems.Count - 1)
            {
                _categoryIndex++;
                RefreshSelection();
            }
        }

        // Up arrow
        if (Keyboard.current.upArrowKey.wasPressedThisFrame)
        {
            if (_arrowFocused)
            {
                _arrowFocused    = false;
                _currentCategory = 1;
                _categoryIndex   = Mathf.Max(0, GetCategoryItems(1).Count - 1);
                UpdateArrowHighlight();
                RefreshSelection();
                return;
            }
            NavigateCategory(-1);
        }

        // Down arrow
        if (Keyboard.current.downArrowKey.wasPressedThisFrame)
        {
            if (_arrowFocused)
            {
                _arrowFocused    = false;
                _currentCategory = 1;
                _categoryIndex   = Mathf.Max(0, GetCategoryItems(1).Count - 1);
                UpdateArrowHighlight();
                RefreshSelection();
                return;
            }
            NavigateCategory(1);
        }

        // Space: open charm preview only when the arrow button is focused
        if (Keyboard.current.spaceKey.wasPressedThisFrame && _arrowFocused)
            OpenCharmPreview();
    }

    // ── Open / close ──────────────────────────────────────────────────────────

    private void TryOpenInventory()
    {
        if (PlayerMovement.Instance == null)     return;
        if (!PlayerMovement.Instance.IsGrounded) return;
        if (PlayerMovement.Instance.IsSitting)   return;

        IsOpen           = true;
        _currentCategory = 0;
        _categoryIndex   = 0;
        _arrowFocused    = false;
        OnInventoryToggled?.Invoke(true);

        inventoryPanel.SetActive(true);
        PlayerMovement.Instance.SetGameplayBlocked(true);
        UpdateArrowHighlight();
        RefreshAll();
    }

    private void CloseInventory()
    {
        IsOpen        = false;
        OnInventoryToggled?.Invoke(false);
        _arrowFocused = false;
        UpdateArrowHighlight();
        inventoryPanel.SetActive(false);
        PlayerMovement.Instance?.SetGameplayBlocked(false);
    }

    private void HandleInventoryChanged()
    {
        if (IsOpen && !_charmPreviewOpen) RefreshAll();
    }

    // ── Inventory refresh ─────────────────────────────────────────────────────

    private void RefreshAll()
    {
        Debug.Log($"[InventoryUI] RefreshAll: coins={GameManager.Instance?.Coins}");
        BuildItemList();
        InstantiateItems();
        RefreshSeparator();
        RefreshSelection();
        RefreshCurrencyDisplay();
    }

    private void RefreshCurrencyDisplay()
    {
        if (currencyGrid == null) return;

        foreach (Transform child in currencyGrid) Destroy(child.gameObject);

        GameObject btn = Instantiate(itemButtonPrefab, currencyGrid);

        Image icon = btn.GetComponent<Image>();
        if (icon != null) icon.sprite = currencySprite;

        TextMeshProUGUI label = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null)
        {
            label.gameObject.SetActive(true);
            label.text      = CurrencyManager.Instance?.CurrentSouls.ToString() ?? "0";
            label.fontSize  = 38f;
            label.alignment = TextAlignmentOptions.MidlineLeft;

            var labelRect = label.GetComponent<RectTransform>();
            labelRect.anchoredPosition = new Vector2(75f, -4f);
        }
    }

    private void BuildItemList()
    {
        _items.Clear();

        Debug.Log($"[InventoryUI] BuildItemList: coins={GameManager.Instance?.Coins}");

        if (AbilityRegistry.Instance != null)
        {
            if (AbilityRegistry.Instance.DashUnlocked)
                _items.Add(new ItemEntry
                {
                    id = "dash", displayName = "Dash",
                    description = dashDescription, icon = dashIcon, isAbility = true
                });

            if (AbilityRegistry.Instance.DoubleJumpUnlocked)
                _items.Add(new ItemEntry
                {
                    id = "doublejump", displayName = "Double Jump",
                    description = doubleJumpDescription, icon = doubleJumpIcon, isAbility = true
                });

            if (AbilityRegistry.Instance.WallJumpUnlocked)
                _items.Add(new ItemEntry
                {
                    id = "walljump", displayName = "Wall Jump",
                    description = wallJumpDescription, icon = wallJumpIcon, isAbility = true
                });
        }

        if (GameManager.Instance != null && GameManager.Instance.Coins > 0)
            _items.Add(new ItemEntry
            {
                id = "coins", displayName = $"x {GameManager.Instance.Coins}",
                description = "Collected coins.", icon = coinSprite, isAbility = false
            });
    }

    private void InstantiateItems()
    {
        foreach (Transform child in abilityRow) Destroy(child.gameObject);
        foreach (Transform child in itemGrid)   Destroy(child.gameObject);
        _itemImages.Clear();

        foreach (var entry in _items)
        {
            Transform  parent = entry.isAbility ? abilityRow : itemGrid;
            GameObject btn    = Instantiate(itemButtonPrefab, parent);

            Image icon = btn.GetComponent<Image>();
            _itemImages.Add(icon);
            if (icon != null) icon.sprite = entry.icon;

            TextMeshProUGUI label = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                bool show = !entry.isAbility;
                label.gameObject.SetActive(show);
                if (show) label.text = entry.displayName;
            }
        }
    }

    private void RefreshSeparator()
    {
        if (separator == null) return;

        bool hasAbilities = false, hasOther = false;
        foreach (var item in _items)
        {
            if (item.isAbility) hasAbilities = true;
            else                hasOther     = true;
        }
        separator.SetActive(hasAbilities && hasOther);
    }

    private void RefreshSelection()
    {
        var catItems = GetCategoryItems(_currentCategory);

        if (catItems.Count == 0)
        {
            ClearDetailPanel();
            foreach (var img in _itemImages)
                if (img != null) img.color = itemNormalColor;
            return;
        }

        if (_categoryIndex >= catItems.Count)
            _categoryIndex = catItems.Count - 1;

        _selectedIndex = GetGlobalIndex(_currentCategory, _categoryIndex);

        for (int i = 0; i < _itemImages.Count; i++)
        {
            if (_itemImages[i] != null)
                _itemImages[i].color = i == _selectedIndex ? itemSelectedColor : itemNormalColor;
        }

        ItemEntry sel = catItems[_categoryIndex];
        if (detailIcon != null)
        {
            detailIcon.sprite  = sel.icon;
            detailIcon.enabled = sel.icon != null;
        }
        if (detailName        != null) detailName.text        = sel.isAbility ? sel.displayName : "Coins";
        if (detailDescription != null) detailDescription.text = sel.description;
    }

    private void ClearDetailPanel()
    {
        if (detailIcon        != null) { detailIcon.sprite = null; detailIcon.enabled = false; }
        if (detailName        != null) detailName.text        = "";
        if (detailDescription != null) detailDescription.text = "";
    }

    private void ClearCharmDetailPanel()
    {
        if (charmDetailIcon        != null) { charmDetailIcon.sprite = null; charmDetailIcon.enabled = false; }
        if (charmDetailName        != null) charmDetailName.text        = "";
        if (charmDetailDescription != null) charmDetailDescription.text = "";
    }

    // ── Category helpers ──────────────────────────────────────────────────────

    private List<ItemEntry> GetCategoryItems(int category)
    {
        var result = new List<ItemEntry>();
        foreach (var item in _items)
        {
            if (category == 0 &&  item.isAbility) result.Add(item);
            if (category == 1 && !item.isAbility) result.Add(item);
        }
        return result;
    }

    // Maps category+local index → index in _items / _itemImages
    private int GetGlobalIndex(int category, int catIndex)
    {
        int abilityCount = GetCategoryItems(0).Count;
        return category == 0 ? catIndex : abilityCount + catIndex;
    }

    private void NavigateCategory(int direction)
    {
        int next = _currentCategory + direction;
        while (next >= 0 && next <= 1)
        {
            if (GetCategoryItems(next).Count > 0)
            {
                _currentCategory = next;
                _categoryIndex   = 0;
                RefreshSelection();
                return;
            }
            next += direction;
        }
    }

    private void UpdateArrowHighlight()
    {
        if (charmPreviewButtonImage != null)
            charmPreviewButtonImage.color = _arrowFocused ? itemSelectedColor : itemNormalColor;

        if (_arrowFocused)
        {
            foreach (var img in _itemImages)
                if (img != null) img.color = itemNormalColor;
            ClearDetailPanel();
        }
    }

    // ── Charm preview ─────────────────────────────────────────────────────────

    private void OpenCharmPreview()
    {
        if (charmPreviewPanel == null) return;
        OnInventoryToggled?.Invoke(true);

        inventoryPanel.SetActive(false);
        charmPreviewPanel.SetActive(true);
        _charmPreviewOpen   = true;
        _charmSelectedIndex = 0;
        _charmBackFocused   = false;
        RebuildCharmGrid();
        RefreshCharmSelection();
        RefreshEquippedSlots();
    }

    private void CloseCharmPreview()
    {
        _charmBackFocused = false;
        UpdateCharmBackHighlight();
        _arrowFocused    = false;
        _currentCategory = 0;
        _categoryIndex   = 0;
        UpdateArrowHighlight();
        if (charmPreviewPanel != null) charmPreviewPanel.SetActive(false);
        inventoryPanel.SetActive(true);
        _charmPreviewOpen = false;
        RefreshAll();
    }

    private void RebuildCharmGrid()
    {
        foreach (Transform child in charmGrid) Destroy(child.gameObject);
        _charmImages.Clear();

        if (CharmInventory.Instance == null) return;

        foreach (var charm in CharmInventory.Instance.CollectedCharms)
        {
            GameObject btn = Instantiate(charmButtonPrefab, charmGrid);
            if (btn.TryGetComponent(out Image icon))
            {
                icon.sprite = charm.icon;
                _charmImages.Add(icon);
            }

            var label = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.gameObject.SetActive(false);
        }
    }

    private void RefreshCharmSelection()
    {
        if (CharmInventory.Instance == null) { ClearCharmDetailPanel(); return; }
        var charms = CharmInventory.Instance.CollectedCharms;

        if (charms == null || charms.Count == 0)
        {
            if (charmDetailIcon        != null) { charmDetailIcon.sprite = null; charmDetailIcon.enabled = false; }
            if (charmDetailName        != null) charmDetailName.text        = "";
            if (charmDetailDescription != null) charmDetailDescription.text = "";
            return;
        }

        if (_charmSelectedIndex >= charms.Count)
            _charmSelectedIndex = charms.Count - 1;

        for (int i = 0; i < _charmImages.Count; i++)
        {
            if (_charmImages[i] != null)
                _charmImages[i].color = i == _charmSelectedIndex ? itemSelectedColor : itemNormalColor;
        }

        var sel = charms[_charmSelectedIndex];
        if (charmDetailIcon        != null) { charmDetailIcon.sprite = sel.icon; charmDetailIcon.enabled = sel.icon != null; }
        if (charmDetailName        != null) charmDetailName.text        = sel.charmName;
        if (charmDetailDescription != null) charmDetailDescription.text = sel.description;

        RefreshEquippedSlots();
    }

    private void RefreshEquippedSlots()
    {
        if (CharmInventory.Instance == null) return;
        CharmData[] equipped = CharmInventory.Instance.EquippedCharms;
        Image[] slots = { slot1Image, slot2Image, slot3Image };
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null) continue;
            CharmData charm = i < equipped.Length ? equipped[i] : null;
            slots[i].sprite = charm?.icon;
            slots[i].color  = charm != null
                ? new Color(1f, 1f, 0.5f, 0.8f)
                : new Color(1f, 1f, 1f, 0.2f);
        }
    }

    private void HandleCharmPreviewInput()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CloseCharmPreview();
            return;
        }

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            if (_charmBackFocused) { CloseCharmPreview(); return; }
            ShowBenchMessage();
            return;
        }

        if (CharmInventory.Instance == null) return;
        var charms = CharmInventory.Instance.CollectedCharms;

        if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
        {
            if (_charmBackFocused) return;

            if (charms.Count == 0 || _charmSelectedIndex == 0)
            {
                _charmBackFocused = true;
                UpdateCharmBackHighlight();
            }
            else
            {
                _charmSelectedIndex--;
                RefreshCharmSelection();
            }
        }
        else if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
        {
            if (_charmBackFocused)
            {
                _charmBackFocused = false;
                UpdateCharmBackHighlight();
                _charmSelectedIndex = 0;
                RefreshCharmSelection();
            }
            else if (_charmSelectedIndex < charms.Count - 1)
            {
                _charmSelectedIndex++;
                RefreshCharmSelection();
            }
        }
    }

    private void UpdateCharmBackHighlight()
    {
        if (charmPreviewBackButton != null)
            charmPreviewBackButton.image.color = _charmBackFocused ? itemSelectedColor : itemNormalColor;

        if (_charmBackFocused)
        {
            foreach (var img in _charmImages)
                if (img != null) img.color = itemNormalColor;
            ClearCharmDetailPanel();
        }
    }

    private void ShowBenchMessage()
    {
        if (benchMessage == null) return;
        if (_benchCoroutine != null) StopCoroutine(_benchCoroutine);
        _benchCoroutine = StartCoroutine(BenchMessageRoutine());
    }

    private IEnumerator BenchMessageRoutine()
    {
        benchMessage.gameObject.SetActive(true);
        yield return _benchWait;
        benchMessage.gameObject.SetActive(false);
        _benchCoroutine = null;
    }
}
