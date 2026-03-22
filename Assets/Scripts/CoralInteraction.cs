using UnityEngine;

/// <summary>
/// Attach to coral particle objects.
/// When player touches the coral, releases yellow particles.
/// </summary>
[RequireComponent(typeof(Collider))]
public class CoralInteraction : MonoBehaviour
{
    [Header("Effect Settings")]
    [Tooltip("Particle system that emits yellow particles on touch")]
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
        Debug.Log($"[CoralInteraction] 成功释放了 {particlesToEmit} 个黄色粒子！");
    }

    private void CreateReleaseEffect()
    {
        // Create a child object for the release effect
        GameObject effectObj = new GameObject("ReleaseEffect");
        effectObj.transform.SetParent(transform, false);
        // 按您的要求，起点往下挪一点
        effectObj.transform.localPosition = new Vector3(0, -0.08f, 0);

        ParticleSystem ps = effectObj.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psr = effectObj.GetComponent<ParticleSystemRenderer>();

        var main = ps.main;
        // 把粒子稍微调大一点点
        main.startSize = new ParticleSystem.MinMaxCurve(0.015f, 0.045f); 
        main.scalingMode = ParticleSystemScalingMode.Hierarchy; // 确保特效跟随珊瑚一并放大
        main.startSpeed = 0.6f; // 更符合火苗的升空速度
        main.startLifetime = 6f; // 存活时间多一倍
        main.maxParticles = 500;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.loop = false;
        main.playOnAwake = false;
        main.gravityModifier = -0.08f; // 更像火焰由于热力往上蹿

        // 火红色类似火焰一样的基色
        main.startColor = new Color(1f, 0.3f, 0.05f, 0.9f);

        // No continuous emission - only emit via code
        var emission = ps.emission;
        emission.rateOverTime = 0f;

        // Small spawn area around coral
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.15f;

        // Float upward like flames
        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.y = new ParticleSystem.MinMaxCurve(0.3f, 0.6f); // Upward velocity

        // Gentle side-to-side wobble (like flames flickering)
        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.15f;
        noise.frequency = 0.8f;
        noise.scrollSpeed = 0.5f;
        noise.separateAxes = true;
        noise.strengthX = 0.15f;
        noise.strengthY = 0.02f; 
        noise.strengthZ = 0.15f;

        // Size varies slightly over lifetime
        var sizeOverLife = ps.sizeOverLifetime;
        sizeOverLife.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.6f);
        sizeCurve.AddKey(0.3f, 1f);
        sizeCurve.AddKey(1f, 0.1f);  // 火焰头慢慢变尖消失
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // 像火焰一样的颜色渐变：从红->橙->黄->最后消失
        var colorOverLife = ps.colorOverLifetime;
        colorOverLife.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 0.1f, 0f), 0f),     // 底部深红
                new GradientColorKey(new Color(1f, 0.5f, 0f), 0.4f),   // 中部亮橙
                new GradientColorKey(new Color(1f, 0.8f, 0.1f), 1f)    // 尖端焰黄
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),      // Fade in quickly
                new GradientAlphaKey(0.8f, 0.2f), // Strong visibility
                new GradientAlphaKey(0.5f, 0.7f), // Starts to fade
                new GradientAlphaKey(0f, 1f)      // Vanish
            }
        );
        colorOverLife.color = gradient;

        // Use Glowing Sphere material instead of hard-edged cubes
        psr.material = ParticleUtils.GetGlowingSphereMaterial();

        releaseEffect = ps;
    }
}
