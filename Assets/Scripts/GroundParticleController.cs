using UnityEngine;

/// <summary>
/// GroundParticleController (Precision Fix): 
/// 1. 使用 Mesh 发射模式替代 Box，实现 100% 精准覆盖。
/// 2. 彻底禁用 MeshRenderer 实现绝对透明地面。
/// 3. 缩短寿命并锁定高度，防止粒子飞得太高。
/// </summary>
public class GroundParticleController : MonoBehaviour
{
    public enum MovementMode { BasicTwinkle, OceanWavy, CatBlinking, TreeRandom }

    [Header("--- Manual Adjustments ---")]
    public Color mainColor = Color.white;
    
    [Range(0.001f, 0.2f)]
    public float particleSize = 0.04f;

    [Range(5f, 500f)]
    public float particleDensity = 80f;

    public MovementMode mode = MovementMode.BasicTwinkle;

    private ParticleSystem ps;
    private ParticleSystemRenderer psr;

    void Awake()
    {
        SetupParticleSystem();
        HideBaseMesh();
    }

    void Update()
    {
        ApplyAdjustments();
    }

    void SetupParticleSystem()
    {
        // 1. 获取地面的 MeshFilter (用于精准吸附)
        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) {
            Debug.LogError("GroundParticleController: 物体上未找到 MeshFilter，无法进行高精度吸附发射。");
            return;
        }

        GameObject psObj = new GameObject("GroundParticles_Container");
        psObj.transform.SetParent(transform, false);
        // 稍微往上抬一点点，防止埋在地板里
        psObj.transform.localPosition = new Vector3(0, 0.02f, 0);
        psObj.transform.localRotation = Quaternion.identity;
        psObj.transform.localScale = Vector3.one;

        ps = psObj.AddComponent<ParticleSystem>();
        psr = psObj.GetComponent<ParticleSystemRenderer>();

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local; // 使用本地空间，防止移动时残留
        
        // ★ 核心修复 1：寿命减半，防止飞得太高。1.5秒左右是最佳“贴地”感
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.0f, 1.8f);
        main.startSpeed = 0f;
        main.gravityModifier = 0f;
        main.maxParticles = 4000;

        // ★ 核心修复 2：网格发射模式 (Mesh Shape)
        // 这将百分之百精准对齐你的 Plane 形状，不管它有多大。
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Mesh;
        shape.mesh = mf.sharedMesh;
        shape.scale = Vector3.one;
        shape.position = Vector3.zero;
        shape.rotation = Vector3.zero;

        // ★ 材质与渲染
        psr.material = GetParticleMaterial();
        psr.renderMode = ParticleSystemRenderMode.Billboard;

        ConfigureMode();
        ps.Play();
    }

    void HideBaseMesh()
    {
        // ★ 核心修复 3：直接彻底禁用地面的显示
        // 这样不仅透明，而且毫无渲染压力。Collider 依然会生效。
        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr != null) mr.enabled = false;
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
                // 海洋粒子：保持低频微动
                noise.enabled = true;
                noise.strength = 0.25f; // 降低强度，防止飘离
                noise.frequency = 0.5f;
                noise.scrollSpeed = 0.15f;
                break;

            case MovementMode.CatBlinking:
                colorOL.enabled = true;
                colorOL.color = GetBlinkingGradient();
                break;

            case MovementMode.TreeRandom:
                // 森林粒子：由于是网格吸附，现在的移动将更自然
                var vel = ps.velocityOverLifetime;
                vel.enabled = true;
                vel.x = new ParticleSystem.MinMaxCurve(-0.1f, 0.1f);
                vel.z = new ParticleSystem.MinMaxCurve(-0.1f, 0.1f);
                vel.y = new ParticleSystem.MinMaxCurve(0f, 0f); // 强制垂直速度为 0
                noise.enabled = true;
                noise.strength = 0.04f; 
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
    }

    private Material GetParticleMaterial()
    {
        Shader s = Shader.Find("Universal Render Pipeline/Particles/Unlit");
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
            new GradientAlphaKey[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.3f), new GradientAlphaKey(1, 0.7f), new GradientAlphaKey(0, 1) }
        );
        return g;
    }
}
