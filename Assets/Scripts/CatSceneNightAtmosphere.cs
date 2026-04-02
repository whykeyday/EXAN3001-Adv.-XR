using UnityEngine;

public class CatSceneNightAtmosphere : MonoBehaviour
{
    [Header("Night Settings — 深蓝色而非纯黑")]
    public Color nightFogColor = new Color(0.0f, 0.03f, 0.1f);  // 深蓝而非纯黑
    public Color skyColor = new Color(0.0f, 0.02f, 0.08f);      // 天空也是深蓝
    public float fogDensity = 0.035f;
    public float nightAmbientIntensity = 0.15f; // 保留一点微弱环境光

    [Header("Twinkling Stars")]
    public int starCount = 300;
    public Vector3 starBoxSize = new Vector3(25f, 8f, 25f);
    public float starHeightOffset = 4f;
    public float starSizeMin = 0.03f;
    public float starSizeMax = 0.25f;

    private ParticleSystem starParticles;

    void Start()
    {
        RenderSettings.fog = true;
        RenderSettings.fogColor = nightFogColor;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = fogDensity;

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.02f, 0.03f, 0.08f); // 极微弱的深蓝环境光
        RenderSettings.ambientIntensity = nightAmbientIntensity;
        
        RenderSettings.skybox = null; 
        
        // 只削弱方向光，不完全关闭
        Light[] lights = FindObjectsOfType<Light>();
        foreach(var l in lights)
        {
            if (l.type == LightType.Directional) {
                l.intensity = 0.05f; // 极微弱，但不是0
                l.color = new Color(0.3f, 0.4f, 0.7f); // 月光蓝色调
            }
        }

        // 摄像机背景为深蓝色
        Camera cam = Camera.main;
        if (cam == null) cam = FindObjectOfType<Camera>();
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = skyColor;
        }

        // 创建天空闪烁呼吸星星
        CreateTwinklingStars();
    }

    void CreateTwinklingStars()
    {
        GameObject starObj = new GameObject("TwinklingStars");
        starObj.transform.SetParent(transform);
        starObj.transform.localPosition = Vector3.up * starHeightOffset;

        starParticles = starObj.AddComponent<ParticleSystem>();
        starParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = starParticles.main;
        main.loop = true;
        main.prewarm = true;
        main.duration = 60f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(15f, 40f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(starSizeMin, starSizeMax);
        main.maxParticles = starCount;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;

        // 白色/微蓝色星星，HDR 增强确保可见
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.7f, 0.8f, 1f, 0.6f),  
            new Color(1f, 1f, 1f, 1f) * 3.0f   // HDR 高亮
        );

        var emission = starParticles.emission;
        emission.rateOverTime = starCount / 5f; // 更快填充

        var shape = starParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = starBoxSize;

        // 静止
        var vel = starParticles.velocityOverLifetime;
        vel.enabled = false;

        // 闪烁呼吸：Alpha 反复上下交替
        var colorOL = starParticles.colorOverLifetime;
        colorOL.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(Color.white, 0f), 
                new GradientColorKey(new Color(0.8f, 0.9f, 1f), 0.5f),
                new GradientColorKey(Color.white, 1f) 
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.9f, 0.1f),
                new GradientAlphaKey(0.2f, 0.3f),
                new GradientAlphaKey(0.8f, 0.5f),
                new GradientAlphaKey(0.15f, 0.7f),
                new GradientAlphaKey(0.7f, 0.85f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOL.color = grad;

        var noise = starParticles.noise;
        noise.enabled = false;

        // 使用高亮发光材质
        var psr = starParticles.GetComponent<ParticleSystemRenderer>();
        psr.renderMode = ParticleSystemRenderMode.Billboard;
        psr.material = CreateStarMaterial();

        starParticles.Play();
    }

    Material CreateStarMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) shader = Shader.Find("Legacy Shaders/Particles/Additive");

        if (shader != null)
        {
            Material mat = new Material(shader);
            mat.name = "TwinklingStar";
            
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white * 2f);
            if (mat.HasProperty("_EmissionColor")) 
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", Color.white * 5.0f); // 非常亮
            }

            // 使用 Additive 混合模式，让星星更亮
            mat.SetFloat("_Surface", 1.0f); 
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_ZWrite", 0);
            
            Texture defaultTex = Resources.GetBuiltinResource<Texture2D>("Default-Particle.psd");
            if (defaultTex != null)
            {
                mat.mainTexture = defaultTex;
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", defaultTex);
            }
            return mat;
        }
        return ParticleUtils.GetGlowingSphereMaterial();
    }
}
