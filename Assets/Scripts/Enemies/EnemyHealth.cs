using UnityEngine;
using System;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 5;
    [SerializeField] private float deathDelay = 0f;
    private int _currentHealth;

    public event Action<int, int> OnHealthChanged;
    public event Action OnDeath;

    private void Awake()
    {
        _currentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        _currentHealth = Mathf.Max(0, _currentHealth - amount);
        OnHealthChanged?.Invoke(_currentHealth, maxHealth);

        if (_currentHealth <= 0)
            Die();
    }

    private void Die()
    {
        OnDeath?.Invoke();
        Destroy(gameObject, deathDelay);
    }

    public int GetCurrentHealth() => _currentHealth;
}