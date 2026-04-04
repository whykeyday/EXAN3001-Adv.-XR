using UnityEngine;

/// <summary>
/// 全局音效管理器：贯穿所有场景随机循环播放背景音乐。
/// 挂载此组件的物体会自动跨场景保留 (DontDestroyOnLoad)。
/// 进入 BasicScene 后开始播放，切换任何场景都不会中断。
/// </summary>
public class GlobalAudioManager : MonoBehaviour
{

    [Header("BGM Tracks (拖入 global1/2/3/4 音频)")]
    public AudioClip[] meditationTracks;

    [Header("Volume")]
    [Range(0f, 1f)]
    public float bgmVolume = 0.4f;

    private AudioSource audioSource;
    private float trackStartTime;
    private int lastPlayedIndex = -1;

    private static GlobalAudioManager _instance = null;
    public static GlobalAudioManager Instance => _instance;

    void Awake()
    {
        // ★ 采用用户提供的标准 Singleton 模式
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        _instance = this;
        transform.SetParent(null); // DDOL 要求必须是根物体
        DontDestroyOnLoad(this.gameObject);

        // 自动准备 AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false; // 由脚本控制循环
        audioSource.spatialBlend = 0f; // ★ 2D Constant Volume
        audioSource.ignoreListenerPause = true;
        audioSource.ignoreListenerVolume = true;

        Debug.Log("[DIAGNOSTIC] GlobalAudioManager Singleton Initialized. 2D mode active.");
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
            Debug.LogWarning("[DIAGNOSTIC] GlobalBGM AudioSource was lost — rebuilt.");
        }

        // 强行锁定状态，应对任何外部脚本篡改
        audioSource.mute = false;
        audioSource.volume = bgmVolume;
        audioSource.spatialBlend = 0f;
        audioSource.ignoreListenerPause = true;
        audioSource.ignoreListenerVolume = true;

        if (!audioSource.isPlaying && meditationTracks != null && meditationTracks.Length > 0)
        {
            Debug.LogWarning("[DIAGNOSTIC] GlobalBGM silent in Update — forcing recovery.");
            PlayRandomTrack();
        }

        // ★ 音量实时同步 Inspector 调节
        if (audioSource != null)
        {
            audioSource.volume = bgmVolume;
            // 强行锁定 2D + 忽略暂停，应对任何意外覆盖
            audioSource.spatialBlend = 0f;
            audioSource.ignoreListenerPause = true;
            audioSource.ignoreListenerVolume = true; // ★ 即使 Master volume 为 0 也不受影响

            if (!audioSource.isPlaying && meditationTracks != null && meditationTracks.Length > 0)
            {
                Debug.LogWarning("[GlobalBGM] AudioSource was NOT playing in Update — forced recovery.");
                PlayRandomTrack();
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
        if (index < 0 || index >= meditationTracks.Length || meditationTracks[index] == null)
        {
            Debug.LogError($"[DIAGNOSTIC] Track {index} is NULL or out of range!");
            return;
        }

        lastPlayedIndex = index;
        audioSource.clip = meditationTracks[index];
        audioSource.loop = false; // 由 Update 链式驱动
        audioSource.mute = false;
        audioSource.volume = bgmVolume;
        audioSource.Play();
        trackStartTime = Time.time;
        Debug.Log($"[DIAGNOSTIC] BGM Playing track {index}: {meditationTracks[index].name}, Vol: {audioSource.volume}, Mute: {audioSource.mute}");
    }

    [ContextMenu(">>> FORCE RESTART BGM <<<")]
    public void ForcePlay()
    {
        Debug.Log($"[GlobalBGM] ForcePlay (Restart) called. Track count: {meditationTracks?.Length ?? 0}");
        if (meditationTracks != null && meditationTracks.Length > 0)
        {
            PlayRandomTrack();
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
