using UnityEngine;

/// <summary>
/// GroundParticleController (Ground Zero - Final Fix): 
/// 1. 强力锁定 Y 轴高度 (Height Lock) 到脚底 (World Y = 0.05)。
/// 2. 扩大的搜索逻辑 (Scan for Ground/Terrain/Water)。
/// 3. 设置更为宽广的默认发射范围。
/// </summary>
public class GroundParticleController : MonoBehaviour
{
    public enum MovementMode { BasicTwinkle, OceanWavy, CatBlinking, TreeRandom }

    [Header("--- Manual Adjustments ---")]
    public Color mainColor = Color.white;
    
    [Range(0.001f, 0.2f)]
    public float particleSize = 0.045f;

    [Range(5f, 1500f)]
    public float particleDensity = 250f;

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

    void Start()
    {
        // 进一步扩大默认范围以确保 100% 覆盖
        UpdateEmissionBounds();
    }

    void Update()
    {
        ApplyAdjustments();
        
        // ★ 核心修复 1：地心锁定 (Height Lock)。
        // 无论 Plane/Water 物体本身在腰部还是眼睛，粒子物体都被拽到脚底 (Y=0.05)
        if (ps != null) {
            ps.transform.position = new Vector3(transform.position.x, 0.05f, transform.position.z);
        }
    }

    void SetupParticleSystem()
    {
        GameObject psObj = new GameObject("GroundZero_Particles");
        // 注意：这里不设为 transform 的子物体，或者设为子物体但 Update 里强制设 WorldPos
        psObj.transform.SetParent(transform, true); 

        ps = psObj.AddComponent<ParticleSystem>();
        psr = psObj.GetComponent<ParticleSystemRenderer>();

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World; // 世界空间以便锁定高度
        main.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 5.0f);
        main.startSpeed = 0f;
        main.gravityModifier = 0f;
        main.maxParticles = 8000;

        // ★ 核心修复 2：超大范围发射
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        // 初始给一个巨大的覆盖面积 (50x50m)，稍后在 UpdateEmissionBounds 精调
        shape.scale = new Vector3(50f, 0.01f, 50f); 

        // ★ 珊瑚级渲染
        psr.renderMode = ParticleSystemRenderMode.Mesh;
        GameObject tempCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        psr.mesh = tempCube.GetComponent<MeshFilter>().sharedMesh;
        DestroyImmediate(tempCube);
        psr.material = CreateMetallicMaterial(mainColor);

        ConfigureMode();
        ps.Play();
    }

    void UpdateEmissionBounds()
    {
        if (baseMr == null || ps == null) return;
        
        var shape = ps.shape;
        // 探测包围盒大小
        Vector3 worldSize = baseMr.bounds.size;
        
        // 如果测得的包围盒太小 (可能是局部 Mesh)，强制最小 30 米
        float finalX = Mathf.Max(worldSize.x, 40f);
        float finalZ = Mathf.Max(worldSize.z, 40f);

        // 应用到 Shape (注意坐标系转化)
        shape.scale = new Vector3(finalX / transform.lossyScale.x, 0.01f, finalZ / transform.lossyScale.z);
    }

    void HideBaseMesh()
    {
        // ★ 核心修复 3：强制彻底隐藏
        if (baseMr != null) baseMr.enabled = false;
        
        // 同时也尝试寻找父级或相关的 Renderer
        foreach (var r in GetComponentsInChildren<Renderer>()) {
            if (r != psr) r.enabled = false;
        }
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
                noise.strength = 0.12f; 
                noise.frequency = 0.25f;
                noise.scrollSpeed = 0.1f;
                break;
            case MovementMode.CatBlinking:
                colorOL.enabled = true;
                colorOL.color = GetBlinkingGradient();
                break;
            case MovementMode.TreeRandom:
                var vel = ps.velocityOverLifetime;
                vel.enabled = true;
                vel.x = new ParticleSystem.MinMaxCurve(-0.03f, 0.03f);
                vel.z = new ParticleSystem.MinMaxCurve(-0.03f, 0.03f);
                vel.y = new ParticleSystem.MinMaxCurve(0f, 0f);
                noise.enabled = true;
                noise.strength = 0.015f; 
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

        if (psr != null && psr.material != null) {
            psr.material.SetColor("_BaseColor", new Color(mainColor.r, mainColor.g, mainColor.b, 0.55f));
            psr.material.SetColor("_Color", new Color(mainColor.r, mainColor.g, mainColor.b, 0.55f));
        }
    }

    private Material CreateMetallicMaterial(Color color)
    {
        Shader s = Shader.Find("Universal Render Pipeline/Lit");
        if (s == null) s = Shader.Find("Standard");
        Material mat = new Material(s);
        mat.SetFloat("_Surface", 1); 
        mat.SetFloat("_Mode", 3);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = 3000;
        
        Color tColor = new Color(color.r, color.g, color.b, 0.55f);
        mat.SetColor("_BaseColor", tColor);
        mat.SetColor("_Color", tColor);
        mat.SetFloat("_Metallic", 0.93f);
        mat.SetFloat("_Smoothness", 0.97f);
        mat.SetFloat("_Glossiness", 0.97f);
        return mat;
    }

    private Gradient GetTwinkleGradient()
    {
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.red, 0f), new GradientColorKey(Color.yellow, 0.3f), new GradientColorKey(Color.cyan, 0.6f), new GradientColorKey(Color.white, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.05f, 0), new GradientAlphaKey(1, 0.5f), new GradientAlphaKey(0.05f, 1) }
        );
        return g;
    }

    private Gradient GetBlinkingGradient()
    {
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.05f, 0), new GradientAlphaKey(1, 0.3f), new GradientAlphaKey(1, 0.7f), new GradientAlphaKey(0.05f, 1) }
        );
        return g;
    }
}
