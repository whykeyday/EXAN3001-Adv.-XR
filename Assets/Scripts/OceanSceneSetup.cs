using UnityEngine;

/// <summary>
/// OceanSceneSetup — 只负责海洋氛围设置（深蓝环境 + 雾 + 音频）。
/// 不创建任何模型/气泡！用户已有自己的海洋模型。
///
/// 用法：挂到场景中的 BreathManager 物体上。
/// </summary>
[RequireComponent(typeof(BreathInputManager))]
public class OceanSceneSetup : MonoBehaviour
{
    [Header("1. Breath & Fog Settings")]
    public BreathInputManager breathInput;

    [Header("Fog (breath-driven)")]
    public bool enableFog = true;
    [Range(0f, 0.1f)] public float minFog = 0.003f;
    [Range(0f, 0.1f)] public float maxFog  = 0.04f;

    [Header("Deep Blue Environment")]
    public Color deepBlueColor = new Color(0.0f, 0.04f, 0.12f, 1f);
    public Color fogColor = new Color(0.0f, 0.06f, 0.18f, 1f);

    [Header("2. Loop Audio (Deep Sea Waves)")]
    [Tooltip("海洋环境音（循环）")]
    public AudioClip oceanClip;
    [Range(0f, 1f)] public float minVolume = 0.15f;
    [Range(0f, 1f)] public float maxVolume = 1.0f;
    
    [Header("3. Ocean Audio — 海鸥/水泡 (全局随机环境音)")]
    [Tooltip("海鸥叫声（随机选 1-4 个）")]
    public AudioClip[] seagullClips;
    [Range(0f, 1f)] public float seagullVolume = 0.6f;
    [Tooltip("海鸥随机响起的间隔 (秒)")]
    public float minSeagullInterval = 4f;
    public float maxSeagullInterval = 7f;
    
    [Space(5)]
    [Tooltip("气泡冒泡声")]
    public AudioClip bubbleClip;
    [Range(0f, 1f)] public float bubbleVolume = 0.4f;
    [Tooltip("气泡随机响起的间隔 (秒)")]
    public float minBubbleInterval = 4f;
    public float maxBubbleInterval = 7f;

    [Header("Random Sounds Distance Fade")]
    public float ambientNearDistance = 0.5f;
    public float ambientFarDistance = 15f;
    public float ambientFalloff = 1.5f;

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

        // 启动随机音效
        StartCoroutine(SeagullLoop());
        StartCoroutine(BubbleLoop());
    }

    private System.Collections.IEnumerator SeagullLoop()
    {
        while (true)
        {
            float wait = Random.Range(minSeagullInterval, maxSeagullInterval);
            yield return new WaitForSeconds(wait);

            if (seagullClips != null && seagullClips.Length > 0)
            {
                AudioClip clip = seagullClips[Random.Range(0, seagullClips.Length)];
                if (clip != null)
                {
                    // Create a temporary emitter to allow Distance Fade
                    GameObject emitter = new GameObject("SeagullEmitter_Temp");
                    emitter.transform.position = Camera.main.transform.position + Random.onUnitSphere * 5f;
                    AudioSource src = emitter.AddComponent<AudioSource>();
                    src.clip = clip;
                    src.volume = seagullVolume;
                    src.spatialBlend = 1f;
                    src.Play();
                    AudioDistanceFader.Setup(src, ambientFarDistance, ambientNearDistance, ambientFalloff);
                    
                    // Auto-destroy after clip ends
                    Destroy(emitter, clip.length + 1f);
                    Debug.Log($"[OceanSound] Played Seagull: {clip.name} with Volume {seagullVolume}");
                }
            }
        }
    }

    private System.Collections.IEnumerator BubbleLoop()
    {
        while (true)
        {
            float wait = Random.Range(minBubbleInterval, maxBubbleInterval);
            yield return new WaitForSeconds(wait);

            if (bubbleClip != null)
            {
                GameObject emitter = new GameObject("BubbleEmitter_Temp");
                emitter.transform.position = Camera.main.transform.position + Random.insideUnitSphere * 2f;
                AudioSource src = emitter.AddComponent<AudioSource>();
                src.clip = bubbleClip;
                src.volume = bubbleVolume;
                src.spatialBlend = 1f;
                src.Play();
                AudioDistanceFader.Setup(src, ambientFarDistance, ambientNearDistance, ambientFalloff);

                Destroy(emitter, bubbleClip.length + 1f);
                Debug.Log($"[OceanSound] Played Bubble: {bubbleClip.name} with Volume {bubbleVolume}");
            }
        }
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

                Color planeColor = new Color(0.0f, 0.03f, 0.08f, 0.35f);
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
