using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CircleCollider2D))]
public class CoinPickup : MonoBehaviour
{
    [SerializeField] private string pickupID = "";

    private bool _playerInRange;
    private bool _collected;
    private PlayerInput _playerInput;

    private const string PlayerTag = "Player";

    private void Start()
    {
        GetComponent<Collider2D>().isTrigger = true;

        if (GameManager.Instance != null && GameManager.Instance.IsPickupCollected(pickupID))
        {
            gameObject.SetActive(false);
        }
        else
        {
            CoinManager.Instance?.RegisterCoin();
        }

        _playerInput = PlayerMovement.Instance?.GetComponent<PlayerInput>();
    }

    private void Update()
    {
        if (_collected || !_playerInRange)
            return;

        if (_playerInput != null && _playerInput.actions["Interact"].WasPressedThisFrame())
            Collect();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(PlayerTag)) _playerInRange = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(PlayerTag)) _playerInRange = false;
    }

    private void Collect()
    {
        _collected = true;
        GameManager.Instance?.AddCoin();
        GameManager.Instance?.RegisterCollectedPickup(pickupID);

        if (CoinManager.Instance != null && GameManager.Instance != null &&
            GameManager.Instance.Coins >= CoinManager.Instance.TotalCoins)
            AchievementManager.Instance?.UnlockAchievement("lucky");

        StartCoroutine(CollectAnimation());
    }

    private IEnumerator CollectAnimation()
    {
        Vector3 originalScale = transform.localScale;
        Vector3 bigScale      = originalScale * 1.4f;
        const float half      = 0.1f;
        float elapsed         = 0f;

        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(originalScale, bigScale, elapsed / half);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(bigScale, Vector3.zero, elapsed / half);
            yield return null;
        }

        Destroy(gameObject);
    }
}
