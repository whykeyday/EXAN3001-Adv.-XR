using UnityEngine;

/// <summary>
/// GroundParticleController (Coral Style - Master Version): 
/// 1. 复刻珊瑚的几何金属感 (Metallic Gems)。
/// 2. 自动探测地面边界 (Auto-Bounds) 并铺满全场。
/// 3. 实现绝对透明的“容器”化。
/// </summary>
public class GroundParticleController : MonoBehaviour
{
    public enum MovementMode { BasicTwinkle, OceanWavy, CatBlinking, TreeRandom }

    [Header("--- Manual Adjustments ---")]
    public Color mainColor = Color.white;
    
    [Range(0.001f, 0.2f)]
    public float particleSize = 0.04f;

    [Range(5f, 1000f)]
    public float particleDensity = 150f;

    public MovementMode mode = MovementMode.BasicTwinkle;

    private ParticleSystem ps;
    private ParticleSystemRenderer psr;
    private MeshRenderer baseMr;

    void Awake()
    {
        baseMr = GetComponent<MeshRenderer>();
        SetupParticleSystem();
        HideBaseMesh();
    }

    void Update()
    {
        ApplyAdjustments();
    }

    void SetupParticleSystem()
    {
        GameObject psObj = new GameObject("CoralStyle_GroundParticles");
        psObj.transform.SetParent(transform, false);
        psObj.transform.localPosition = new Vector3(0, 0.02f, 0);
        psObj.transform.localRotation = Quaternion.identity;
        psObj.transform.localScale = Vector3.one;

        ps = psObj.AddComponent<ParticleSystem>();
        psr = psObj.GetComponent<ParticleSystemRenderer>();

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(2.0f, 4.0f);
        main.startSpeed = 0f;
        main.gravityModifier = 0f;
        main.maxParticles = 5000;

        // ★ 珊瑚级核心 1：自适应全图覆盖 (Auto-Bounds)
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        
        if (baseMr != null) {
            // 通过获取 Bounds 的世界尺寸来同步粒子范围
            Vector3 worldSize = baseMr.bounds.size;
            // 因为粒子是子物体，所以要除以父物体的 LossyScale 来抵消
            Vector3 localSize = new Vector3(
                worldSize.x / transform.lossyScale.x,
                0.01f,
                worldSize.z / transform.lossyScale.z
            );
            shape.scale = localSize;
        } else {
            shape.scale = new Vector3(10f, 0.01f, 10f); // Fallback
        }

        // ★ 珊瑚级核心 2：几何金属化渲染 (Metallic Cube Rendering)
        psr.renderMode = ParticleSystemRenderMode.Mesh;
        GameObject tempCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        psr.mesh = tempCube.GetComponent<MeshFilter>().sharedMesh;
        DestroyImmediate(tempCube);

        psr.material = CreateMetallicMaterial(mainColor);

        ConfigureMode();
        ps.Play();
    }

    void HideBaseMesh()
    {
        // ★ 核心 3：强制隐藏地面，实现 100% 透明容器
        if (baseMr != null) baseMr.enabled = false;
    }

    void ConfigureMode()
    {
        var noise = ps.noise;
        var colorOL = ps.colorOverLifetime;

        switch (mode)
        {
            case MovementMode.BasicTwinkle:
                colorOL.enabled = true;
                colorOL.color = GetTwinkleGradient();
                break;

            case MovementMode.OceanWavy:
                noise.enabled = true;
                noise.strength = 0.15f; 
                noise.frequency = 0.2f;
                noise.scrollSpeed = 0.1f;
                break;

            case MovementMode.CatBlinking:
                colorOL.enabled = true;
                colorOL.color = GetBlinkingGradient();
                break;

            case MovementMode.TreeRandom:
                var vel = ps.velocityOverLifetime;
                vel.enabled = true;
                vel.x = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);
                vel.z = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);
                vel.y = new ParticleSystem.MinMaxCurve(0f, 0f);
                noise.enabled = true;
                noise.strength = 0.02f; 
                break;
        }
    }

    void ApplyAdjustments()
    {
        if (ps == null) return;
        var main = ps.main;
        main.startSize = particleSize;
        main.startColor = mainColor;

        var emission = ps.emission;
        emission.rateOverTime = particleDensity;

        // 如果你在 Inspector 调整了颜色，材质也会动态更新反光色
        if (psr != null && psr.material != null) {
            psr.material.SetColor("_BaseColor", new Color(mainColor.r, mainColor.g, mainColor.b, 0.6f));
            psr.material.SetColor("_Color", new Color(mainColor.r, mainColor.g, mainColor.b, 0.6f));
        }
    }

    // --- 珊瑚级核心材质：高反射、高金属感、极致透明 ---
    private Material CreateMetallicMaterial(Color color)
    {
        Shader s = Shader.Find("Universal Render Pipeline/Lit");
        if (s == null) s = Shader.Find("Standard");
        
        Material mat = new Material(s);
        
        // 透明模式开关 (URP & Standard)
        mat.SetFloat("_Surface", 1); 
        mat.SetFloat("_Mode", 3);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = 3000;
        
        Color tColor = new Color(color.r, color.g, color.b, 0.6f);
        mat.SetColor("_BaseColor", tColor);
        mat.SetColor("_Color", tColor);
        
        // ★ 核心：金属镜面质感 (同珊瑚设置)
        mat.SetFloat("_Metallic", 0.92f);
        mat.SetFloat("_Smoothness", 0.96f);
        mat.SetFloat("_Glossiness", 0.96f);
        
        return mat;
    }

    private Gradient GetTwinkleGradient()
    {
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.red, 0f), new GradientColorKey(Color.yellow, 0.3f), new GradientColorKey(Color.cyan, 0.6f), new GradientColorKey(Color.white, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.1f, 0), new GradientAlphaKey(1, 0.5f), new GradientAlphaKey(0.1f, 1) }
        );
        return g;
    }

    private Gradient GetBlinkingGradient()
    {
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.1f, 0), new GradientAlphaKey(1, 0.3f), new GradientAlphaKey(1, 0.7f), new GradientAlphaKey(0.1f, 1) }
        );
        return g;
    }
}
