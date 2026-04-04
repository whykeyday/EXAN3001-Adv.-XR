using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// GroundParticleController (God-Mode Tuner Version): 
/// 1. 提供实时 Height Offset 高度调节。
/// 2. 提供 Style 预设（金属宝石、微光圆点、柔和虚化）。
/// 3. 提供自定义 Mesh 接口。
/// 4. 强制 ShadowsOnly 实现 100% 地面透明化。
/// </summary>
public class GroundParticleController : MonoBehaviour
{
    public enum ParticleStyle { MetallicGem, GlowingSphere, SoftTranslucent }

    [Header("--- 1. Height Alignment (高度校准) ---")]
    [Tooltip("手动上下调节粒子高度，直到贴合脚底。建议范围 0.01 - 2.0")]
    public float heightOffset = 0.05f;

    [Header("--- 2. Visual Style (视觉风格) ---")]
    [Tooltip("切换不同质感的粒子预设")]
    public ParticleStyle style = ParticleStyle.MetallicGem;
    
    [Header("--- 3. Manual Adjustments (手动微调) ---")]
    public Color mainColor = Color.white;
    
    [Range(0.001f, 0.3f)]
    public float particleSize = 0.045f;

    [Range(5f, 3000f)]
    public float particleDensity = 400f;

    [Tooltip("自定义粒子形状（如菱形、十字架等），留空则使用默认形状")]
    public Mesh customMesh;

    private ParticleSystem ps;
    private ParticleSystemRenderer psr;
    private MeshRenderer baseMr;
    private ParticleStyle lastStyle;

    void Awake()
    {
        baseMr = GetComponent<MeshRenderer>();
        SetupParticleSystem();
        HideBaseMesh();
    }

    void Update()
    {
        ApplyAdjustments();
        
        // ★ 核心修复：上帝视角高度实时映射
        if (ps != null) {
            // 将控制器上的 heightOffset 实时同步到独立的世界坐标粒子系统中
            ps.transform.position = new Vector3(transform.position.x, heightOffset, transform.position.z);
            ps.transform.localScale = Vector3.one; 
        }
    }

    void SetupParticleSystem()
    {
        GameObject psObj = new GameObject("GodMode_GroundParticles");
        psObj.transform.SetParent(null); // 脱离缩放影响
        psObj.transform.position = new Vector3(transform.position.x, heightOffset, transform.position.z);

        ps = psObj.AddComponent<ParticleSystem>();
        psr = psObj.GetComponent<ParticleSystemRenderer>();

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 6f);
        main.startSpeed = 0f;
        main.maxParticles = 12000;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(80f, 0.01f, 80f); 

        UpdateStyleSettings();
        ps.Play();
    }

    void HideBaseMesh()
    {
        // ★ 核心：绝对透明化方案
        if (baseMr != null) {
            // 通过 ShadowsOnly 剔除视觉。模型依然有碰撞，但绝对不可见。
            baseMr.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            baseMr.receiveShadows = false;
            baseMr.enabled = false; 
        }
        
        Renderer[] allRs = GetComponentsInChildren<Renderer>(true);
        foreach (var r in allRs) {
            if (r != psr) r.enabled = false;
        }
    }

    void UpdateStyleSettings()
    {
        if (psr == null) return;

        psr.material = CreateStyleMaterial(style, mainColor);
        
        // 渲染网格切换
        if (customMesh != null) {
            psr.renderMode = ParticleSystemRenderMode.Mesh;
            psr.mesh = customMesh;
        } else {
            if (style == ParticleStyle.MetallicGem) {
                psr.renderMode = ParticleSystemRenderMode.Mesh;
                GameObject tempCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                psr.mesh = tempCube.GetComponent<MeshFilter>().sharedMesh;
                DestroyImmediate(tempCube);
            } else {
                // 圆球通常使用 Billboard 模式或 Sphere Mesh
                psr.renderMode = ParticleSystemRenderMode.Billboard;
            }
        }
        lastStyle = style;
    }

    void ApplyAdjustments()
    {
        if (ps == null) return;
        
        var main = ps.main;
        main.startSize = particleSize;
        main.startColor = mainColor;

        var emission = ps.emission;
        emission.rateOverTime = particleDensity;

        // 检测风格变化并应用
        if (style != lastStyle) {
            UpdateStyleSettings();
        }

        // 实时更新材质颜色（如果你在 Inspector 实时调色）
        if (psr != null && psr.material != null) {
            float alpha = (style == ParticleStyle.SoftTranslucent) ? 0.3f : 0.6f;
            psr.material.SetColor("_BaseColor", new Color(mainColor.r, mainColor.g, mainColor.b, alpha));
            psr.material.SetColor("_Color", new Color(mainColor.r, mainColor.g, mainColor.b, alpha));
        }
    }

    private Material CreateStyleMaterial(ParticleStyle s, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Standard");

        Material mat = new Material(shader);
        mat.SetFloat("_Surface", 1); 
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = 3000;
        
        float alpha = 0.6f;
        mat.SetColor("_BaseColor", new Color(color.r, color.g, color.b, alpha));

        switch (s)
        {
            case ParticleStyle.MetallicGem:
                // 复刻珊瑚质感：高反射、极致平滑
                mat.SetFloat("_Metallic", 0.95f);
                mat.SetFloat("_Smoothness", 0.98f);
                break;
            case ParticleStyle.GlowingSphere:
                // 微光质感
                mat.SetFloat("_Metallic", 0.1f);
                mat.SetFloat("_Smoothness", 0.5f);
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color * 1.5f);
                break;
            case ParticleStyle.SoftTranslucent:
                // 柔和半透明
                mat.SetFloat("_Metallic", 0f);
                mat.SetFloat("_Smoothness", 0.1f);
                mat.SetColor("_BaseColor", new Color(color.r, color.g, color.b, 0.3f));
                break;
        }
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
