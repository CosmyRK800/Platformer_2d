using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Populates the Achievements scene's scrollable list entirely in code.
/// No row prefab needed — just assign contentParent (the Content RectTransform
/// inside the ScrollRect) and the ordered achievements list.
/// </summary>
public class AchievementsScreenController : MonoBehaviour
{
    [Header("Scroll View — drag the ScrollRect's Content RectTransform here")]
    public Transform contentParent;

    [Header("Achievements (in display order)")]
    public List<AchievementData> achievements;

    // ── Layout constants ──────────────────────────────────────────────────────
    private const float RowHeight    = 100f;
    private const float RowSpacing   =   4f;
    private const float IconPadLeft  =  10f;
    private const float IconSize     =  80f;
    private const float TextLeft     = IconPadLeft + IconSize + 10f; // 100 px
    private const float TextPadRight =  10f;

    // ── Colours ───────────────────────────────────────────────────────────────
    private static readonly Color RowBgColor   = new Color(0.10f, 0.10f, 0.10f, 0.45f);
    private static readonly Color DescColor    = new Color(0.667f, 0.667f, 0.667f, 1f); // #AAAAAA
    private static readonly Color LockedColor  = new Color(0.28f, 0.28f, 0.28f, 1f);

    // -------------------------------------------------------------------------

    private void Start()
    {
        EnsureContentLayout();

        HashSet<string> unlockedIDs = LoadUnlockedIDsFromDisk();
        Debug.Log($"AchievementsScreenController: {unlockedIDs.Count} achievement(s) unlocked globally: [{string.Join(", ", unlockedIDs)}]");

        PopulateList(unlockedIDs);
    }

    /// <summary>
    /// Reads the global achievements file directly — no slot logic needed.
    /// Achievements are shared across all save slots.
    /// </summary>
    private HashSet<string> LoadUnlockedIDsFromDisk()
    {
        return new HashSet<string>(SaveSystem.LoadAchievements());
    }

    // ── Auto-configure Content if VLG / CSF are missing ──────────────────────

    private void EnsureContentLayout()
    {
        if (contentParent == null) return;
        var go = contentParent.gameObject;

        if (go.GetComponent<VerticalLayoutGroup>() == null)
        {
            var vlg = go.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment         = TextAnchor.UpperCenter;
            vlg.childControlWidth      = true;
            vlg.childControlHeight     = true;
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = RowSpacing;
            vlg.padding = new RectOffset(0, 0, 10, 10);
        }

        if (go.GetComponent<ContentSizeFitter>() == null)
        {
            var csf = go.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
    }

    // ── Row population ────────────────────────────────────────────────────────

    private void PopulateList(HashSet<string> unlockedIDs)
    {
        if (contentParent == null)
        {
            Debug.LogWarning("AchievementsScreenController: contentParent not assigned.");
            return;
        }

        foreach (AchievementData data in achievements)
        {
            if (data == null) continue;
            bool unlocked = unlockedIDs.Contains(data.achievementID);
            BuildRow(data, unlocked);
        }
    }

    // ── Programmatic row builder ──────────────────────────────────────────────

    private void BuildRow(AchievementData data, bool unlocked)
    {
        // ── Row root ──────────────────────────────────────────────────────────
        //   Image call auto-converts Transform → RectTransform.
        //   VerticalLayoutGroup on contentParent will set final width/height
        //   based on the LayoutElement values below.
        var rowGO = new GameObject("Row_" + data.achievementID);
        rowGO.transform.SetParent(contentParent, false);

        var rowBg = rowGO.AddComponent<Image>();
        rowBg.color = RowBgColor;

        // Tell the parent VLG how tall this row is.
        var rowLE = rowGO.AddComponent<LayoutElement>();
        rowLE.preferredHeight = RowHeight;
        rowLE.flexibleWidth   = 1f;

        // ── Icon ──────────────────────────────────────────────────────────────
        //   Anchored to the left-center of the row; exact pixel size.
        var iconGO   = new GameObject("Icon");
        iconGO.transform.SetParent(rowGO.transform, false);

        var iconImg  = iconGO.AddComponent<Image>(); // auto-adds RectTransform
        var iconRect = iconGO.GetComponent<RectTransform>();
        iconRect.anchorMin        = new Vector2(0f, 0.5f);
        iconRect.anchorMax        = new Vector2(0f, 0.5f);
        iconRect.pivot            = new Vector2(0f, 0.5f);
        iconRect.anchoredPosition = new Vector2(IconPadLeft, 0f);
        iconRect.sizeDelta        = new Vector2(IconSize, IconSize);

        if (unlocked)
        {
            iconImg.sprite = data.icon;
            iconImg.color  = data.icon != null ? Color.white : new Color(0.6f, 0.6f, 0.6f);
        }
        else
        {
            iconImg.sprite = null;
            iconImg.color  = LockedColor;
        }

        // ── Text area ─────────────────────────────────────────────────────────
        //   Stretches to fill the whole row height; left edge starts after icon.
        //   VerticalLayoutGroup inside centers the title+desc block vertically.
        var textGO = new GameObject("TextArea");
        textGO.transform.SetParent(rowGO.transform, false);

        // VerticalLayoutGroup auto-adds RectTransform.
        var vlg = textGO.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment         = TextAnchor.MiddleLeft; // centers block vertically
        vlg.childControlWidth      = true;
        vlg.childControlHeight     = true;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 4f;

        var textRect = textGO.GetComponent<RectTransform>();
        // Full vertical stretch inside the row; offset left to clear the icon.
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.offsetMin = new Vector2(TextLeft, 0f);
        textRect.offsetMax = new Vector2(-TextPadRight, 0f);

        // ── Title ─────────────────────────────────────────────────────────────
        var titleGO  = new GameObject("Title");
        titleGO.transform.SetParent(textGO.transform, false);

        var titleTMP = titleGO.AddComponent<TextMeshProUGUI>(); // auto-adds RectTransform
        titleTMP.text      = data.displayName;
        titleTMP.fontSize  = 20f;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.color     = Color.white;
        titleTMP.overflowMode = TextOverflowModes.Ellipsis;

        var titleLE = titleGO.AddComponent<LayoutElement>();
        titleLE.preferredHeight = 28f;

        // ── Description ───────────────────────────────────────────────────────
        var descGO  = new GameObject("Desc");
        descGO.transform.SetParent(textGO.transform, false);

        var descTMP = descGO.AddComponent<TextMeshProUGUI>(); // auto-adds RectTransform
        descTMP.text         = data.description;
        descTMP.fontSize     = 14f;
        descTMP.color        = DescColor;
        descTMP.overflowMode = TextOverflowModes.Ellipsis;

        var descLE = descGO.AddComponent<LayoutElement>();
        descLE.preferredHeight = 22f;
    }

    // ── Navigation ────────────────────────────────────────────────────────────

    public void GoBack()
    {
        SceneManager.LoadScene(0);
    }
}
