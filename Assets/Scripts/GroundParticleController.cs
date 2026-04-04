using UnityEngine;

/// <summary>
/// GroundParticleController (Precision Coral Version): 
/// 1. 不再进行任何自动化盲目搜索。
/// 2. 专门负责将当前物体变为透明粒子容器。
/// 3. 强制锁定 Y 轴高度到脚底 (Y = 0.05)。
/// </summary>
public class GroundParticleController : MonoBehaviour
{
    public enum MovementMode { BasicTwinkle, OceanWavy, CatBlinking, TreeRandom }

    [Header("--- Manual Adjustments ---")]
    public Color mainColor = Color.white;
    
    [Range(0.001f, 0.2f)]
    public float particleSize = 0.045f;

    [Range(5f, 2000f)]
    public float particleDensity = 300f;

    public MovementMode mode = MovementMode.BasicTwinkle;

    private ParticleSystem ps;
    private ParticleSystemRenderer psr;
    private MeshRenderer baseMr;

    void Awake()
    {
        baseMr = GetComponent<MeshRenderer>();
        SetupParticleSystem();
        HideTargetMesh();
    }

    void Update()
    {
        ApplyAdjustments();
        
        // ★ 核心修复：强制地心引力 (World Y = 0.05)
        // 确保粒子永远在脚底，而不是在手掌、腰部或天空。
        if (ps != null) {
            ps.transform.position = new Vector3(transform.position.x, 0.05f, transform.position.z);
        }
    }

    void SetupParticleSystem()
    {
        // 建立纯净容器
        GameObject psObj = new GameObject("GroundParticles_CoralContainer");
        psObj.transform.SetParent(transform, true); 

        ps = psObj.AddComponent<ParticleSystem>();
        psr = psObj.GetComponent<ParticleSystemRenderer>();

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(2f, 4f);
        main.startSpeed = 0f;
        main.gravityModifier = 0f;
        main.maxParticles = 10000;

        // ★ 珊瑚级覆盖逻辑：自适应容器大小
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        
        if (baseMr != null) {
            Vector3 worldSize = baseMr.bounds.size;
            // 确保覆盖全域，设置最小值为 50m
            float finalX = Mathf.Max(worldSize.x, 50f);
            float finalZ = Mathf.Max(worldSize.z, 50f);
            shape.scale = new Vector3(finalX / transform.lossyScale.x, 0.01f, finalZ / transform.lossyScale.z);
        } else {
            shape.scale = new Vector3(50f, 0.01f, 50f);
        }

        // ★ 核心渲染：几何金属颗粒 (Cube Rendering)
        psr.renderMode = ParticleSystemRenderMode.Mesh;
        GameObject tempCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        psr.mesh = tempCube.GetComponent<MeshFilter>().sharedMesh;
        DestroyImmediate(tempCube);
        psr.material = CreateMetallicMaterial(mainColor);

        ConfigureMode();
        ps.Play();
    }

    void HideTargetMesh()
    {
        // ★ 核心：绝对透明容器化
        if (baseMr != null) baseMr.enabled = false;
        
        // 递归隐藏所有子层级 Renderer (防止手掌模型或其他附件被渲染出来)
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
                vel.x = new ParticleSystem.MinMaxCurve(-0.06f, 0.06f);
                vel.z = new ParticleSystem.MinMaxCurve(-0.06f, 0.06f);
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
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = 3000;
        
        Color tColor = new Color(color.r, color.g, color.b, 0.55f);
        mat.SetColor("_BaseColor", tColor);
        mat.SetFloat("_Metallic", 0.93f);
        mat.SetFloat("_Smoothness", 0.97f);
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
