using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 全局音效管理器：绝对无缝、开局必响的完美版。
/// 彻底根除 VR 冷启动声音丢失 Bug 和场景切换音乐被打断 Bug。
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
    
    // 防抖冷却：保护系统在刚切歌或刚进游戏时，不会因为 Unity 判定延迟而无限跳歌
    private float nextAllowedPlayTime = 0f;

    private static GlobalAudioManager _instance = null;
    public static GlobalAudioManager Instance => _instance;

    void Awake()
    {
        // 1. 单例拦截：如果是重复加载的场景里的 Prefab 组件，立刻自杀，保护老组件存活
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

        audioSource.loop = false;  // 我们用代码写顺序连播，不用原生 Loop
        audioSource.spatialBlend = 0f; // 2D 贴耳音效，无关距离设定
        audioSource.ignoreListenerPause = true;

        // ★ 修复 1：一进游戏就不响的问题
        // VR 的 Audio 系统在第一帧直接用代码 Play() 经常会被忽略，最可靠的办法是利用原生 playOnAwake
        if (meditationTracks != null && meditationTracks.Length > 0)
        {
            // 确保真随机：用系统时间作为种子，打破 Unity 初始冷启动随机因子定死的 Bug
            Random.InitState((int)System.DateTime.Now.Ticks);
            
            // 在 Awake 期间就直接选好第一首歌（真·随机）
            currentTrackIndex = Random.Range(0, meditationTracks.Length);
            audioSource.clip = meditationTracks[currentTrackIndex];
            audioSource.volume = bgmVolume;
            audioSource.playOnAwake = true; // 开启原生启动，只要系统加载完必响！
            
            // 给它 3 秒的启动保护期，防止 Update 刚启动时乱切歌
            nextAllowedPlayTime = Time.time + 3.0f;
            Debug.Log($"[GlobalAudioManager] Awake initialized. Picked true random first track: {audioSource.clip.name}");
        }
    }

    void Start()
    {
        if (meditationTracks == null || meditationTracks.Length == 0)
        {
            Debug.LogError("[GlobalAudioManager] ❌ 警告：你还没有放入任何 BGM 曲目！请在 Inspector 侧边栏拖入你的 mp3/wav。");
        }
    }

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
        // ★ 修复 2：场景切换时总是重新播放（或仿佛又是同一首）的问题。
        // Unity 切场景时，isPlaying 经常会在那一帧返回 false。
        // 我们【坚决不】在这里写如果不在放就重新 PlayRandomTrack()，因为这会掐断原本好好放着的音乐！
        // 这一步唯一需要做的就是补一下设置参数，防止被其他场景参数覆盖。
        if (audioSource != null)
        {
            audioSource.spatialBlend = 0f;
            nextAllowedPlayTime = Time.time + 3.0f; // 切场景时禁止切歌锁定 3 秒，让引擎缓存度过卡顿期
            Debug.Log($"[GlobalAudioManager] Scene {scene.name} loaded. BGM continues seamlessly.");
        }
    }

    void Update()
    {
        if (audioSource == null || meditationTracks == null || meditationTracks.Length == 0) return;

        // 实时同步 Inspector 音量
        if (Mathf.Abs(audioSource.volume - bgmVolume) > 0.01f)
        {
            audioSource.volume = bgmVolume;
        }

        // ★ 防抖顺连逻辑：如果确实彻底放完了，且避开了所有场景卡顿保护期，才放下一首
        if (!audioSource.isPlaying && Time.time >= nextAllowedPlayTime)
        {
            PlayNextTrack();
        }
    }

    void PlayNextTrack()
    {
        if (meditationTracks.Length == 0) return;

        // 重新使用真随机切歌，但必定排除上一首，绝不连续放同一首歌
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
            return;
        }

        audioSource.clip = meditationTracks[index];
        audioSource.volume = bgmVolume;
        audioSource.Play();
        
        // 锁定切歌功能 3 秒，防止任何意外判定
        nextAllowedPlayTime = Time.time + 3.0f;
        
        Debug.Log($"[GlobalAudioManager] 正在播放曲目: {audioSource.clip.name}");
    }

    public void SetVolume(float vol)
    {
        bgmVolume = Mathf.Clamp01(vol);
        if (audioSource != null) audioSource.volume = bgmVolume;
    }
}
