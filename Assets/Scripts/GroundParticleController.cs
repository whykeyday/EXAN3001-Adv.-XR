using UnityEngine;

/// <summary>
/// GroundParticleController: 
/// 将 Plane 地面转换为透明粒子容器的通用控制组件。
/// 支持手动在 Inspector 面板调整：大小、密度、颜色。
/// </summary>
public class GroundParticleController : MonoBehaviour
{
    public enum MovementMode { BasicTwinkle, OceanWavy, CatBlinking, TreeRandom }

    [Header("--- Manual Adjustments ---")]
    [Tooltip("粒子的基础颜色")]
    public Color mainColor = Color.white;
    
    [Tooltip("粒子的大小 (建议 0.02 - 0.1)")]
    [Range(0.001f, 0.2f)]
    public float particleSize = 0.04f;

    [Tooltip("粒子的密度 (每秒发射数量)")]
    [Range(5f, 500f)]
    public float particleDensity = 80f;

    [Tooltip("地面的动态模式")]
    public MovementMode mode = MovementMode.BasicTwinkle;

    [Header("--- Internals ---")]
    private ParticleSystem ps;
    private ParticleSystemRenderer psr;

    void Awake()
    {
        SetupParticleSystem();
        HideBaseMesh();
    }

    void Update()
    {
        // 实时应用 Inspector 的调整
        ApplyAdjustments();
    }

    void SetupParticleSystem()
    {
        // 创建粒子系统物体
        GameObject psObj = new GameObject("GroundParticles_Container");
        psObj.transform.SetParent(transform, false);
        // 紧贴地面 (Y=0.01)
        psObj.transform.localPosition = new Vector3(0, 0.01f, 0);

        ps = psObj.AddComponent<ParticleSystem>();
        psr = psObj.GetComponent<ParticleSystemRenderer>();

        // 1. 基础设置 (Main)
        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 8f);
        main.gravityModifier = 0f;
        main.maxParticles = 2000;

        // 2. 发射形状 (Shape) - 自动匹配 Plane 的大小
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        // Plane 的默认大小是 10x10，Scale 1 对应 10m
        shape.scale = new Vector3(10f, 0.1f, 10f); 
        shape.rotation = new Vector3(90, 0, 0); // 确保朝上发射

        // 3. 渲染器设置 (Renderer)
        psr.material = GetGlowingMaterial();
        psr.renderMode = ParticleSystemRenderMode.Billboard;

        ConfigureMode();
        ps.Play();
    }

    void HideBaseMesh()
    {
        // 让原始的 Plane 彻底透明，只作为容器
        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr != null)
        {
            // 创建一个全透明材质
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (mat != null)
            {
                mat.SetFloat("_Surface", 1); // Transparent
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.SetColor("_BaseColor", new Color(0, 0, 0, 0));
                mr.material = mat;
            }
        }
    }

    void ConfigureMode()
    {
        var main = ps.main;
        var noise = ps.noise;
        var colorOL = ps.colorOverLifetime;
        var emission = ps.emission;

        switch (mode)
        {
            case MovementMode.BasicTwinkle:
                // 五颜六色闪耀
                main.startColor = new ParticleSystem.MinMaxGradient(Color.white, Color.grey);
                emission.rateOverTime = particleDensity;
                colorOL.enabled = true;
                colorOL.color = GetTwinkleGradient();
                break;

            case MovementMode.OceanWavy:
                // 蓝色波动 (使用 Noise)
                main.startColor = mainColor;
                noise.enabled = true;
                noise.strength = 0.5f;
                noise.frequency = 0.5f;
                noise.scrollSpeed = 0.2f;
                break;

            case MovementMode.CatBlinking:
                // 淡红闪烁
                main.startColor = mainColor;
                colorOL.enabled = true;
                colorOL.color = GetBlinkingGradient();
                break;

            case MovementMode.TreeRandom:
                // 黄色随机移动
                main.startColor = mainColor;
                var vel = ps.velocityOverLifetime;
                vel.enabled = true;
                vel.x = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);
                vel.z = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);
                noise.enabled = true;
                noise.strength = 0.1f;
                break;
        }
    }

    void ApplyAdjustments()
    {
        if (ps == null) return;

        var main = ps.main;
        main.startSize = particleSize;
        
        // 如果是 Basic 模式，强制应用随机颜色
        if (mode == MovementMode.BasicTwinkle) {
            // 自动循环颜色
            main.startColor = new ParticleSystem.MinMaxGradient(mainColor, Color.white);
        } else {
            main.startColor = mainColor;
        }

        var emission = ps.emission;
        emission.rateOverTime = particleDensity;
    }

    // --- Helpers ---

    private Material GetGlowingMaterial()
    {
        Shader s = Shader.Find("Universal Render Pipeline/Unlit");
        if (s == null) s = Shader.Find("Sprites/Default");
        Material mat = new Material(s);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        mat.SetInt("_ZWrite", 0);
        return mat;
    }

    private Gradient GetTwinkleGradient()
    {
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.red, 0f), new GradientColorKey(Color.yellow, 0.3f), new GradientColorKey(Color.cyan, 0.6f), new GradientColorKey(Color.white, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.5f), new GradientAlphaKey(0, 1) }
        );
        return g;
    }

    private Gradient GetBlinkingGradient()
    {
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.2f), new GradientAlphaKey(1, 0.8f), new GradientAlphaKey(0, 1) }
        );
        return g;
    }
}
