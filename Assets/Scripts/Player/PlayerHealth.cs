using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerHealth : MonoBehaviour
{
    public static PlayerHealth Instance { get; private set; }

    [Header("Health")]
    public int maxLives = 3;
    public float invincibilityDuration = 1.2f;
    [SerializeField] private float flickerInterval = 0.05f;

    [Header("Healing")]
    [SerializeField] private float healDuration    = 1f;
    [SerializeField] private float healChannelTime = 1f;

    [Header("Collider")]
    [SerializeField] private Collider2D _playerCollider;

    [Header("Respawn")]
    [Tooltip("PlayerMovement on the Player — used to read the last grounded position.")]
    public PlayerMovement playerMovement;
    [Tooltip("Fallback checkpoint when all lives are lost. Assign the Start_Checkpoint GameObject.")]
    public Transform startCheckpoint;

    private int _currentLives;
    private int _baseLives;
    private bool _isInvincible;
    private bool _isHealing;
    private Coroutine _healCoroutine;
    private readonly List<Collider2D> _ignoredColliders = new List<Collider2D>();
    private SpriteRenderer _spriteRenderer;
    private PlayerInput _playerInput;

    public event Action<int> OnLivesChanged;
    public event Action<int> OnMaxLivesChanged;
    public event Action OnRespawn;

    public static event Func<bool> OnDamageCheck;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        _baseLives    = maxLives;
        _currentLives = maxLives;
    }

    private void Start()
    {
        _playerInput = PlayerMovement.Instance?.GetComponent<PlayerInput>();
    }

    private void Update()
    {
        if (_playerInput == null || !_playerInput.actions["Heal"].WasPressedThisFrame()) return;
        if (_isHealing || _isInvincible) return;
        if (PlayerMovement.Instance == null || !PlayerMovement.Instance.IsGrounded) return;
        if (SoulManager.Instance == null || !SoulManager.Instance.CanHeal()) return;
        if (_currentLives >= maxLives) return;

        _healCoroutine = StartCoroutine(HealRoutine());
    }

    /// <summary>
    /// Deals one point of damage. Respawns the player to the last grounded position.
    /// When all lives are lost the player is sent to the start checkpoint with full lives restored.
    /// </summary>
    public void TakeDamage()
    {
        if (_isInvincible)
            return;

        if (OnDamageCheck != null)
            foreach (var inv in OnDamageCheck.GetInvocationList())
                if ((bool)inv.DynamicInvoke())
                    return;

        if (_isHealing && _healCoroutine != null)
        {
            StopCoroutine(_healCoroutine);
            _healCoroutine = null;
            _isHealing = false;
            PlayerMovement.Instance?.SetGameplayBlocked(false);
        }

        _currentLives = Mathf.Max(0, _currentLives - 1);
        OnLivesChanged?.Invoke(_currentLives);

        if (_currentLives <= 0)
        {
            Die();
        }
        else
        {
            RespawnToLastGroundedPosition();
            StartCoroutine(InvincibilityRoutine());
        }
    }

    /// <summary>Returns the current number of lives.</summary>
    public int GetCurrentLives() => _currentLives;

    /// <summary>
    /// Resets maxLives and currentLives to the serialized Inspector value,
    /// undoing any charm bonuses left over from a previous session.
    /// Call this before LoadGame() when re-entering the game scene.
    /// </summary>
    public void ForceReset()
    {
        maxLives      = _baseLives;
        _currentLives = _baseLives;
        OnMaxLivesChanged?.Invoke(maxLives);
        OnLivesChanged?.Invoke(_currentLives);
    }

    /// <summary>
    /// Adds amount to both maxLives and currentLives.
    /// Used by charm effects to grant extra lives.
    /// </summary>
