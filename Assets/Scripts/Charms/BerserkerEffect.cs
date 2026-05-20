using UnityEngine;

[CreateAssetMenu(fileName = "BerserkerEffect", menuName = "Charms/Effects/Berserker")]
public class BerserkerEffect : CharmEffect
{
    public override void Apply(PlayerHealth health)
    {
        if (PlayerMovement.Instance == null) return;
        PlayerMovement.Instance.gameObject.AddComponent<BerserkerMonitor>();
    }

    public override void Remove(PlayerHealth health)
    {
        if (PlayerMovement.Instance == null) return;
        var monitor = PlayerMovement.Instance.GetComponent<BerserkerMonitor>();
        if (monitor != null)
            Object.Destroy(monitor);
    }
}

public class BerserkerMonitor : MonoBehaviour
{
    private bool _isActive;
    private int _originalAttackDamage;
    private SpriteRenderer _slashRenderer;
    private ParticleSystem _particles;

    private void Awake()
    {
        _slashRenderer = transform.Find("SlashEffect")?.GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (PlayerHealth.Instance == null) return;

        int lives = PlayerHealth.Instance.GetCurrentLives();

        if (lives == 1 && !_isActive)
            Activate();
        else if (lives > 1 && _isActive)
            Deactivate();
    }

    private void Activate()
    {
        _isActive = true;
        _originalAttackDamage = PlayerMovement.Instance.attackDamage;
        PlayerMovement.Instance.attackDamage *= 2;

        if (_slashRenderer != null)
            _slashRenderer.color = Color.red;

        SpawnParticles();
    }

    private void Deactivate()
    {
        _isActive = false;

        if (PlayerMovement.Instance != null)
            PlayerMovement.Instance.attackDamage = _originalAttackDamage;

        if (_slashRenderer != null)
            _slashRenderer.color = Color.white;

        if (_particles != null)
        {
            Destroy(_particles.gameObject);
            _particles = null;
        }
    }

    private void SpawnParticles()
    {
        var go = new GameObject("BerserkerParticles");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.zero;

        _particles = go.AddComponent<ParticleSystem>();
        _particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = _particles.main;
        main.loop            = true;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(0.3f, 0.5f);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
        main.startSize       = new ParticleSystem.MinMaxCurve(0.05f, 0.1f);
        main.startColor      = new ParticleSystem.MinMaxGradient(new Color(1f, 0f, 0f, 1f));
        main.gravityModifier = -0.5f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var renderer = _particles.GetComponent<ParticleSystemRenderer>();
        renderer.material       = new Material(Shader.Find("Sprites/Default"));
        renderer.material.color = new Color(1f, 0f, 0f, 1f);

        var emission = _particles.emission;
        emission.enabled      = true;
        emission.rateOverTime = 30f;

        var shape = _particles.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius    = 0.3f;

        _particles.Play();
    }

    private void OnDestroy()
    {
        if (_isActive)
            Deactivate();
    }
}
