using UnityEngine;

public class StalactiteRespawner : MonoBehaviour
{
    private Vector3   _originPosition;
    private float     _resetDelay;
    private LayerMask _groundLayer;
    private Color     _particleColor;
    private float     _shakeDelay;
    private float     _shakeIntensity;
    private Sprite    _sprite;
    private Vector2   _colliderSize;
    private Vector2   _detectionZoneSize;
    private Vector2   _detectionZoneOffset;

    private void Awake()    => StalactiteManager.Register(this);
    private void OnDestroy() => StalactiteManager.Unregister(this);

    public void Init(
        Vector3   originPosition,
        float     resetDelay,
        LayerMask groundLayer,
        Color     particleColor,
        float     shakeDelay,
        float     shakeIntensity,
        Sprite    sprite,
        Vector2   colliderSize,
        Vector2   detectionZoneSize,
        Vector2   detectionZoneOffset)
    {
        _originPosition      = originPosition;
        _resetDelay          = resetDelay;
        _groundLayer         = groundLayer;
        _particleColor       = particleColor;
        _shakeDelay          = shakeDelay;
        _shakeIntensity      = shakeIntensity;
        _sprite              = sprite;
        _colliderSize        = colliderSize;
        _detectionZoneSize   = detectionZoneSize;
        _detectionZoneOffset = detectionZoneOffset;
    }

    public void TriggerRespawn()
    {
        Reconstruct();
        Destroy(gameObject);
    }

    private void Reconstruct()
    {
        // 1. Root GO at original ceiling position
        GameObject stalactiteGO = new GameObject("Stalactite");
        stalactiteGO.transform.position = _originPosition;

        // 2. SpriteRenderer
        SpriteRenderer sr = stalactiteGO.AddComponent<SpriteRenderer>();
        sr.sprite = _sprite;

        // 3. Rigidbody2D — Kinematic at rest, Continuous for reliable fall collision
        Rigidbody2D rb = stalactiteGO.AddComponent<Rigidbody2D>();
        rb.bodyType               = RigidbodyType2D.Kinematic;
        rb.constraints            = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // 4. Body collider — solid, not a trigger
        BoxCollider2D col = stalactiteGO.AddComponent<BoxCollider2D>();
        col.size      = _colliderSize;
        col.isTrigger = false;

        // 5. Stalactite behaviour — Awake runs here, reads position + sprite + rb
        Stalactite stalactite = stalactiteGO.AddComponent<Stalactite>();
        stalactite.groundLayer    = _groundLayer;
        stalactite.particleColor  = _particleColor;
        stalactite.shakeDelay     = _shakeDelay;
        stalactite.shakeIntensity = _shakeIntensity;
        stalactite.resetDelay     = _resetDelay;

        // 6. Detection zone child
        GameObject detectionZoneGO = new GameObject("DetectionZone");
        detectionZoneGO.transform.SetParent(stalactiteGO.transform, false);

        // 7. Trigger collider sized to original detection zone
        BoxCollider2D detectionCol = detectionZoneGO.AddComponent<BoxCollider2D>();
        detectionCol.size      = _detectionZoneSize;
        detectionCol.offset    = _detectionZoneOffset;
        detectionCol.isTrigger = true;

        // 8. Trigger zone script — Awake finds parent Stalactite via GetComponentInParent
        detectionZoneGO.AddComponent<StalactiteTriggerZone>();
    }
}
