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
    [Tooltip("音频完全能听到的最近距离")]
    public float nearDistance = 1f;
    [Tooltip("音频完全听不到的最远距离")]
    public float farDistance = 15f;
    [Tooltip("衰减曲线指数 (1=线性, 2=二次, 3=更快衰减)")]
    public float falloffExponent = 2f;

    [Header("Fade Transition")]
    [Tooltip("淡入淡出过渡时间（秒）")]
    public float fadeDuration = 1.5f;

    [Header("References")]
    public AudioSource targetAudio;

    private float baseVolume = 1f;
    private float fadeMultiplier = 1f;
    private float fadeTarget = 1f;
    private Transform listener;

    /// <summary>
    /// 快捷设置方法：在代码中创建 AudioSource 后调用此方法自动挂载衰减
    /// </summary>
    public static AudioDistanceFader Setup(AudioSource source, float maxDist = 15f, float fadeSec = 1.5f)
    {
        if (source == null) return null;
        AudioDistanceFader fader = source.gameObject.AddComponent<AudioDistanceFader>();
        fader.targetAudio = source;
        fader.farDistance = maxDist;
        fader.fadeDuration = fadeSec;
        fader.baseVolume = source.volume;
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

        // 找到听音者（VR摄像机）
        if (listener == null)
        {
            Camera cam = Camera.main;
            if (cam == null) cam = FindObjectOfType<Camera>();
            if (cam != null) listener = cam.transform;
        }

        // 计算距离衰减
        float distanceFade = 1f;
        if (listener != null)
        {
            float dist = Vector3.Distance(transform.position, listener.position);
            float t = Mathf.InverseLerp(nearDistance, farDistance, dist);
            distanceFade = Mathf.Pow(1f - Mathf.Clamp01(t), falloffExponent);
        }

        // 平滑淡入淡出过渡
        fadeMultiplier = Mathf.MoveTowards(fadeMultiplier, fadeTarget, Time.deltaTime / Mathf.Max(0.01f, fadeDuration));

        // 最终音量 = 基础音量 × 距离衰减 × 淡入淡出
        targetAudio.volume = baseVolume * distanceFade * fadeMultiplier;
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
