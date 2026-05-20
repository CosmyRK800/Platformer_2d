using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Attach to a controller GameObject in the SaveSlotScreen scene (index 1).
/// Fill the three SlotCardUI entries and the Back button in the Inspector.
/// </summary>
public class SaveSlotScreenController : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Inspector-wirable per-card struct
    // -------------------------------------------------------------------------

    [Serializable]
    public struct SlotCardUI
    {
        [Tooltip("TMP label that shows 'Slot N'.")]
        public TextMeshProUGUI slotLabel;

        [Tooltip("TMP label that shows checkpoint name + play time, or 'Empty'.")]
        public TextMeshProUGUI infoLabel;

        [Tooltip("Button that starts the game in this slot.")]
        public Button playButton;

        [Tooltip("Button that deletes this slot's data. Hidden when slot is empty.")]
        public Button deleteButton;
    }

    [Header("Slot Cards (0 = Slot 1, 1 = Slot 2, 2 = Slot 3)")]
    [SerializeField] private SlotCardUI[] slots = new SlotCardUI[3];

    [Header("Navigation")]
    [SerializeField] private Button backButton;

    // -------------------------------------------------------------------------
    // Unity messages
    // -------------------------------------------------------------------------

    private void Start()
    {
        backButton.onClick.AddListener(() => SceneManager.LoadScene(0));
        RefreshAll();
    }

    // -------------------------------------------------------------------------
    // UI refresh
    // -------------------------------------------------------------------------

    private void RefreshAll()
    {
        for (int i = 0; i < slots.Length; i++)
            RefreshSlot(i);
    }

    private void RefreshSlot(int index)
    {
        SlotCardUI card = slots[index];

        // Always remove previous listeners so double-refresh doesn't double-register.
        card.playButton.onClick.RemoveAllListeners();
        card.deleteButton.onClick.RemoveAllListeners();

        card.slotLabel.text = $"Slot {index + 1}";

        SlotPreview preview = GameManager.Instance != null
            ? GameManager.Instance.GetSlotPreview(index)
            : new SlotPreview { HasData = false };

        int captured = index; // capture for lambda

        if (preview.HasData)
        {
            int total   = Mathf.FloorToInt(preview.PlayTimeSeconds);
            int hours   = total / 3600;
            int minutes = total % 3600 / 60;
            int seconds = total % 60;

            card.infoLabel.text = $"{preview.LastCheckpointName}\n{hours:00}:{minutes:00}:{seconds:00}";

            card.playButton.onClick.AddListener(() => OnPlay(captured));
            card.deleteButton.onClick.AddListener(() => OnDelete(captured));
            card.deleteButton.gameObject.SetActive(true);
        }
        else
        {
            card.infoLabel.text = "Empty";

            card.playButton.onClick.AddListener(() => OnPlay(captured));
            card.deleteButton.gameObject.SetActive(false);
        }
    }

    // -------------------------------------------------------------------------
    // Button handlers
    // -------------------------------------------------------------------------

    private void OnPlay(int slotIndex)
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("SaveSlotScreenController: GameManager not found. " +
                             "Make sure a GameManager GameObject exists in the MainMenu scene.");
            return;
        }

        GameManager.Instance.SetActiveSlot(slotIndex);
        SceneManager.LoadScene(2);
    }

    private void OnDelete(int slotIndex)
    {
        if (GameManager.Instance == null)
            return;

        GameManager.Instance.DeleteSlot(slotIndex);
        RefreshAll();
    }
}
