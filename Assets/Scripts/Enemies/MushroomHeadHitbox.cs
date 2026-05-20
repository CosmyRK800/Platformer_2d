using UnityEngine;

public class MushroomHeadHitbox : MonoBehaviour
{
    [SerializeField] private MushroomEnemy _mushroom;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("DownAttack"))
            _mushroom.TriggerStun();
    }
}
