using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 【终极全能版】GroundParticleController:
/// 所有调节（高度、颜色、质感、运动、透明度）全部集成在此代码中。
/// </summary>
public class GroundParticleController : MonoBehaviour
{
    public enum ParticleStyle { MetallicGem, GlowingSphere, SoftTranslucent }
    public enum MovementMode { OceanWavy, CatBlinking, TreeRandom, BasicTwinkle }

    [Header("--- 1. Height & Area (高度与范围) ---")]
    [Range(-1f, 5f)] public float heightOffset = 0.05f;
    [Range(10f, 200f)] public float coverageArea = 80f;

    [Header("--- 2. Visual Style (视觉质感) ---")]
    public ParticleStyle visualStyle = ParticleStyle.MetallicGem;
    public Color mainColor = Color.white;
    public bool useMultiColor = false;
    [Tooltip("点击右下角的 + 号直接添加想要的单独颜色槽！最多同时支持 8 种颜色，粒子的生成会在这几个纯色里随机抽取。")]
    public Color[] multiColorSlots = new Color[] { new Color(0f, 0.8f, 1f), new Color(1f, 0.6f, 0.8f) };
    [Range(0.001f, 0.3f)] public float particleSize = 0.045f;
    [Range(5f, 3000f)] public float particleDensity = 400f;

    [Header("--- 3. Movement (动力模式) ---")]
    public MovementMode movementMode = MovementMode.BasicTwinkle;

    [Header("--- 4. Transparency (极致透明) ---")]
    public bool hideOriginalMesh = true;

    private ParticleSystem ps;
    private ParticleSystemRenderer psr;
    private MeshRenderer baseMr;
    private ParticleStyle lastStyle;
    private MovementMode lastMode;

    void Awake()
    {
        baseMr = GetComponent<MeshRenderer>();
        SetupParticleSystem();
    }

    void Update()
    {
        ApplyAdjustments();
        
        // 实时高度同步
        if (ps != null) {
            ps.transform.position = new Vector3(transform.position.x, heightOffset, transform.position.z);
            ps.transform.localScale = Vector3.one; 
        }

        // 实时透明同步
        if (hideOriginalMesh && baseMr != null) {
            baseMr.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            baseMr.enabled = false;
        }
    }

    void SetupParticleSystem()
    {
        GameObject psObj = new GameObject("Ultimate_GroundParticles");
        psObj.transform.SetParent(null); // 脱离父级缩放
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

        UpdateStyleAndMovement();
        ps.Play();
    }

    void UpdateStyleAndMovement()
    {
        if (psr == null) return;

        // 1. 设置视觉材质与 Mesh
        psr.material = CreateStyleMaterial(visualStyle, mainColor);
        if (visualStyle == ParticleStyle.MetallicGem) {
            psr.renderMode = ParticleSystemRenderMode.Mesh;
            // ★ 核心修复：用户要求不要正方体，改为球体 (Sphere)
            GameObject tempMesh = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            psr.mesh = tempMesh.GetComponent<MeshFilter>().sharedMesh;
            DestroyImmediate(tempMesh);
        } else {
            psr.renderMode = ParticleSystemRenderMode.Billboard;
        }

        // 2. 设置动力模式 (Noise/Velocity)
        var noise = ps.noise;
        var colorOL = ps.colorOverLifetime;
        var vel = ps.velocityOverLifetime;
        
        // 重置
        noise.enabled = false;
        colorOL.enabled = false;
        vel.enabled = false;

        switch (movementMode)
        {
            case MovementMode.BasicTwinkle:
                colorOL.enabled = true;
                colorOL.color = GetTwinkleGradient();
                break;
            case MovementMode.OceanWavy:
                noise.enabled = true;
                noise.strength = 0.15f; 
                noise.frequency = 0.25f;
                noise.scrollSpeed = 0.12f;
                break;
            case MovementMode.CatBlinking:
                colorOL.enabled = true;
                colorOL.color = GetBlinkingGradient();
                break;
            case MovementMode.TreeRandom:
                vel.enabled = true;
                vel.x = new ParticleSystem.MinMaxCurve(-0.06f, 0.06f);
                vel.z = new ParticleSystem.MinMaxCurve(-0.06f, 0.06f);
                vel.y = new ParticleSystem.MinMaxCurve(0f, 0f);
                noise.enabled = true;
                noise.strength = 0.02f; 
                break;
        }

        lastStyle = visualStyle;
        lastMode = movementMode;
    }

