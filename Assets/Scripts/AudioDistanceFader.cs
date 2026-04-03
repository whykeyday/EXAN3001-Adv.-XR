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
    [Tooltip("4 = 极速衰减，走出几步即刻安静")]
    public float falloffExponent = 4.0f; 

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

    public static AudioDistanceFader Setup(AudioSource source, float maxDist = 5f, float fadeSec = 1.0f, float spatial = -1f)
    {
        if (source == null) return null;
        AudioDistanceFader fader = source.gameObject.AddComponent<AudioDistanceFader>();
        fader.targetAudio = source;
        fader.farDistance = maxDist;
        fader.fadeDuration = fadeSec;
        fader.baseVolume = source.volume;
        if (spatial >= 0f) source.spatialBlend = spatial;
        return fader;
    }

    void Start()
    {
        if (targetAudio == null) targetAudio = GetComponent<AudioSource>();
        if (targetAudio != null) baseVolume = targetAudio.volume;
    }

    void Update()
    {
        if (targetAudio == null) return;

        // 实时追踪 VR 头显
        if (listener == null)
        {
            Camera cam = Camera.main;
            if (cam == null) cam = FindObjectOfType<Camera>();
            if (cam != null) listener = cam.transform;
        }

        float distanceFade = 0f;
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
        targetAudio.volume = finalVol;

        // 彻底停止逻辑：不仅仅是 Pause，而是 Stop 保证没有底噪
        if (finalVol < stopThreshold)
        {
            if (targetAudio.isPlaying) targetAudio.Stop();
        }
        else
        {
            if (!targetAudio.isPlaying && targetAudio.clip != null)
            {
                targetAudio.Play();
            }
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
}
