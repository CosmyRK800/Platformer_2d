using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class StalactiteTriggerZone : MonoBehaviour
{
    private Stalactite _stalactite;

    private const string PlayerTag = "Player";

    private void Awake()
    {
        _stalactite = GetComponentInParent<Stalactite>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(PlayerTag))
            _stalactite?.OnPlayerEnterZone();
    }
}