    void ApplyAdjustments()
    {
        if (ps == null) return;
        
        var main = ps.main;
        main.startSize = particleSize;
        
        if (useMultiColor && multiColorSlots != null && multiColorSlots.Length > 0)
        {
            int count = multiColorSlots.Length;
            Gradient grad = new Gradient();
            // 必须是 Fixed，保证粒子是实打实的纯色，不要渐变乱混色！
            grad.mode = GradientMode.Fixed;
            
            GradientColorKey[] colorKeys = new GradientColorKey[count];
            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[count];
            
            for (int i = 0; i < count; i++)
            {
                float t = (count == 1) ? 0f : ((float)i / (count - 1)); 
                colorKeys[i] = new GradientColorKey(multiColorSlots[i], t);
                alphaKeys[i] = new GradientAlphaKey(multiColorSlots[i].a, t);
            }
            grad.SetKeys(colorKeys, alphaKeys);

            var minMax = new ParticleSystem.MinMaxGradient(grad);
            minMax.mode = ParticleSystemGradientMode.RandomColor; 
            main.startColor = minMax;
        }
        else
        {
            main.startColor = mainColor;
        }

        var emission = ps.emission;
        emission.rateOverTime = particleDensity;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(coverageArea, 0.01f, coverageArea);

        if (visualStyle != lastStyle || movementMode != lastMode) {
            UpdateStyleAndMovement();
        }

        if (psr != null && psr.material != null && (!useMultiColor)) {
            float alpha = (visualStyle == ParticleStyle.SoftTranslucent) ? 0.3f : 0.6f;
            psr.material.SetColor("_BaseColor", new Color(mainColor.r, mainColor.g, mainColor.b, alpha));
            psr.material.SetColor("_Color", new Color(mainColor.r, mainColor.g, mainColor.b, alpha));
        }
    }

    private Material CreateStyleMaterial(ParticleStyle s, Color color)
    {
        // ★ 核心底层修复：默认的 Lit 不兼容粒子的自身定色，必须用 Particles专属 Shader 才能保证马卡龙色不被蓝底色吞噬！
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Lit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Standard");

        Material mat = new Material(shader);
        
        // ★ 核心修复：如果是 2D Billboard 模式，强制使用程序化圆形贴图，彻底消除正方形边缘
        mat.mainTexture = ParticleUtils.GetSoftCircleTexture();
        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", ParticleUtils.GetSoftCircleTexture());

        mat.SetFloat("_Surface", 1); 
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = 3000;
        
        float alpha = 0.6f;
        mat.SetColor("_BaseColor", new Color(color.r, color.g, color.b, alpha));

        switch (s)
        {
            case ParticleStyle.MetallicGem:
                mat.SetFloat("_Metallic", 0.95f);
                mat.SetFloat("_Smoothness", 0.98f);
                break;
            case ParticleStyle.GlowingSphere:
                mat.SetFloat("_Metallic", 0.1f);
                mat.SetFloat("_Smoothness", 0.5f);
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color * 1.5f);
                break;
            case ParticleStyle.SoftTranslucent:
                mat.SetColor("_BaseColor", new Color(color.r, color.g, color.b, 0.3f));
                break;
        }
        return mat;
    }

    private Gradient GetTwinkleGradient()
    {
        Gradient g = new Gradient();
        g.SetKeys(
            // ★ 把原来的红黄蓝渐变强制洗掉，改用纯白（让我们的多色槽去真正接管颜色）
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
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
