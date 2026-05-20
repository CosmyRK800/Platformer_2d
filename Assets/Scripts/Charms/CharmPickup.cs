using UnityEngine;
using UnityEngine.InputSystem;
using System;

[RequireComponent(typeof(CircleCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class CharmPickup : MonoBehaviour
{
    [Header("Save System")]
    [SerializeField] private string pickupID = "";

    [Header("Charm")]
    public CharmData charmData;

    [Header("Glow")]
    public UnityEngine.Rendering.Universal.Light2D glowLight;

    [SerializeField] private float _pulseSpeed = 2f;
    [SerializeField] private float _minIntensity = 0.8f;
    [SerializeField] private float _maxIntensity = 2.5f;

    private bool _playerInRange;
    private PlayerInput _playerInput;

    public static event Action<CharmData> OnCharmCollected;

    private void Start()
    {
        Collider2D col = GetComponent<Collider2D>();
        col.isTrigger = true;

        if (GameManager.Instance != null && GameManager.Instance.IsPickupCollected(pickupID))
            gameObject.SetActive(false);

        _playerInput = PlayerMovement.Instance?.GetComponent<PlayerInput>();
    }

    private void Update()
    {
        // Efect de pulsare a luminii
        if (glowLight != null)
        {
            float t = (Mathf.Sin(Time.time * _pulseSpeed) + 1f) / 2f;
            glowLight.intensity = Mathf.Lerp(_minIntensity, _maxIntensity, t);
        }

        if (!_playerInRange)
            return;

        if (_playerInput != null && _playerInput.actions["Interact"].WasPressedThisFrame())
            Collect();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        _playerInRange = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        _playerInRange = false;
    }

    private void Collect()
    {
        if (charmData == null)
        {
            Debug.LogWarning("CharmPickup: charmData is not assigned.");
            return;
        }

        GameManager.Instance?.RegisterCollectedPickup(pickupID);
        CharmInventory.Instance?.AddCharm(charmData);
        OnCharmCollected?.Invoke(charmData);

        Destroy(gameObject);
    }
}