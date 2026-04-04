using UnityEngine;

/// <summary>
/// 通用音频距离衰减器：
/// - 根据玩家（Camera）与音源的距离，自动调节 volume
/// - 淡出过渡平滑（不突然静音）
/// - 当音源被禁用或场景切走时，音频平滑淡出
///
/// 用法：挂到任何有 AudioSource 的物体上，或在代码中调用
///   AudioDistanceFader.Setup(audioSource, maxDist, fadeDuration)
/// </summary>
public class AudioDistanceFader : MonoBehaviour
{
    [Header("Distance Fade")]
    public float nearDistance = 0.5f;
    public float farDistance = 4f;
    [Tooltip("1.5 = 较慢衰减，走出几步依然能听到余音")]
    public float falloffExponent = 1.5f; 

    [Header("Silence Threshold")]
    [Tooltip("当音量低于此值时，彻底停止播放以节省性能并保证安静")]
    public float stopThreshold = 0.02f;

    [Header("Fade Transition")]
    public float fadeDuration = 1.0f;

    [Header("References")]
    public AudioSource targetAudio;

    private float baseVolume = 1f;
    private float fadeMultiplier = 1f;
    private float fadeTarget = 1f;
    private Transform listener;
    private bool setupWasCalled = false; // Setup() 已经设好 baseVolume，Start() 不得覆盖

    public static AudioDistanceFader Setup(AudioSource source, float maxDist = 10f, float nearDist = 0.5f, float exponent = 1.5f, float fadeSec = 1.0f, float spatial = -1f)
    {
        if (source == null) return null;
        AudioDistanceFader fader = source.GetComponent<AudioDistanceFader>() ?? source.gameObject.AddComponent<AudioDistanceFader>();
        fader.targetAudio = source;
        fader.farDistance = maxDist;
        fader.nearDistance = nearDist;
        fader.falloffExponent = exponent;
        fader.fadeDuration = fadeSec;
        fader.baseVolume = source.volume;
        fader.setupWasCalled = true; // 防止 Start() 覆盖 baseVolume
        if (spatial >= 0f) source.spatialBlend = spatial;
        return fader;
    }

    void Start()
    {
        if (targetAudio == null) targetAudio = GetComponent<AudioSource>();
        // ★ 只有当 Setup() 没被调用过时才从 AudioSource 读取 baseVolume
        // 否则 Setup→SetSilentInstant 之后 volume 已经是 0，这里会把正确的 baseVolume 覆盖为 0
        if (targetAudio != null && !setupWasCalled) baseVolume = targetAudio.volume;
    }

    void Update()
    {
        if (targetAudio == null) return;

        // 实时追踪 VR 头显（场景切换后旧 Camera 被销毁，需重新查找）
        if (listener == null || !listener.gameObject.activeInHierarchy)
        {
            listener = null;
            Camera cam = Camera.main;
            if (cam == null) cam = GameObject.Find("CenterEyeAnchor")?.GetComponent<Camera>(); // Oculus-specific
            if (cam == null) cam = FindObjectOfType<Camera>();
            if (cam != null) listener = cam.transform;
            
            // 如果还找不到，可能是 VR 头显还没初始化
            // 此时保持 distanceFade 为 1，防止冷启动时声音全无
        }

        float distanceFade = 1f; // 默认 1，防止找不到 Listener 时静音
        if (listener != null)
        {
            float dist = Vector3.Distance(transform.position, listener.position);
            // 核心逻辑：如果在范围内，根据指数衰减；如果超出，直接 0
            if (dist < farDistance)
            {
                float t = Mathf.InverseLerp(nearDistance, farDistance, dist);
                distanceFade = Mathf.Pow(1f - t, falloffExponent);
            }
            else
            {
                distanceFade = 0f;
            }
        }

        fadeMultiplier = Mathf.MoveTowards(fadeMultiplier, fadeTarget, Time.deltaTime / Mathf.Max(0.01f, fadeDuration));

        float finalVol = baseVolume * distanceFade * fadeMultiplier;
        if (targetAudio != null)
        {
            targetAudio.volume = finalVol;
        }
    }

    /// <summary>
    /// 触发平滑淡出（例如玩家离开区域时调用）
    /// </summary>
    public void FadeOut()
    {
        fadeTarget = 0f;
    }

    /// <summary>
    /// 触发平滑淡入
    /// </summary>
    public void FadeIn()
    {
        fadeTarget = 1f;
    }

    /// <summary>
    /// 设置基础音量（不受距离影响的最大音量）
    /// </summary>
    public void SetBaseVolume(float vol)
    {
        baseVolume = vol;
    }

    /// <summary>
    /// 立即静音（不过渡），并保持 fadeTarget = 0，等待外部调用 FadeIn() 再响起。
    /// 用于初始化时确保不发出任何声音。
    /// </summary>
    public void SetSilentInstant()
    {
        fadeMultiplier = 0f;
        fadeTarget = 0f;
        if (targetAudio != null) targetAudio.volume = 0f;
    }
}
