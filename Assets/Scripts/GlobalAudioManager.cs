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
        audioSource.ignoreListenerPause = true;  // ★ XR 传送/场景切换时 AudioListener 可能短暂暂停，BGM 不受影响
        audioSource.ignoreListenerVolume = true;  // ★ 不被全局 AudioListener.volume 拖累

        // 确保没有任何距离衰减脚本干扰全局 BGM
        AudioDistanceFader oldFader = GetComponent<AudioDistanceFader>();
        if (oldFader != null) Destroy(oldFader);
    }

    void Start()
    {
        Debug.Log($"[GlobalAudioManager] Start. Volume: {bgmVolume}, tracks: {meditationTracks?.Length ?? 0}, GO: {gameObject.name}, parent: {(transform.parent != null ? transform.parent.name : "ROOT")}");
        if (meditationTracks != null && meditationTracks.Length > 0)
        {
            // 逐个检查 tracks 是否为 null
            for (int i = 0; i < meditationTracks.Length; i++)
                Debug.Log($"[GlobalAudioManager] Track[{i}]: {(meditationTracks[i] != null ? meditationTracks[i].name : "NULL")}");

            if (!audioSource.isPlaying)
                PlayRandomTrack();
        }
        else
        {
            Debug.LogError("[GlobalAudioManager] ❌ No tracks assigned in the Inspector! 请拖入 BGM 音频！");
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
        Debug.Log($"[GlobalBGM] Scene loaded: {scene.name}. isPlaying: {audioSource?.isPlaying}");

        // 场景切换后确保音频继续播放（Unity有时会在切场景时暂停AudioSource）
        if (audioSource != null && meditationTracks != null && meditationTracks.Length > 0)
        {
            // ★ 强制刷新 2D + ignoreListenerPause，防止新场景中的某些设置覆盖
            audioSource.spatialBlend = 0f;
            audioSource.ignoreListenerPause = true;
            audioSource.ignoreListenerVolume = true;

            if (!audioSource.isPlaying)
            {
                audioSource.UnPause();
                if (!audioSource.isPlaying)
                {
                    Debug.Log("[GlobalBGM] UnPause failed, playing random track.");
                    PlayRandomTrack();
                }
            }
        }
        else
        {
             Debug.LogWarning($"[GlobalBGM] SceneLoaded but audioSource or tracks are null! Source: {audioSource != null}, Tracks: {meditationTracks?.Length ?? 0}");
        }
    }

    void Update()
    {
        // ★ 防御性恢复：如果 AudioSource 被意外销毁（场景切换副作用），立即重建
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f;
            audioSource.ignoreListenerPause = true;
            audioSource.ignoreListenerVolume = true;
            Debug.LogWarning("[GlobalAudioManager] AudioSource was lost — rebuilt.");
        }

        // ★ 音量实时同步 Inspector 调节
        if (audioSource != null)
        {
            audioSource.volume = bgmVolume;
            // 强行锁定 2D + 忽略暂停，应对任何意外覆盖
            audioSource.spatialBlend = 0f;
            audioSource.ignoreListenerPause = true;
            audioSource.ignoreListenerVolume = true;

            if (!audioSource.isPlaying && meditationTracks != null && meditationTracks.Length > 0)
            {
                Debug.LogWarning("[GlobalBGM] AudioSource was NOT playing — forced PlayNextTrack().");
                PlayNextTrack();
            }
        }

        // 当前曲目播完，自动播下一首曲目
        if (meditationTracks != null && meditationTracks.Length > 0)
        {
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
