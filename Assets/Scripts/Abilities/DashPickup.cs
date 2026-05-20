using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CircleCollider2D))]
public class DashPickup : MonoBehaviour
{
    [SerializeField] private string pickupID = "";

    private bool _playerInRange;
    private PlayerInput _playerInput;

    private void Start()
    {
        GetComponent<Collider2D>().isTrigger = true;

        if (GameManager.Instance != null && GameManager.Instance.IsPickupCollected(pickupID))
            gameObject.SetActive(false);

        _playerInput = PlayerMovement.Instance?.GetComponent<PlayerInput>();
    }

    private void Update()
    {
        if (!_playerInRange) return;
        if (_playerInput != null && _playerInput.actions["Interact"].WasPressedThisFrame())
            Collect();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) _playerInRange = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player")) _playerInRange = false;
    }

    private void Collect()
    {
        GameManager.Instance?.RegisterCollectedPickup(pickupID);
        AbilityRegistry.Instance?.Unlock("Dash");
        Destroy(gameObject);
    }
}
