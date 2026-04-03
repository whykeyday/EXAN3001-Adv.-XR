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

    [Header("Bowl Coral Special")]
    [Tooltip("启用碗状珊瑚独特的上升点亮特效")]
    public bool isBowlCoral = false;

    private float lastEmitTime = -999f;
    private Light bowlLight;

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
            if (isBowlCoral)
                CreateBowlCoralEffect();
            else
                CreateReleaseEffect();
        }

        if (isBowlCoral)
            CreateBowlLight();
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

        // 碗状珊瑚触发光脉冲
        if (isBowlCoral && bowlLight != null)
        {
            StartCoroutine(BowlLightPulse());
        }
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

    /// <summary>
    /// 碗状珊瑚独特上升点亮特效：半球形发射 + 更快上升 + 暖白光粒子
    /// </summary>
    private void CreateBowlCoralEffect()
    {
        GameObject effectObj = new GameObject("BowlReleaseEffect");
        effectObj.transform.SetParent(transform, false);
        effectObj.transform.localPosition = Vector3.zero;

        ParticleSystem ps = effectObj.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psr = effectObj.GetComponent<ParticleSystemRenderer>();

        var main = ps.main;
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.1f);
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
        main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 7f);
        main.maxParticles = 500;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.loop = false;
        main.playOnAwake = false;
        main.gravityModifier = -0.05f;

        // 暖白色 HDR 粒子
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.95f, 0.8f, 1f) * 1.5f,
            new Color(1f, 1f, 1f, 1f) * 2.0f
        );

        var emission = ps.emission;
        emission.rateOverTime = 0f;

        // 半球形发射（开口朝上），模拟从碗内上升
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Hemisphere;
        shape.radius = 0.2f;
        shape.rotation = new Vector3(-90f, 0f, 0f); // 开口朝上

        // 更强的上升力
        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.y = new ParticleSystem.MinMaxCurve(0.15f, 0.4f);

        // 螺旋上升效果
        velocity.orbitalY = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);

        // 柔和摇曳
        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.08f;
        noise.frequency = 0.5f;

        // 渐变：暖白 → 明亮 → 淡出
        var colorOverLife = ps.colorOverLifetime;
        colorOverLife.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 0.9f, 0.7f), 0f),
                new GradientColorKey(new Color(1f, 1f, 0.95f), 0.3f),
                new GradientColorKey(new Color(0.9f, 0.95f, 1f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.1f),
                new GradientAlphaKey(0.8f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLife.color = gradient;

        // 尺寸变化
        var sizeOL = ps.sizeOverLifetime;
        sizeOL.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.3f);
        sizeCurve.AddKey(0.2f, 1f);
        sizeCurve.AddKey(0.7f, 0.8f);
        sizeCurve.AddKey(1f, 0f);
        sizeOL.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        psr.material = ParticleUtils.GetGlowingSphereMaterial();
        releaseEffect = ps;
    }

    private void CreateBowlLight()
    {
        GameObject lightObj = new GameObject("BowlCoralLight");
        lightObj.transform.SetParent(transform);
        lightObj.transform.localPosition = Vector3.up * 0.1f;
        bowlLight = lightObj.AddComponent<Light>();
        bowlLight.type = LightType.Point;
        bowlLight.color = new Color(0.8f, 0.9f, 1f);
        bowlLight.range = 3f;
        bowlLight.intensity = 0f;
    }

    private System.Collections.IEnumerator BowlLightPulse()
    {
        if (bowlLight == null) yield break;

        // 快速亮起
        float elapsed = 0f;
        float riseTime = 0.3f;
        while (elapsed < riseTime)
        {
            elapsed += Time.deltaTime;
            bowlLight.intensity = Mathf.Lerp(0f, 5f, elapsed / riseTime);
            yield return null;
        }

        // 缓慢熄灭
        elapsed = 0f;
        float fadeTime = 2f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            bowlLight.intensity = Mathf.Lerp(5f, 0f, elapsed / fadeTime);
            yield return null;
        }

        bowlLight.intensity = 0f;
    }
}
