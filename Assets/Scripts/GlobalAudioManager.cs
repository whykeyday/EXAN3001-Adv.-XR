using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 全局音效管理器：单例模式 (Singleton) + DontDestroyOnLoad。
/// 修复了因为 Unity 判定 isPlaying 延迟导致的“无限连切”静音 Bug。
/// </summary>
public class GlobalAudioManager : MonoBehaviour
{
    [Header("BGM Tracks (拖入 4 首冥想音频)")]
    public AudioClip[] meditationTracks;

    [Header("Volume (音量调节)")]
    [Range(0f, 1f)]
    public float bgmVolume = 0.4f;

    private AudioSource audioSource;
    private int currentTrackIndex = -1;
    
    // 冷却时间，防止 Update 每帧都在疯狂切歌导致没声音
    private float nextAllowedPlayTime = 0f;

    private static GlobalAudioManager _instance = null;
    public static GlobalAudioManager Instance => _instance;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
            return; 
        }

        _instance = this;
        transform.SetParent(null); 
        DontDestroyOnLoad(this.gameObject);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false; 
        audioSource.spatialBlend = 0f; // 2D 贴耳音效
    }

    private bool isAllowedToPlay = false;

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        isAllowedToPlay = true;

        if (audioSource != null && !audioSource.isPlaying)
        {
            PlayRandomTrack();
        }
    }

    void Start()
    {
        if (meditationTracks == null || meditationTracks.Length == 0)
        {
            Debug.LogError("[GlobalAudioManager] ❌ 警告：你还没有放入任何 BGM 曲目！请在 Inspector 侧边栏拖入你的 mp3/wav。");
            return;
        }

        isAllowedToPlay = true;
        PlayRandomTrack();
    }

    void Update()
    {
        if (audioSource == null || meditationTracks == null || meditationTracks.Length == 0) return;

        // 实时同步 Inspector 音量
        if (Mathf.Abs(audioSource.volume - bgmVolume) > 0.01f)
        {
            audioSource.volume = bgmVolume;
        }

        // ★ 防抖切歌逻辑：当音乐确实完全停止，且过了冷却时间，才切下一首
        // 只有被允许播放 (isAllowedToPlay == true) 时才自动切歌
        if (isAllowedToPlay && !audioSource.isPlaying && Time.time >= nextAllowedPlayTime)
        {
            PlayNextTrack();
        }
    }

    void PlayNextTrack()
    {
        if (meditationTracks.Length == 0) return;

        currentTrackIndex = (currentTrackIndex + 1) % meditationTracks.Length;
        PlayTrack(currentTrackIndex);
    }

    void PlayRandomTrack()
    {
        if (meditationTracks.Length == 0) return;

        if (meditationTracks.Length == 1)
        {
            currentTrackIndex = 0;
        }
        else
        {
            int newIndex;
            do { newIndex = Random.Range(0, meditationTracks.Length); }
            while (newIndex == currentTrackIndex);
            
            currentTrackIndex = newIndex;
        }

        PlayTrack(currentTrackIndex);
    }

    void PlayTrack(int index)
    {
        if (index < 0 || index >= meditationTracks.Length || meditationTracks[index] == null)
        {
            Debug.LogWarning($"[GlobalAudioManager] 试图播放空白曲目 (Index: {index})");
            return;
        }

        audioSource.clip = meditationTracks[index];
        audioSource.volume = bgmVolume;
        audioSource.Play();
        
        // 关键！给 AudioSource 起步的时间，防止下一帧判定为 isPlaying == false 从而闪现切歌
        nextAllowedPlayTime = Time.time + 1.0f;
        
        Debug.Log($"[GlobalAudioManager] 正在播放曲目: {audioSource.clip.name}");
    }

    public void SetVolume(float vol)
    {
        bgmVolume = Mathf.Clamp01(vol);
        if (audioSource != null) audioSource.volume = bgmVolume;
    }
}
