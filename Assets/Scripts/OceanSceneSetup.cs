using UnityEngine;

/// <summary>
/// OceanSceneSetup — 只负责海洋氛围设置（深蓝环境 + 雾 + 音频）。
/// 不创建任何模型/气泡！用户已有自己的海洋模型。
///
/// 用法：挂到场景中已有的 OceanManager 或 BreathManager 物体上。
/// </summary>
[RequireComponent(typeof(BreathInputManager))]
public class OceanSceneSetup : MonoBehaviour
{
    [Header("Breath")]
    public BreathInputManager breathInput;

    [Header("Fog (breath-driven)")]
    public bool enableFog = true;
    [Range(0f, 0.1f)] public float minFog = 0.003f;
    [Range(0f, 0.1f)] public float maxFog  = 0.04f;

    [Header("Deep Blue Environment")]
    public Color deepBlueColor = new Color(0.0f, 0.04f, 0.12f, 1f);
    public Color fogColor = new Color(0.0f, 0.06f, 0.18f, 1f);

    [Header("Audio — 直接拖音频文件即可")]
    [Tooltip("海洋环境音（循环）")]
    public AudioClip oceanClip;
    [Range(0f, 1f)] public float minVolume = 0.15f;
    [Range(0f, 1f)] public float maxVolume = 1.0f;

    private AudioSource oceanAudio;

    void Awake()
    {
        if (breathInput == null) breathInput = GetComponent<BreathInputManager>();
    }

    void Start()
    {
        SetupAtmosphere();
        SetupAudio();
        MakePlaneDeepBlue();
        SlowDownBackgroundParticles();
    }

    void SetupAtmosphere()
    {
        // 深蓝色环境
        if (enableFog)
        {
            RenderSettings.fog      = true;
            RenderSettings.fogMode  = FogMode.ExponentialSquared;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogDensity = minFog;
        }

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.02f, 0.06f, 0.15f);
        RenderSettings.ambientIntensity = 0.3f;
        RenderSettings.skybox = null;

        Camera cam = Camera.main;
        if (cam == null) cam = FindObjectOfType<Camera>();
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = deepBlueColor;
        }

        // 关闭/调弱方向光
        Light[] lights = FindObjectsOfType<Light>();
        foreach (var l in lights)
        {
            if (l.type == LightType.Directional)
            {
                l.intensity = 0.1f;
            }
        }
    }

    void SetupAudio()
    {
        if (oceanClip != null)
        {
            oceanAudio = gameObject.AddComponent<AudioSource>();
            oceanAudio.clip = oceanClip;
            oceanAudio.spatialBlend = 0f;
            oceanAudio.loop = true;
            oceanAudio.Play();
            AudioDistanceFader.Setup(oceanAudio, 6f, 1f);
        }
    }

    /// <summary>
    /// 把已有的 Plane 设为深蓝色（移除白色地面）
    /// </summary>
    void MakePlaneDeepBlue()
    {
        foreach (var mr in FindObjectsOfType<MeshRenderer>())
        {
            if (mr.gameObject.name.Contains("Plane"))
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (mat == null) mat = new Material(Shader.Find("Standard"));

                Color planeColor = new Color(0.0f, 0.03f, 0.08f, 0.8f);
                mat.SetFloat("_Surface", 1f); 
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.renderQueue = 3000;
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.SetColor("_BaseColor", planeColor);
                mat.color = planeColor;

                mr.material = mat;
                mr.enabled = true; 
            }
        }
    }

    /// <summary>
    /// 海洋背景粒子降速一倍，营造静谧感
    /// </summary>
    void SlowDownBackgroundParticles()
    {
        ParticleSystem[] allPS = FindObjectsOfType<ParticleSystem>();
        foreach (var ps in allPS)
        {
            var main = ps.main;
            main.simulationSpeed = main.simulationSpeed * 0.5f;
        }
    }

    void Update()
    {
        if (breathInput == null) return;
        float b = breathInput.BreathValue;

        if (enableFog)
            RenderSettings.fogDensity = Mathf.Lerp(minFog, maxFog, b);

        if (oceanAudio != null)
            oceanAudio.volume = Mathf.Lerp(minVolume, maxVolume, b);
    }
}
