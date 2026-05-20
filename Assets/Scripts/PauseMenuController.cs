using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Controls the pause menu: toggling visibility, keyboard navigation (W/S), and confirm (Space).
/// Assign all references in the Inspector after building the UI hierarchy.
/// </summary>
public class PauseMenuController : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Root panel of the pause menu (child of the canvas overlay).")]
    public GameObject pausePanel;

    [Tooltip("Left arrow images, one per menu item, in the same order as menuItems.")]
    public Image[] leftArrows;

    [Tooltip("Right arrow images, one per menu item, in the same order as menuItems.")]
    public Image[] rightArrows;

    [Header("Player References")]
    [Tooltip("The PlayerMovement component on the Player GameObject.")]
    public PlayerMovement playerMovement;

    [Header("Quit to Menu Confirmation")]
    [SerializeField] private GameObject confirmPanel;
    [SerializeField] private Image[] confirmLeftArrows;   // index 0 = Yes, 1 = No
    [SerializeField] private Image[] confirmRightArrows;  // index 0 = Yes, 1 = No

    // Menu item indices
    private const int IndexResume = 0;
    private const int IndexOptions = 1;
    private const int IndexQuit = 2;
    private const int MenuItemCount = 3;

    private bool isPaused;
    private int selectedIndex;

    // Input cooldown so held keys don't scroll instantly
    private const float NavigationCooldown = 0.2f;
    private float navigationTimer;

    // Confirm popup navigation
    private const int ConfirmYes       = 0;
    private const int ConfirmNo        = 1;
    private const int ConfirmItemCount = 2;
    private int _confirmSelectedIndex  = ConfirmNo;

    private void Start()
    {
        if (confirmPanel != null) confirmPanel.SetActive(false);
        RefreshConfirmArrows();
    }

    private void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (InventoryUI.Instance != null && InventoryUI.Instance.IsOpen) return;

            if (confirmPanel != null && confirmPanel.activeSelf)
                OnConfirmNo();  // Escape dismisses the confirmation, not the whole pause menu
            else
                TogglePause();
        }

        if (!isPaused)
            return;

        if (confirmPanel != null && confirmPanel.activeSelf)
        {
            HandleConfirmNavigation();
            return;
        }

        navigationTimer -= Time.unscaledDeltaTime;

        bool moveUp = Keyboard.current.wKey.wasPressedThisFrame ||
                      (Keyboard.current.wKey.isPressed && navigationTimer <= 0f);

        bool moveDown = Keyboard.current.sKey.wasPressedThisFrame ||
                        (Keyboard.current.sKey.isPressed && navigationTimer <= 0f);

        if (moveUp)
        {
            Navigate(-1);
            navigationTimer = NavigationCooldown;
        }
        else if (moveDown)
        {
            Navigate(1);
            navigationTimer = NavigationCooldown;
        }

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
            ConfirmSelection();
    }

    /// <summary>Toggles the pause state on/off.</summary>
    public void TogglePause()
    {
        if (isPaused)
            Resume();
        else
            Pause();
    }

    private void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;
        selectedIndex = 0;
        RefreshArrows();
        pausePanel.SetActive(true);
        if (confirmPanel != null) confirmPanel.SetActive(false);

        if (playerMovement != null)
            playerMovement.SetGameplayBlocked(true);
    }

    private void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;
        pausePanel.SetActive(false);
        if (confirmPanel != null) confirmPanel.SetActive(false);

        if (playerMovement != null)
            playerMovement.SetGameplayBlocked(false);
    }

    private void Navigate(int direction)
    {
        selectedIndex = (selectedIndex + direction + MenuItemCount) % MenuItemCount;
        RefreshArrows();
    }

    private void ConfirmSelection()
    {
        switch (selectedIndex)
        {
            case IndexResume:
                Resume();
                break;
            case IndexOptions:
                // Options not implemented yet
                break;
            case IndexQuit:
                OnQuitToMenuClicked();
                break;
        }
    }

    /// <summary>Called by the Quit to Menu button; shows the confirmation popup.</summary>
    public void OnQuitToMenuClicked()
    {
        if (confirmPanel != null)
        {
            _confirmSelectedIndex = ConfirmNo;
            RefreshConfirmArrows();
            pausePanel.SetActive(false);
            confirmPanel.SetActive(true);
        }
    }

    private void OnConfirmYes()
    {
        Time.timeScale = 1f;
        GameManager.Instance?.SaveGame();
        SceneManager.LoadScene(0);
    }

    private void OnConfirmNo()
    {
        if (confirmPanel != null)
        {
            pausePanel.SetActive(true);
            confirmPanel.SetActive(false);
        }
    }

    /// <summary>Shows arrows only next to the currently selected menu item.</summary>
    private void RefreshArrows()
    {
        for (int i = 0; i < MenuItemCount; i++)
        {
            bool selected = i == selectedIndex;

            if (i < leftArrows.Length && leftArrows[i] != null)
                leftArrows[i].enabled = selected;

            if (i < rightArrows.Length && rightArrows[i] != null)
                rightArrows[i].enabled = selected;
        }
    }

    private void HandleConfirmNavigation()
    {
        if (Keyboard.current.wKey.wasPressedThisFrame || Keyboard.current.sKey.wasPressedThisFrame)
        {
            _confirmSelectedIndex = _confirmSelectedIndex == ConfirmYes ? ConfirmNo : ConfirmYes;
            RefreshConfirmArrows();
        }

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            if (_confirmSelectedIndex == ConfirmYes)
                OnConfirmYes();
            else
                OnConfirmNo();
        }
    }

    private void RefreshConfirmArrows()
    {
        for (int i = 0; i < ConfirmItemCount; i++)
        {
            bool selected = i == _confirmSelectedIndex;

            if (confirmLeftArrows != null && i < confirmLeftArrows.Length && confirmLeftArrows[i] != null)
                confirmLeftArrows[i].enabled = selected;

            if (confirmRightArrows != null && i < confirmRightArrows.Length && confirmRightArrows[i] != null)
                confirmRightArrows[i].enabled = selected;
        }
    }
}
