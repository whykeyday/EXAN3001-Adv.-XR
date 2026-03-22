using UnityEngine;

/// <summary>
/// 全局音效管理器：贯穿所有场景循环播放背景音乐。
/// 挂载此组件的物体会自动跨场景保留 (DontDestroyOnLoad)。
/// </summary>
public class GlobalAudioManager : MonoBehaviour
{
    public static GlobalAudioManager Instance { get; private set; }
    
    [Header("BGM Tracks (Assign 3-4 meditation tracks)")]
    public AudioClip[] meditationTracks;
    
    private AudioSource audioSource;
    private int currentTrackIndex = 0;

    void Awake()
    {
        // 保证单例及跨场景存活
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = false; // 由脚本控制循环以便切歌
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 完全2D音效，充当BGM
        audioSource.volume = 0.5f;
    }

    void Start()
    {
        PlayNextTrack();
    }

    void Update()
    {
        // 如果当前没有在播放，并且列表里有歌，就播放下一首
        if (audioSource != null && !audioSource.isPlaying && meditationTracks != null && meditationTracks.Length > 0)
        {
            PlayNextTrack();
        }
    }

    void PlayNextTrack()
    {
        if (meditationTracks == null || meditationTracks.Length == 0) return;

        audioSource.clip = meditationTracks[currentTrackIndex];
        audioSource.Play();

        // 索引推进
        currentTrackIndex = (currentTrackIndex + 1) % meditationTracks.Length;
    }
}
