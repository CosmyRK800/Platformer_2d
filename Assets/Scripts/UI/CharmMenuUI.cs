// // using UnityEngine;
// // using UnityEngine.UI;
// // using TMPro;

// // /// <summary>
// // /// Charm equip/unequip menu. Opens only when the player is sitting at a bench.
// // /// Attach to a Canvas GameObject in the UI hierarchy.
// // /// </summary>
// // public class CharmMenuUI : MonoBehaviour
// // {
// //     [Header("Panels")]
// //     public GameObject menuPanel;

// //     [Header("Equipped Slots")]
// //     [Tooltip("Three slot buttons for equipped charms.")]
// //     public Button[] equippedSlotButtons;
// //     public Image[] equippedSlotIcons;
// //     public TextMeshProUGUI[] equippedSlotNames;

// //     [Header("Inventory")]
// //     [Tooltip("Parent transform where collected charm buttons are spawned.")]
// //     public Transform inventoryContainer;
// //     public GameObject charmButtonPrefab;

// //     private bool _isOpen;

// //     private void Awake()
// //     {
// //         menuPanel.SetActive(false);
// //     }

// //     private void OnEnable()
// //     {
// //         Checkpoint.OnCharmMenuRequested += ToggleMenu;
// //     }

// //     private void OnDisable()
// //     {
// //         Checkpoint.OnCharmMenuRequested -= ToggleMenu;
// //     }

// //     private void ToggleMenu()
// //     {
// //         if (_isOpen)
// //             CloseMenu();
// //         else
// //             OpenMenu();
// //     }

// //     private void OpenMenu()
// //     {
// //         _isOpen = true;
// //         menuPanel.SetActive(true);
// //         RefreshUI();
// //     }

// //     private void CloseMenu()
// //     {
// //         _isOpen = false;
// //         menuPanel.SetActive(false);
// //     }

// //     private void RefreshUI()
// //     {
// //         RefreshEquippedSlots();
// //         RefreshInventory();
// //     }

// //     private void RefreshEquippedSlots()
// //     {
// //         for (int i = 0; i < equippedSlotButtons.Length; i++)
// //         {
// //             int slotIndex = i;
// //             CharmData equipped = CharmInventory.Instance.EquippedCharms[slotIndex];

// //             equippedSlotIcons[i].sprite = equipped != null ? equipped.icon : null;
// //             equippedSlotIcons[i].enabled = equipped != null;
// //             equippedSlotNames[i].text = equipped != null ? equipped.charmName : "Empty";

// //             equippedSlotButtons[i].onClick.RemoveAllListeners();
// //             if (equipped != null)
// //             {
// //                 equippedSlotButtons[i].onClick.AddListener(() =>
// //                 {
// //                     CharmInventory.Instance.UnequipCharm(slotIndex);
// //                     RefreshUI();
// //                 });
// //             }
// //         }
// //     }

// //     private void RefreshInventory()
// //     {
// //         // Clear old buttons.
// //         foreach (Transform child in inventoryContainer)
// //             Destroy(child.gameObject);

// //         foreach (CharmData charm in CharmInventory.Instance.CollectedCharms)
// //         {
// //             GameObject btn = Instantiate(charmButtonPrefab, inventoryContainer);

// //             // Set icon.
// //             Image icon = btn.GetComponentInChildren<Image>();
// //             if (icon != null)
// //                 icon.sprite = charm.icon;

// //             // Set name.
// //             TextMeshProUGUI label = btn.GetComponentInChildren<TextMeshProUGUI>();
// //             if (label != null)
// //                 label.text = charm.charmName;

// //             // On click: equip in first free slot.
// //             Button button = btn.GetComponent<Button>();
// //             CharmData charmRef = charm;
// //             button.onClick.AddListener(() =>
// //             {
// //                 EquipInFirstFreeSlot(charmRef);
// //                 RefreshUI();
// //             });
// //         }
// //     }

// //     private void EquipInFirstFreeSlot(CharmData charm)
// //     {
// //         CharmData[] equipped = CharmInventory.Instance.EquippedCharms;
// //         for (int i = 0; i < equipped.Length; i++)
// //         {
// //             if (equipped[i] == null)
// //             {
// //                 CharmInventory.Instance.EquipCharm(charm, i);
// //                 return;
// //             }
// //         }

// //         // Toate sloturile pline — echipeaza in slotul 0.
// //         CharmInventory.Instance.EquipCharm(charm, 0);
// //     }
// // }
// using UnityEngine;
// using UnityEngine.UI;
// using TMPro;

// public class CharmMenuUI : MonoBehaviour
// {
//     [Header("Panels")]
//     public GameObject menuPanel;

//     [Header("Equipped Slots")]
//     public Button[] equippedSlotButtons;
//     public Image[] equippedSlotIcons;
//     public TextMeshProUGUI[] equippedSlotNames;

