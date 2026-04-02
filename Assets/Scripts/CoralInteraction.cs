using UnityEngine;

/// <summary>
/// Attach to coral particle objects.
/// When player touches the coral, releases WHITE particles that float upward.
/// </summary>
[RequireComponent(typeof(Collider))]
public class CoralInteraction : MonoBehaviour
{
    [Header("Effect Settings")]
    [Tooltip("Particle system that emits white particles on touch")]
    public ParticleSystem releaseEffect;

    [Tooltip("Number of particles to emit on each touch")]
    public int particlesToEmit = 30;

    [Tooltip("Cooldown between emissions (seconds)")]
    public float cooldown = 0.5f;

    [Header("Auto-Create Effect")]
    [Tooltip("If true, automatically creates the release effect if not assigned")]
    public bool autoCreateEffect = true;

    private float lastEmitTime = -999f;

    private void Start()
    {
        // Ensure collider is trigger
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        // Auto-create release effect if needed
        if (releaseEffect == null && autoCreateEffect)
        {
            CreateReleaseEffect();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Accept any collider that isn't part of this coral
        if (other.transform.IsChildOf(transform.root)) return;
        
        Debug.Log($"[CoralInteraction] 触发！碰撞体名称是: {other.name}");
        TryEmitParticles();
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.transform.IsChildOf(transform.root)) return;
        TryEmitParticles();
    }

    private void TryEmitParticles()
    {
        if (releaseEffect == null) return;

        // Check cooldown
        if (Time.time - lastEmitTime < cooldown) return;

        lastEmitTime = Time.time;

        // Emit particles at contact point
        releaseEffect.Emit(particlesToEmit);
        Debug.Log($"[CoralInteraction] 成功释放了 {particlesToEmit} 个白色粒子！");
    }

    private void CreateReleaseEffect()
    {
        // Create a child object for the release effect
        GameObject effectObj = new GameObject("ReleaseEffect");
        effectObj.transform.SetParent(transform, false);
        effectObj.transform.localPosition = new Vector3(0, -0.08f, 0);

        ParticleSystem ps = effectObj.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psr = effectObj.GetComponent<ParticleSystemRenderer>();

        var main = ps.main;
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.08f); // 更大更亮
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.startSpeed = 0.15f; // 更慢
        main.startLifetime = 8f;  // 存活更久
        main.maxParticles = 500;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.loop = false;
        main.playOnAwake = false;
        main.gravityModifier = -0.03f; // 更轻柔的上飘

        // 白色粒子 HDR 增强！
        main.startColor = new Color(1f, 1f, 1f, 1f) * 1.5f;

        // No continuous emission - only emit via code
        var emission = ps.emission;
        emission.rateOverTime = 0f;

        // Small spawn area around coral
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.15f;

        // Float upward gently
        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.y = new ParticleSystem.MinMaxCurve(0.08f, 0.2f); // 更慢的上飘

        // Gentle side-to-side wobble
        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.1f;
        noise.frequency = 0.6f;
        noise.scrollSpeed = 0.3f;
        noise.separateAxes = true;
        noise.strengthX = 0.12f;
        noise.strengthY = 0.02f; 
        noise.strengthZ = 0.12f;

        // Size varies slightly over lifetime
        var sizeOverLife = ps.sizeOverLifetime;
        sizeOverLife.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.5f);
        sizeCurve.AddKey(0.3f, 1f);
        sizeCurve.AddKey(1f, 0.2f);
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // 白色渐变：纯白 → 淡蓝白 → 消失
        var colorOverLife = ps.colorOverLifetime;
        colorOverLife.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 1f, 1f), 0f),        // 纯白
                new GradientColorKey(new Color(0.9f, 0.95f, 1f), 0.5f), // 微蓝白
                new GradientColorKey(new Color(0.8f, 0.9f, 1f), 1f)     // 淡蓝
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),       // Fade in
                new GradientAlphaKey(0.9f, 0.15f),  // Strong
                new GradientAlphaKey(0.6f, 0.6f),   // Start fading
                new GradientAlphaKey(0f, 1f)         // Vanish
            }
        );
        colorOverLife.color = gradient;

        // Use Glowing Sphere material
        psr.material = ParticleUtils.GetGlowingSphereMaterial();

        releaseEffect = ps;
    }
}
