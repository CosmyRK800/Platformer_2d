using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Singleton that tracks unlocked achievements across all scenes.
/// Builds its own DontDestroyOnLoad popup canvas so notifications work from any scene.
/// Place on a GameObject in the MainMenu scene alongside GameManager.
/// </summary>
[DefaultExecutionOrder(110)]
public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance { get; private set; }

    [Header("Achievement Definitions")]
    public List<AchievementData> allAchievements = new();

    private readonly HashSet<string> _unlockedIDs = new();

    private GameObject _popupPanel;
    private Image      _popupIcon;
    private TextMeshProUGUI _popupName;
    private Coroutine  _hideRoutine;

    // -------------------------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Achievements are global — load from the dedicated achievements.json immediately.
        foreach (string id in SaveSystem.LoadAchievements())
            _unlockedIDs.Add(id);

        BuildPopupUI();
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Unlocks the achievement with the given ID and shows the popup notification.
    /// Safe to call even if already unlocked — duplicate calls are ignored.
    /// </summary>
    public void UnlockAchievement(string id)
    {
        if (string.IsNullOrEmpty(id) || _unlockedIDs.Contains(id))
            return;

        _unlockedIDs.Add(id);
        SaveSystem.SaveAchievements(GetUnlockedIDs());

        AchievementData data = allAchievements.Find(a => a.achievementID == id);
        if (data != null)
            ShowPopup(data);
    }

    public bool IsUnlocked(string id) => _unlockedIDs.Contains(id);

    public List<string> GetUnlockedIDs() => new List<string>(_unlockedIDs);

    /// <summary>
    /// Called by GameManager.LoadGame() to restore saved unlock state.
    /// Clears current state first so loading a different slot never bleeds
    /// achievements from the previous slot into the new one.
    /// </summary>
    public void RestoreUnlocked(List<string> ids)
    {
        _unlockedIDs.Clear();
        if (ids == null) return;
        foreach (string id in ids)
            _unlockedIDs.Add(id);
    }

    // -------------------------------------------------------------------------
    // Popup notification
    // -------------------------------------------------------------------------

    private void ShowPopup(AchievementData data)
    {
        if (_hideRoutine != null)
            StopCoroutine(_hideRoutine);

        _popupName.text   = data.displayName;
        _popupIcon.sprite = data.icon;
        _popupIcon.color  = data.icon != null ? Color.white : new Color(0.45f, 0.45f, 0.45f);

        _popupPanel.SetActive(true);
        _hideRoutine = StartCoroutine(HideAfterDelay(3f));
    }

    private IEnumerator HideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        _popupPanel.SetActive(false);
        _hideRoutine = null;
    }

    // -------------------------------------------------------------------------
    // Procedural popup UI construction
    // -------------------------------------------------------------------------

    private void BuildPopupUI()
    {
        // ── Canvas ────────────────────────────────────────────────────────────
        var canvasGO = new GameObject("AchievementPopupCanvas");
        DontDestroyOnLoad(canvasGO);

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight  = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // ── Panel (bottom-center) ─────────────────────────────────────────────
        _popupPanel = new GameObject("PopupPanel");
        _popupPanel.transform.SetParent(canvasGO.transform, false);

        // Image auto-converts Transform → RectTransform
        var bg = _popupPanel.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.08f, 0.08f, 0.92f);

        var panelRect = _popupPanel.GetComponent<RectTransform>();
        panelRect.anchorMin        = new Vector2(0.5f, 0f);
        panelRect.anchorMax        = new Vector2(0.5f, 0f);
        panelRect.pivot            = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = new Vector2(0f, 30f);
        panelRect.sizeDelta        = new Vector2(420f, 90f);

        var hLayout = _popupPanel.AddComponent<HorizontalLayoutGroup>();
        hLayout.padding                = new RectOffset(12, 12, 12, 12);
        hLayout.spacing                = 12f;
        hLayout.childAlignment         = TextAnchor.MiddleLeft;
        hLayout.childControlWidth      = true;
        hLayout.childControlHeight     = true;
        hLayout.childForceExpandWidth  = false;
        hLayout.childForceExpandHeight = true;

        // ── Icon ──────────────────────────────────────────────────────────────
        var iconGO = new GameObject("Icon");
        iconGO.transform.SetParent(_popupPanel.transform, false);
        _popupIcon = iconGO.AddComponent<Image>();
        _popupIcon.color = Color.white;

        var iconLE = iconGO.AddComponent<LayoutElement>();
        iconLE.minWidth      = 66f;
        iconLE.preferredWidth = 66f;
        iconLE.flexibleWidth  = 0f;

        // ── Text container ────────────────────────────────────────────────────
        var textGO = new GameObject("TextContainer");
        textGO.transform.SetParent(_popupPanel.transform, false);

        // VerticalLayoutGroup auto-converts Transform → RectTransform
        var vLayout = textGO.AddComponent<VerticalLayoutGroup>();
        vLayout.childAlignment         = TextAnchor.MiddleLeft;
        vLayout.childControlWidth      = true;
        vLayout.childControlHeight     = false;
        vLayout.childForceExpandWidth  = true;
        vLayout.childForceExpandHeight = false;
        vLayout.spacing = 2f;

        var textLE = textGO.AddComponent<LayoutElement>();
        textLE.flexibleWidth = 1f;

        // ── "Achievement Unlocked!" header ────────────────────────────────────
        var headerGO = new GameObject("Header");
        headerGO.transform.SetParent(textGO.transform, false);
        var header = headerGO.AddComponent<TextMeshProUGUI>();
        header.text      = "Achievement Unlocked!";
        header.fontSize  = 14f;
        header.color     = new Color(1f, 0.85f, 0.2f);
        header.fontStyle = FontStyles.Bold;

        var headerLE = headerGO.AddComponent<LayoutElement>();
        headerLE.preferredHeight = 22f;

        // ── Achievement name ──────────────────────────────────────────────────
        var nameGO = new GameObject("Name");
        nameGO.transform.SetParent(textGO.transform, false);
        _popupName = nameGO.AddComponent<TextMeshProUGUI>();
        _popupName.fontSize  = 20f;
        _popupName.color     = Color.white;
        _popupName.fontStyle = FontStyles.Bold;

        var nameLE = nameGO.AddComponent<LayoutElement>();
        nameLE.preferredHeight = 30f;

        _popupPanel.SetActive(false);
    }
}
