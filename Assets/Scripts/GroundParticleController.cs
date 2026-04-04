using UnityEngine;

/// <summary>
/// GroundParticleController (Infinite Horizon Version): 
/// 1. 彻底从父级缩放脱离 (Parent Decoupling)，进入 1:1 世界坐标。
/// 2. 强制 80 米超广域覆盖，再也没有“远处一小块”的情况。
/// 3. 环境色深邃化校正，确保背景不透色。
/// </summary>
public class GroundParticleController : MonoBehaviour
{
    public enum MovementMode { BasicTwinkle, OceanWavy, CatBlinking, TreeRandom }

    [Header("--- Manual Adjustments ---")]
    public Color mainColor = Color.white;
    
    [Range(0.001f, 0.2f)]
    public float particleSize = 0.05f;

    [Range(10f, 3000f)]
    public float particleDensity = 400f;

    public MovementMode mode = MovementMode.BasicTwinkle;

    private ParticleSystem ps;
    private ParticleSystemRenderer psr;
    private MeshRenderer baseMr;

    void Awake()
    {
        baseMr = GetComponent<MeshRenderer>();
        SetupParticleSystem();
        HideBaseMesh();
        AdjustEnvironment();
    }

    void Update()
    {
        ApplyAdjustments();
        
        // ★ 核心修复：坐标锁定 (World Align)
        // 使粒子系统保持在指定的 XZ 中心，且锁定在脚底高度 0.05f。
        if (ps != null) {
            ps.transform.position = new Vector3(transform.position.x, 0.05f, transform.position.z);
            // 确保缩放始终为物理 1:1:1
            ps.transform.localScale = Vector3.one; 
        }
    }

    void SetupParticleSystem()
    {
        GameObject psObj = new GameObject("InfiniteHorizon_Particles");
        // ★ 核心修复 2：彻底脱离父级旋转和缩放的干扰，设为 null (Root Object)
        psObj.transform.SetParent(null); 
        psObj.transform.position = new Vector3(transform.position.x, 0.05f, transform.position.z);
        psObj.transform.localScale = Vector3.one;

        ps = psObj.AddComponent<ParticleSystem>();
        psr = psObj.GetComponent<ParticleSystemRenderer>();

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 6f);
        main.startSpeed = 0f;
        main.maxParticles = 12000;

        // ★ 核心修复 3：强力 80x80 米覆盖。
        // 因为它是顶级物体且 Scale 为 1，这里的 80f 就是物理上的 80 米。
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(80f, 0.01f, 80f); 

        // ★ 核心渲染：几何金属化 (Cube Mesh) 复刻珊瑚逻辑
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
        // ★ 核心修复 4：暴力递归隐藏所有 Renderer（确保毫无残留）
        if (baseMr != null) baseMr.enabled = false;
        Renderer[] allRs = GetComponentsInChildren<Renderer>(true);
        foreach (var r in allRs) {
            if (r != psr) r.enabled = false;
        }
    }

    void AdjustEnvironment()
    {
        // ★ 核心修复 5：调暗底色，解决“地面依然有蓝色”的问题
        Camera cam = Camera.main;
        if (cam != null) {
            cam.clearFlags = CameraClearFlags.SolidColor;
            // 确保背景深度足够，不产生灰蒙感
            cam.backgroundColor = new Color(0.01f, 0.02f, 0.04f, 1f); 
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
                noise.strength = 0.14f; 
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

        if (psr != null && psr.material != null) {
            // 同步金属材质色相
            psr.material.SetColor("_BaseColor", new Color(mainColor.r, mainColor.g, mainColor.b, 0.5f));
            psr.material.SetColor("_Color", new Color(mainColor.r, mainColor.g, mainColor.b, 0.5f));
        }
    }

    private Material CreateMetallicMaterial(Color color)
    {
        Shader s = Shader.Find("Universal Render Pipeline/Lit");
        if (s == null) s = Shader.Find("Standard");
        Material mat = new Material(s);
        mat.SetFloat("_Surface", 1); 
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = 3000;
        
        Color tColor = new Color(color.r, color.g, color.b, 0.5f);
        mat.SetColor("_BaseColor", tColor);
        mat.SetFloat("_Metallic", 0.94f); // 极高反射率
        mat.SetFloat("_Smoothness", 0.98f); // 极致镜面
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
