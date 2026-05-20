using UnityEngine;

public class MushroomAttackHitbox : MonoBehaviour
{
    [SerializeField] private Collider2D _collider;

    private void Awake()
    {
        _collider.enabled = false;
    }

    public void SetActive(bool active)
    {
        _collider.enabled = active;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerHealth>() != null)
            PlayerHealth.Instance.TakeDamage();
    }
}
