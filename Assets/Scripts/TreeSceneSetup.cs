using UnityEngine;

/// <summary>
/// TreeSceneSetup — 只负责树场景氛围设置。
/// 不创建任何树模型！由于用户已有 TransparentParticleTree。
///
/// 功能：
///   - 深棕色森林氛围（雾、天空、光照）
///   - 棕色地面材质（移除白色 Plane）
///   - 稀疏棕色地面粒子
///   - 森林环境音设置
///
/// 用法：挂到 TreeManager 物体上（和 TreeHealer 在同一个物体）
/// </summary>
public class TreeSceneSetup : MonoBehaviour
{
    [Header("Atmosphere — 深棕森林色调")]
    public Color forestFogColor = new Color(0.06f, 0.04f, 0.02f);
    public Color forestSkyColor = new Color(0.03f, 0.02f, 0.01f);

    [Header("Audio")]
    [Tooltip("森林环境音频文件（循环）")]
    public AudioClip ambientClip;

    private AudioSource ambientAudio;

    void Start()
    {
        SetupAtmosphere();
        MakePlaneBrown();
        // CreateGroundParticles(); // 禁用旧的背景粒子，改为统一的 GroundParticle 控制器
        SetupAudio();
    }

    void SetupAtmosphere()
    {
        // 深棕色森林氛围
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = Color.black; // 强制黑雾
        RenderSettings.fogDensity = 0.003f; // 极致稀薄

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.02f, 0.015f, 0.01f); 
        RenderSettings.ambientIntensity = 0.15f; // 压低环境光
        RenderSettings.skybox = null;

        // 方向光调暖月光色
        Light[] lights = FindObjectsOfType<Light>();
        foreach (var l in lights)
        {
            if (l.type == LightType.Directional)
            {
                l.intensity = 0.08f;
                l.color = new Color(0.8f, 0.6f, 0.3f); 
            }
        }

        Camera cam = Camera.main;
        if (cam == null) cam = FindObjectOfType<Camera>();
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = forestSkyColor;
        }
    }

    [Header("Ground Particle Container")]
    public GameObject groundObject;

    /// <summary>
    /// 将指定 Ground Object 转换为黄色随机漂移粒子容器
    /// </summary>
    void MakePlaneBrown()
    {
        if (groundObject == null)
        {
            Debug.LogWarning("[TreeSceneSetup] 未指定 groundObject。请在 Inspector 中拖入地板物品。");
            return;
        }

        // 挂载粒子控制器
        GroundParticleController controller = groundObject.GetComponent<GroundParticleController>();
        if (controller == null) controller = groundObject.AddComponent<GroundParticleController>();
        
        // 设置森林默认参数：黄色，随机移动
        controller.mainColor = new Color(1.0f, 0.75f, 0.2f, 0.8f);
        controller.mode = GroundParticleController.MovementMode.TreeRandom;
        controller.particleDensity = 150f;
        controller.particleSize = 0.05f;
    }

    /// <summary>
    /// 稀疏深棕色地面粒子 — 在地面微微漂浮
    /// </summary>
    void CreateGroundParticles()
    {
        GameObject psObj = new GameObject("GroundDustParticles");
        psObj.transform.position = new Vector3(0f, 0.1f, 0f);

        ParticleSystem ps = psObj.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = true;
        main.prewarm = true;
        main.duration = 30f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(8f, 15f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.01f, 0.04f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.06f);
        main.maxParticles = 80; 
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0f;

        // 深棕色
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.12f, 0.08f, 0.03f, 0.4f),
            new Color(0.2f, 0.14f, 0.06f, 0.6f)
        );

        var emission = ps.emission;
        emission.rateOverTime = 5f; 

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(12f, 0.3f, 12f); 

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.y = new ParticleSystem.MinMaxCurve(-0.01f, 0.03f);

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.03f;
        noise.frequency = 0.3f;
        noise.scrollSpeed = 0.1f;

        var colorOL = ps.colorOverLifetime;
        colorOL.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.15f, 0.1f, 0.04f), 0f),
                new GradientColorKey(new Color(0.18f, 0.12f, 0.05f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.5f, 0.2f),
                new GradientAlphaKey(0.4f, 0.7f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOL.color = grad;

        var psr = ps.GetComponent<ParticleSystemRenderer>();
        psr.material = ParticleUtils.GetGlowingSphereMaterial();

        ps.Play();
    }

    void SetupAudio()
    {
        if (ambientClip != null)
        {
            ambientAudio = gameObject.AddComponent<AudioSource>();
            ambientAudio.clip = ambientClip;
            ambientAudio.spatialBlend = 0f;
            ambientAudio.loop = true;
            ambientAudio.playOnAwake = false;
            ambientAudio.Play();
            AudioDistanceFader.Setup(ambientAudio, 6f, 1f);
        }
    }
}
