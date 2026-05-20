using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class Stalactite : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float _shakeDelay     = 0.5f;
    [SerializeField] private float _resetDelay     = 5f;

    [Header("Shake")]
    [SerializeField] private float _shakeIntensity = 0.05f;

    [Header("Particles")]
    [SerializeField] private Color _particleColor  = Color.gray;

    [Header("Detection")]
    [SerializeField] private LayerMask _groundLayer;

    // Public setters required by StalactiteRespawner
    public float     shakeDelay     { get => _shakeDelay;     set => _shakeDelay = value; }
    public float     resetDelay     { get => _resetDelay;     set => _resetDelay = value; }
    public float     shakeIntensity { get => _shakeIntensity; set => _shakeIntensity = value; }
    public Color     particleColor  { get => _particleColor;  set => _particleColor = value; }
    public LayerMask groundLayer    { get => _groundLayer;    set => _groundLayer = value; }

    private Vector3  _originPosition;
    private Sprite   _originalSprite;
    private Rigidbody2D _rb;
    private bool _isFalling;
    private bool _hasImpacted;

    private const string PlayerTag = "Player";

    private void Awake()
    {
        _originPosition  = transform.position;
        _originalSprite  = GetComponent<SpriteRenderer>().sprite;
        _rb              = GetComponent<Rigidbody2D>();
        _rb.bodyType     = RigidbodyType2D.Kinematic;
    }

    public void OnPlayerEnterZone()
    {
        if (!_isFalling)
            StartCoroutine(FallSequence());
    }

    private IEnumerator FallSequence()
    {
        _isFalling = true;
        yield return StartCoroutine(ShakeRoutine());
        _rb.bodyType = RigidbodyType2D.Dynamic;
    }

    private IEnumerator ShakeRoutine()
    {
        float elapsed = 0f;
        while (elapsed < _shakeDelay)
        {
            float offset = Mathf.Sin(elapsed * 30f * Mathf.PI * 2f) * _shakeIntensity;
            transform.position = _originPosition + new Vector3(offset, 0f, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = _originPosition;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!_isFalling || _hasImpacted)
            return;

        bool hitGround = (_groundLayer & (1 << collision.gameObject.layer)) != 0;
        bool hitPlayer = collision.gameObject.CompareTag(PlayerTag);

        if (!hitGround && !hitPlayer)
            return;

        _hasImpacted = true;

        if (hitPlayer)
            PlayerHealth.Instance?.TakeDamage();

        SpawnParticles();
        SpawnRespawner();
        Destroy(gameObject);
    }

    private void SpawnRespawner()
    {
        BoxCollider2D col = GetComponent<BoxCollider2D>();

        Transform detectionZoneT = transform.Find("DetectionZone");
        BoxCollider2D detectionZoneCol = detectionZoneT != null
            ? detectionZoneT.GetComponent<BoxCollider2D>()
            : null;

        GameObject respawnerGO = new GameObject("StalactiteRespawner");
        StalactiteRespawner respawner = respawnerGO.AddComponent<StalactiteRespawner>();
        respawner.Init(
            _originPosition,
            _resetDelay,
            _groundLayer,
            _particleColor,
            _shakeDelay,
            _shakeIntensity,
            _originalSprite,
            col.size,
            detectionZoneCol != null ? detectionZoneCol.size   : Vector2.one,
            detectionZoneCol != null ? detectionZoneCol.offset : Vector2.zero
        );
    }

    private void SpawnParticles()
    {
        GameObject particleGO = new GameObject("Stalactite_Particles");
        particleGO.transform.position = transform.position;

        ParticleSystem ps = particleGO.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration        = 0.5f;
        main.loop            = false;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(2f, 7f);
        main.startSize       = new ParticleSystem.MinMaxCurve(0.05f, 0.18f);
        main.startColor      = new ParticleSystem.MinMaxGradient(_particleColor);
        main.gravityModifier = 0.4f;
        main.maxParticles    = 20;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.enabled      = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 18) });

        var shape = ps.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius    = 0.3f;

        ps.Play();
        Destroy(particleGO, 1f);
    }
}