public void AddMaxLives(int amount)
{
    maxLives += amount;
    _currentLives = Mathf.Min(_currentLives + amount, maxLives);
    OnMaxLivesChanged?.Invoke(maxLives);
    OnLivesChanged?.Invoke(_currentLives);
}

    public void SetInvincible(bool state)
    {
        _isInvincible = state;
    }

    public void StartInvincibilityIgnore(Collider2D enemyCol)
    {
        if (_playerCollider == null || enemyCol == null) return;
        if (_ignoredColliders.Contains(enemyCol)) return;
        Physics2D.IgnoreCollision(_playerCollider, enemyCol, true);
        _ignoredColliders.Add(enemyCol);
    }

    private IEnumerator HealRoutine()
    {
        _isHealing = true;
        PlayerMovement.Instance?.SetGameplayBlocked(true);
        SpawnHealParticles();

        yield return new WaitForSeconds(healChannelTime);

        SoulManager.Instance?.ConsumeSoul();
        RestoreHealth(_currentLives + 1);

        _isHealing = false;
        PlayerMovement.Instance?.SetGameplayBlocked(false);
        _healCoroutine = null;
    }

    private void SpawnHealParticles()
    {
        var go = new GameObject("Heal_Particles");
        go.transform.position = transform.position;

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration        = healDuration;
        main.loop            = false;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(healChannelTime * 0.6f, healChannelTime);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(0.4f, 1.2f);
        main.startSize       = new ParticleSystem.MinMaxCurve(0.04f, 0.12f);
        main.startColor      = new ParticleSystem.MinMaxGradient(new Color(0.4f, 1f, 0.5f), Color.white);
        main.gravityModifier = -0.3f;
        main.maxParticles    = 50;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.enabled      = true;
        emission.rateOverTime = 30f;

        var shape = ps.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius    = 0.45f;

        ps.Play();
        Destroy(go, healChannelTime + 1f);
    }

    private IEnumerator InvincibilityRoutine()
    {
        if (_spriteRenderer == null)
            _spriteRenderer = playerMovement?.GetComponent<SpriteRenderer>()
                           ?? GetComponentInChildren<SpriteRenderer>();

        _isInvincible = true;

        float elapsed = 0f;
        while (elapsed < invincibilityDuration)
        {
            if (_spriteRenderer != null)
                _spriteRenderer.enabled = !_spriteRenderer.enabled;
            yield return new WaitForSeconds(flickerInterval);
            elapsed += flickerInterval;
        }

        if (_spriteRenderer != null)
            _spriteRenderer.enabled = true;

        foreach (var col in _ignoredColliders)
            if (col != null && _playerCollider != null)
                Physics2D.IgnoreCollision(_playerCollider, col, false);
        _ignoredColliders.Clear();
        _isInvincible = false;
    }

    /// <summary>Restores the player to full health without triggering a respawn (used by bench checkpoints).</summary>
    public void RestoreFullHealth()
    {
        _currentLives = maxLives;
        OnLivesChanged?.Invoke(_currentLives);
    }

    /// <summary>Repositions the player to their last grounded position and starts invincibility frames — no life lost.</summary>
    public void TriggerDodgeRespawn()
    {
        RespawnToLastGroundedPosition();
        StartCoroutine(InvincibilityRoutine());
    }

    /// <summary>Sets current health to the given value, clamped to [0, maxLives]. Used by the save/load system.</summary>
    public void RestoreHealth(int amount)
    {
        _currentLives = Mathf.Clamp(amount, 0, maxLives);
        OnLivesChanged?.Invoke(_currentLives);
    }

    private void Die()
    {
        _currentLives = maxLives;
        OnLivesChanged?.Invoke(_currentLives);
        CurrencyManager.Instance?.LoseSouls();
        RespawnToCheckpoint();
        OnRespawn?.Invoke();
    }

    private void RespawnToLastGroundedPosition()
    {
        if (playerMovement != null)
            transform.position = playerMovement.LastGroundedPosition;
        else
            Debug.LogWarning("PlayerHealth: playerMovement is not assigned.");
    }

    private void RespawnToCheckpoint()
    {
        if (CheckpointManager.Instance != null && CheckpointManager.Instance.HasCheckpoint)
        {
            transform.position = CheckpointManager.Instance.ActiveCheckpointPosition;

            string activeID = CheckpointManager.Instance.ActiveCheckpointID;
            if (CheckpointManager.Instance.IsBenchCheckpoint(activeID) &&
                PlayerMovement.Instance != null)
            {
                Vector3 sitPos = CheckpointManager.Instance.GetBenchSitPosition(activeID);
                PlayerMovement.Instance.SetSitting(true, sitPos);
            }

            StalactiteManager.RespawnAll();
            return;
        }

        if (startCheckpoint != null)
            transform.position = startCheckpoint.position;
        else
            Debug.LogWarning("PlayerHealth: no active checkpoint and startCheckpoint is not assigned.");
    }
}