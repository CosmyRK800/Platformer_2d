using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class WishingWell : MonoBehaviour
{
    [SerializeField] private GameObject rewardCharmPrefab;
    [SerializeField] private Transform rewardSpawnPoint;
    [SerializeField] private GameObject wellUIPanel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI subtitleText;
    [SerializeField] private TextMeshProUGUI yesText;
    [SerializeField] private TextMeshProUGUI noText;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private Color selectedColor = Color.white;
    [SerializeField] private Color normalColor = Color.gray;

    public static WishingWell Instance { get; private set; }

    private bool _playerInZone;
    private bool _panelOpen;
    private bool _isUsed;
    private int _coinsThrown;
    private bool _yesSelected = true;
    private bool _inputCooldown;
    private PlayerInput _playerInput;

    public int CoinsThrown => _coinsThrown;
    public bool IsUsed => _isUsed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        _playerInput = PlayerMovement.Instance?.GetComponent<PlayerInput>();
        wellUIPanel.SetActive(false);
        UpdateProgress();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            _playerInZone = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        _playerInZone = false;
        if (_panelOpen)
            ClosePanel();
    }

    private void Update()
    {
        if (_playerInZone && !_panelOpen && !_isUsed)
        {
            if (_playerInput != null && _playerInput.actions["Interact"].WasPressedThisFrame())
                OpenPanel();
        }

        if (_panelOpen)
            HandlePanelInput();
    }

    private void OpenPanel()
    {
        wellUIPanel.SetActive(true);
        _panelOpen = true;
        _yesSelected = true;
        _inputCooldown = true;
        PlayerMovement.Instance?.SetGameplayBlocked(true);
        UpdateSelection();
        UpdateProgress();
    }

    private void ClosePanel()
    {
        wellUIPanel.SetActive(false);
        _panelOpen = false;
        PlayerMovement.Instance?.SetGameplayBlocked(false);
    }

    private void HandlePanelInput()
    {
        if (_inputCooldown) { _inputCooldown = false; return; }

        if (Keyboard.current != null &&
            (Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.rightArrowKey.wasPressedThisFrame))
        {
            _yesSelected = !_yesSelected;
            UpdateSelection();
        }

        if (_playerInput != null && _playerInput.actions["Interact"].WasPressedThisFrame())
        {
            if (_yesSelected)
                ThrowCoin();
            else
                ClosePanel();
        }
    }

    private void UpdateSelection()
    {
        if (yesText != null) yesText.color = _yesSelected ? selectedColor : normalColor;
        if (noText  != null) noText.color  = !_yesSelected ? selectedColor : normalColor;
    }

    private void UpdateProgress()
    {
        if (progressText == null) return;
        int total = CoinManager.Instance?.TotalCoins ?? 0;
        progressText.text = _coinsThrown + " / " + total;
    }

    private void ThrowCoin()
    {
        if (GameManager.Instance.Coins <= 0)
            return;

        GameManager.Instance.SpendCoin();
        _coinsThrown += 1;
        UpdateProgress();

        int total = CoinManager.Instance?.TotalCoins ?? 0;
        if (total > 0 && _coinsThrown >= total)
        {
            GrantReward();
            ClosePanel();
        }
    }

    private void GrantReward()
    {
        _isUsed = true;
        if (rewardCharmPrefab != null)
        {
            GameObject reward = Instantiate(rewardCharmPrefab,
                rewardSpawnPoint.position + Vector3.up * 4f,
                Quaternion.identity);
            reward.SetActive(true);
            StartCoroutine(BounceIn(reward.transform));
        }
        GameManager.Instance?.SaveGame();
    }

    private IEnumerator BounceIn(Transform t)
    {
        if (t == null) yield break;

        Vector3 startPos = rewardSpawnPoint.position + Vector3.up * 4f;
        Vector3 endPos   = rewardSpawnPoint.position + Vector3.right * 0.5f;

        t.position = startPos;

        yield return null; // let CharmPickup.Start() run before forcing active

        if (t == null) yield break;
        t.gameObject.SetActive(true); // override SetActive(false) from CharmPickup.Start()

        Rigidbody2D rb = t.GetComponent<Rigidbody2D>();
        if (rb != null) rb.gravityScale = 0f;

        const float duration = 0.6f;
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
        {
            if (t == null) yield break;
            float s = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            t.position = Vector3.Lerp(startPos, endPos, s);
            yield return null;
        }

        if (t == null) yield break;
        t.position = endPos;

        if (rb != null) rb.gravityScale = 1f;

        // Squish landing
        Vector3 squished = new Vector3(1.3f, 0.7f, 1f);
        Vector3 normal   = Vector3.one;
        const float half = 0.08f;

        for (float e = 0f; e < half; e += Time.deltaTime)
        {
            if (t == null) yield break;
            t.localScale = Vector3.Lerp(normal, squished, e / half);
            yield return null;
        }

        for (float e = 0f; e < half; e += Time.deltaTime)
        {
            if (t == null) yield break;
            t.localScale = Vector3.Lerp(squished, normal, e / half);
            yield return null;
        }

        if (t != null) t.localScale = normal;
    }

    public void LoadState(bool used, int thrown)
    {
        if (used) _isUsed = true;
        _coinsThrown = thrown;
    }
}
