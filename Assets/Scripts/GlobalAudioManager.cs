using UnityEngine;

/// <summary>
/// 全局音效管理器：贯穿所有场景随机循环播放背景音乐。
/// 挂载此组件的物体会自动跨场景保留 (DontDestroyOnLoad)。
/// 进入 BasicScene 后开始播放，切换任何场景都不会中断。
/// </summary>
public class GlobalAudioManager : MonoBehaviour
{
    public static GlobalAudioManager Instance { get; private set; }

    [Header("BGM Tracks (拖入 global1/2/3/4 音频)")]
    public AudioClip[] meditationTracks;

    [Header("Volume")]
    [Range(0f, 1f)]
    public float bgmVolume = 0.4f;

    private AudioSource audioSource;
    private float trackStartTime;
    private int lastPlayedIndex = -1;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        transform.SetParent(null); // DontDestroyOnLoad 只对根级物体有效，强制脱离父节点
        DontDestroyOnLoad(gameObject);

        // 检查是否已经有 AudioSource（防止重复添加）
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.loop = false;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // Force 2D
        audioSource.volume = bgmVolume;

        // 确保没有任何距离衰减脚本干扰全局 BGM
        AudioDistanceFader oldFader = GetComponent<AudioDistanceFader>();
        if (oldFader != null) Destroy(oldFader);
    }

    void Start()
    {
        Debug.Log("[GlobalAudioManager] Start. Volume: " + bgmVolume);
        if (meditationTracks != null && meditationTracks.Length > 0)
        {
            if (!audioSource.isPlaying)
                PlayRandomTrack();
        }
        else
        {
            Debug.LogError("[GlobalAudioManager] No tracks assigned in the Inspector!");
        }
    }

    void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        // 场景切换后确保音频继续播放（Unity有时会在切场景时暂停AudioSource）
        if (audioSource != null && meditationTracks != null && meditationTracks.Length > 0)
        {
            if (!audioSource.isPlaying)
            {
                audioSource.UnPause();
                if (!audioSource.isPlaying)
                    PlayRandomTrack();
            }
        }
    }

    void Update()
    {
        // 音量实时同步 Inspector 调节
        if (audioSource != null)
            audioSource.volume = bgmVolume;

        // 当前曲目播完，自动播下一首曲目
        if (audioSource != null && meditationTracks != null && meditationTracks.Length > 0)
        {
            // 如果没在播放，说明上一首已经放完（或由于某些原因停止）
            if (!audioSource.isPlaying)
            {
                PlayNextTrack();
            }
        }
    }

    void PlayNextTrack()
    {
        if (meditationTracks == null || meditationTracks.Length == 0) return;
        
        // 顺序循环
        int nextIndex = (lastPlayedIndex + 1) % meditationTracks.Length;
        PlayTrack(nextIndex);
    }

    void PlayTrack(int index)
    {
        if (index < 0 || index >= meditationTracks.Length || meditationTracks[index] == null) return;

        lastPlayedIndex = index;
        audioSource.clip = meditationTracks[index];
        audioSource.loop = false; // 由 Update 链式驱动
        audioSource.Play();
        trackStartTime = Time.time;
        Debug.Log($"[GlobalAudioManager] Now playing track {index}: {meditationTracks[index].name}");
    }

    public void ForcePlay()
    {
        Debug.Log($"[GlobalAudioManager] ForcePlay called. Track count: {meditationTracks?.Length ?? 0}");
        if (meditationTracks != null && meditationTracks.Length > 0)
        {
            PlayNextTrack();
        }
    }

    void PlayRandomTrack()
    {
        if (meditationTracks == null || meditationTracks.Length == 0) return;

        // 随机选一首（避免连续重复）
        int index;
        if (meditationTracks.Length == 1)
        {
            index = 0;
        }
        else
        {
            do { index = Random.Range(0, meditationTracks.Length); }
            while (index == lastPlayedIndex);
        }

        // 跳过空 clip
        if (meditationTracks[index] == null)
        {
            Debug.LogWarning($"[GlobalAudioManager] meditationTracks[{index}] is null, skipping.");
            return;
        }

        lastPlayedIndex = index;
        audioSource.clip = meditationTracks[index];
        audioSource.volume = bgmVolume;
        audioSource.Play();
        trackStartTime = Time.time;

        Debug.Log($"[GlobalAudioManager] Now playing track {index}: {meditationTracks[index].name}");
    }

    /// <summary>
    /// 外部调用：设置 BGM 音量
    /// </summary>
    public void SetVolume(float vol)
    {
        bgmVolume = Mathf.Clamp01(vol);
        if (audioSource != null) audioSource.volume = bgmVolume;
    }
}
