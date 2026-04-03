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
    [Tooltip("海鸥叫声")]
    public AudioClip seagullClip;
    [Tooltip("水泡声")]
    public AudioClip bubbleClip;
    [Range(0f, 1f)] public float minVolume = 0.15f;
    [Range(0f, 1f)] public float maxVolume = 1.0f;

    private AudioSource oceanAudio;
    private AudioSource seagullAudio;
    private AudioSource bubbleAudio;

    void Awake()
    {
        if (breathInput == null) breathInput = GetComponent<BreathInputManager>();
    }

    void Start()
    {
        SetupAtmosphere();
        SetupAudio();
        MakePlaneDeepBlue();
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
            AudioDistanceFader.Setup(oceanAudio, 25f, 2f);
        }
        if (seagullClip != null)
        {
            seagullAudio = gameObject.AddComponent<AudioSource>();
            seagullAudio.clip = seagullClip;
            seagullAudio.spatialBlend = 0f;
            seagullAudio.playOnAwake = false;
            AudioDistanceFader.Setup(seagullAudio, 15f, 1.5f);
            StartCoroutine(RandomSeagullRoutine());
        }
        if (bubbleClip != null)
        {
            bubbleAudio = gameObject.AddComponent<AudioSource>();
            bubbleAudio.clip = bubbleClip;
            bubbleAudio.spatialBlend = 0f;
            bubbleAudio.playOnAwake = false;
            AudioDistanceFader.Setup(bubbleAudio, 12f, 1f);
            StartCoroutine(RandomBubbleRoutine());
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

    private System.Collections.IEnumerator RandomSeagullRoutine()
    {
        if (seagullAudio != null && !seagullAudio.isPlaying) seagullAudio.Play();

        while (true)
        {
            float waitTime = Random.Range(12f, 25f);
            yield return new WaitForSeconds(waitTime);
            
            if (seagullAudio != null)
            {
                seagullAudio.pitch = Random.Range(0.9f, 1.1f);
                seagullAudio.Play();
            }
        }
    }

    private System.Collections.IEnumerator RandomBubbleRoutine()
    {
        yield return new WaitForSeconds(Random.Range(3f, 8f));

        while (true)
        {
            if (bubbleAudio != null)
            {
                bubbleAudio.pitch = Random.Range(0.85f, 1.15f);
                bubbleAudio.volume = Random.Range(0.3f, 0.7f);
                bubbleAudio.Play();
            }

            float waitTime = Random.Range(8f, 20f);
            yield return new WaitForSeconds(waitTime);
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
