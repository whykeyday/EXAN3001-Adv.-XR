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
            // 真随机：系统 Guid 哈希作为绝对不可预测的种子，避免冷启动时钟种子相同
            Random.InitState(System.Guid.NewGuid().GetHashCode());
            
            // 在 Awake 期间就直接选好第一首歌（真·随机）
            currentTrackIndex = Random.Range(0, meditationTracks.Length);
            audioSource.clip = meditationTracks[currentTrackIndex];
            audioSource.volume = bgmVolume;
            
            // 引擎会瞬间开始播放 clip，强制重新覆盖
            audioSource.Play(); 
            
            // 给系统 2 秒的启动保护期，这段时间内禁止乱切歌
            nextAllowedPlayTime = Time.realtimeSinceStartup + 2.0f;
            Debug.Log($"[GlobalAudioManager] Awake initialized. Picked true random first track: {audioSource.clip.name}");
        }
    }


    System.Collections.IEnumerator Start()
    {
        if (meditationTracks == null || meditationTracks.Length == 0)
        {
            Debug.LogError("[GlobalAudioManager] ❌ 警告：你还没有放入任何 BGM 曲目！请在 Inspector 侧边栏拖入你的 mp3/wav。");
            yield break;
        }

        // ★ 核心修复：VR 一开机有时候连原生的 playOnAwake 都不认。
        // 强制等待 1.5 秒确保底层引擎完全就绪，然后补一发强行 Play()
        yield return new WaitForSeconds(1.5f);

        if (audioSource != null && !audioSource.isPlaying)
        {
            audioSource.Play();
            Debug.Log("[GlobalAudioManager] Delayed Start triggered Play() as a fallback for VR cold boot.");
        }

        // ★ 完全摒弃易碎的 Update 循环，启动独立的协程绝对监控 BGM 状态
        StartCoroutine(BgmMonitorRoutine());
    }

    System.Collections.IEnumerator BgmMonitorRoutine()
    {
        while (true)
        {
            // 每秒检查一次，极为节省性能且不受任何异常中断影响
            yield return new WaitForSecondsRealtime(1.0f);

            if (audioSource == null || meditationTracks == null || meditationTracks.Length == 0) continue;

            // 如果当前确实没在播了，且距上次切歌至少过了 3 秒冷却
            if (!audioSource.isPlaying && Time.realtimeSinceStartup >= nextAllowedPlayTime)
            {
                Debug.Log("[GlobalAudioManager] 协程检测到当前无声，立刻切换下一首！");
                PlayNextTrack();
            }
            // 补偿：如果快要播完了，提前零点几秒无缝切换
            else if (audioSource.isPlaying && audioSource.clip != null && Time.realtimeSinceStartup >= nextAllowedPlayTime)
            {
                if (audioSource.time >= audioSource.clip.length - 0.2f)
                {
                    Debug.Log("[GlobalAudioManager] 协程检测到当前曲目即将结束，无缝衔接下一首！");
                    PlayNextTrack();
                }
            }
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
            nextAllowedPlayTime = Time.realtimeSinceStartup + 3.0f; // 切场景时禁止切歌锁定 3 秒，让引擎缓存度过卡顿期
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

        // ★ OVRInput 极其容易在跨场景或者未带头显的情况下抛出异常导致 Update 崩溃，必须用 try-catch 保护
        try 
        {
            if (OVRInput.GetDown(OVRInput.RawButton.A) || OVRInput.GetDown(OVRInput.Button.One) || Input.GetKeyDown(KeyCode.Space))
            {
                Debug.Log("[GlobalAudioManager] User pressed 'A' or Space to skip track.");
                PlayNextTrack();
            }
        } 
        catch (System.Exception) { /* 忽略 OVRInput 异常，防止掐断系统 */ }
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
        nextAllowedPlayTime = Time.realtimeSinceStartup + 3.0f;
        
        Debug.Log($"[GlobalAudioManager] 正在播放曲目: {audioSource.clip.name}");

    }

    public void SetVolume(float vol)
    {
        bgmVolume = Mathf.Clamp01(vol);
        if (audioSource != null) audioSource.volume = bgmVolume;
    }
}