//     [Header("Inventory")]
//     public Transform inventoryContainer;
//     public GameObject charmButtonPrefab;

//     public bool IsOpen { get; private set; }

//     public static CharmMenuUI Instance { get; private set; }

//     private void Awake()
//     {
//         Instance = this;
//         menuPanel.SetActive(false);
//     }

//     private void OnEnable()
//     {
//         Checkpoint.OnCharmMenuRequested += ToggleMenu;
//     }

//     private void OnDisable()
//     {
//         Checkpoint.OnCharmMenuRequested -= ToggleMenu;
//     }

//     private void ToggleMenu()
//     {
//         if (IsOpen)
//             CloseMenu();
//         else
//             OpenMenu();
//     }

// private void OpenMenu()
// {
//     IsOpen = true;
//     menuPanel.SetActive(true);
//     RefreshUI();
// }

// private void CloseMenu()
// {
//     IsOpen = false;
//     menuPanel.SetActive(false);
// }

//     private void RefreshUI()
//     {
//         RefreshEquippedSlots();
//         RefreshInventory();
//     }

//     private void RefreshEquippedSlots()
//     {
//         for (int i = 0; i < equippedSlotButtons.Length; i++)
//         {
//             int slotIndex = i;
//             CharmData equipped = CharmInventory.Instance.EquippedCharms[slotIndex];

//             equippedSlotIcons[i].sprite = equipped != null ? equipped.icon : null;
//             equippedSlotIcons[i].enabled = equipped != null;
//             equippedSlotNames[i].text = equipped != null ? equipped.charmName : "Empty";

//             equippedSlotButtons[i].onClick.RemoveAllListeners();
//             if (equipped != null)
//             {
//                 equippedSlotButtons[i].onClick.AddListener(() =>
//                 {
//                     CharmInventory.Instance.UnequipCharm(slotIndex);
//                     RefreshUI();
//                 });
//             }
//         }
//     }

//     private void RefreshInventory()
//     {
//         foreach (Transform child in inventoryContainer)
//             Destroy(child.gameObject);

//         foreach (CharmData charm in CharmInventory.Instance.CollectedCharms)
//         {
//             GameObject btn = Instantiate(charmButtonPrefab, inventoryContainer);

//             Image icon = btn.GetComponentInChildren<Image>();
//             if (icon != null)
//                 icon.sprite = charm.icon;

//             TextMeshProUGUI label = btn.GetComponentInChildren<TextMeshProUGUI>();
//             if (label != null)
//                 label.text = charm.charmName;

//             Button button = btn.GetComponent<Button>();
//             CharmData charmRef = charm;
//             button.onClick.AddListener(() =>
//             {
//                 EquipInFirstFreeSlot(charmRef);
//                 RefreshUI();
//             });
//         }
//     }

//     private void EquipInFirstFreeSlot(CharmData charm)
//     {
//         CharmData[] equipped = CharmInventory.Instance.EquippedCharms;
//         for (int i = 0; i < equipped.Length; i++)
//         {
//             if (equipped[i] == null)
//             {
//                 CharmInventory.Instance.EquipCharm(charm, i);
//                 return;
//             }
//         }

//         CharmInventory.Instance.EquipCharm(charm, 0);
//     }

// private void Update()
// {
//     if (!IsOpen)
//         return;

//     if (CharmInventory.Instance.CollectedCharms.Count == 0)
//         return;

//     if (UnityEngine.InputSystem.Keyboard.current != null &&
//         UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame)
//     {
//         CharmData charm = CharmInventory.Instance.CollectedCharms[0];

//         // Verificam daca e deja echipat
//         int equippedSlot = -1;
//         for (int i = 0; i < CharmInventory.Instance.EquippedCharms.Length; i++)
//         {
//             if (CharmInventory.Instance.EquippedCharms[i] == charm)
//             {
//                 equippedSlot = i;
//                 break;
//             }
//         }

//         if (equippedSlot >= 0)
//             CharmInventory.Instance.UnequipCharm(equippedSlot);
//         else
//             EquipInFirstFreeSlot(charm);

//         RefreshUI();
//     }
// }
// }
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class CharmMenuUI : MonoBehaviour
{
    [Header("Panels")]
    public GameObject menuPanel;

    [Header("Left Panel")]
    public Image charmDetailIcon;
    public TextMeshProUGUI charmDetailName;
    public TextMeshProUGUI charmDetailDesc;

    [Header("Slots")]
    public Image[] slotImages;

    [Header("Inventory")]
    public Transform inventoryGrid;
    public GameObject charmButtonPrefab;

    [Header("Colors")]
    public Color slotEmptyColor = new Color(1f, 1f, 1f, 0.2f);
    public Color slotEquippedColor = new Color(1f, 1f, 0.5f, 0.8f);
    public Color itemNormalColor = new Color(1f, 1f, 1f, 0.8f);
    public Color itemSelectedColor = new Color(1f, 1f, 0f, 1f);

    public bool IsOpen { get; private set; }
    public static CharmMenuUI Instance { get; private set; }

    private int _selectedIndex = 0;
    private Image[] _inventoryButtons;

    private void Awake()
    {
        Instance = this;
        menuPanel.SetActive(false);
    }

    private void OnEnable()
    {
        Checkpoint.OnCharmMenuRequested += ToggleMenu;
    }

    private void OnDisable()
    {
        Checkpoint.OnCharmMenuRequested -= ToggleMenu;
    }

    private void ToggleMenu()
    {
        if (IsOpen)
            CloseMenu();
        else
            OpenMenu();
    }

    private void OpenMenu()
    {
        IsOpen = true;
        menuPanel.SetActive(true);
        _selectedIndex = 0;
        RefreshUI();
    }

    private void CloseMenu()
    {
        IsOpen = false;
        menuPanel.SetActive(false);
    }

    private void Update()
    {
        if (!IsOpen)
            return;

        if (Keyboard.current == null)
            return;

        var collected = CharmInventory.Instance.CollectedCharms;

        if (collected.Count == 0)
            return;

        // Navigare stanga/dreapta
        if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
        {
            _selectedIndex = (_selectedIndex - 1 + collected.Count) % collected.Count;
            RefreshSelection();
        }

        if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
        {
            _selectedIndex = (_selectedIndex + 1) % collected.Count;
            RefreshSelection();
        }

        // Space - echipeaza/dezechipeaza
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            CharmData selected = collected[_selectedIndex];
            int equippedSlot = FindEquippedSlot(selected);

            if (equippedSlot >= 0)
                CharmInventory.Instance.UnequipCharm(equippedSlot);
            else
                EquipInFirstFreeSlot(selected);

            RefreshUI();
        }
    }

    private void RefreshUI()
    {
        RefreshSlots();
        RefreshInventory();
        RefreshSelection();
    }

    private void RefreshSlots()
    {
        for (int i = 0; i < slotImages.Length; i++)
        {
            CharmData equipped = CharmInventory.Instance.EquippedCharms[i];
            slotImages[i].sprite = equipped?.icon;
            slotImages[i].color = equipped != null ? slotEquippedColor : slotEmptyColor;
        }
    }

    private void RefreshInventory()
    {
        // Sterge butoanele vechi
        foreach (Transform child in inventoryGrid)
            Destroy(child.gameObject);

        var collected = CharmInventory.Instance.CollectedCharms;
        _inventoryButtons = new Image[collected.Count];

        for (int i = 0; i < collected.Count; i++)
        {
            GameObject btn = Instantiate(charmButtonPrefab, inventoryGrid);

            // Setam doar iconul, fara nume
            Image icon = btn.GetComponent<Image>();
            if (icon != null)
            {
                icon.sprite = collected[i].icon;
                _inventoryButtons[i] = icon;
            }

            // Ascundem textul
            TextMeshProUGUI label = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
                label.gameObject.SetActive(false);
        }
    }

    private void RefreshSelection()
    {
        var collected = CharmInventory.Instance.CollectedCharms;

        if (collected.Count == 0)
        {
            ClearDetailPanel();
            return;
        }

        // Coloreaza butoanele din inventar
        if (_inventoryButtons != null)
        {
            for (int i = 0; i < _inventoryButtons.Length; i++)
            {
                if (_inventoryButtons[i] != null)
                    _inventoryButtons[i].color = i == _selectedIndex ? itemSelectedColor : itemNormalColor;
            }
        }

        // Actualizeaza panoul lateral
        CharmData selected = collected[_selectedIndex];
        if (charmDetailIcon != null)
            charmDetailIcon.sprite = selected.icon;
        if (charmDetailName != null)
            charmDetailName.text = selected.charmName;
        if (charmDetailDesc != null)
            charmDetailDesc.text = selected.description;
    }

    private void ClearDetailPanel()
    {
        if (charmDetailIcon != null) charmDetailIcon.sprite = null;
        if (charmDetailName != null) charmDetailName.text = "";
        if (charmDetailDesc != null) charmDetailDesc.text = "";
    }

    private int FindEquippedSlot(CharmData charm)
    {
        for (int i = 0; i < CharmInventory.Instance.EquippedCharms.Length; i++)
        {
            if (CharmInventory.Instance.EquippedCharms[i] == charm)
                return i;
        }
        return -1;
    }

    private void EquipInFirstFreeSlot(CharmData charm)
    {
        CharmData[] equipped = CharmInventory.Instance.EquippedCharms;
        for (int i = 0; i < equipped.Length; i++)
        {
            if (equipped[i] == null)
            {
                CharmInventory.Instance.EquipCharm(charm, i);
                return;
            }
        }

        // Toate pline, echipeaza in slot 0
        CharmInventory.Instance.EquipCharm(charm, 0);
    }
}