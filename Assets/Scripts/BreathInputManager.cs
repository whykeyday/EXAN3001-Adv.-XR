using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

public class BreathInputManager : MonoBehaviour
{
    [Header("Settings")]
    public float smoothness = 0.8f; // High sensitivity
    public KeyCode debugKey = KeyCode.Space;

    [Header("Calibration")]
    public float calibrationDuration = 5.0f;

    [Header("Debug")]
    public GameObject debugCube;

    [Header("Ocean Audio —— 海鸥/水泡 (全局随机环境音)")]
    [Tooltip("海鸥叫声（手动拖入最多 5 个音频）")]
    public AudioClip[] seagullClips = new AudioClip[5]; 
    [Tooltip("水泡声")]
    public AudioClip bubbleClip;
    [Range(0f, 1f)] public float seagullVolume = 0.6f;
    [Range(0f, 1f)] public float bubbleVolume = 0.5f;

    [Header("Random Intervals (手动调整随机间隔)")]
    public float minSeagullInterval = 4f;
    public float maxSeagullInterval = 7f;
    public float minBubbleInterval = 4f;
    public float maxBubbleInterval = 7f;

    [Header("Distance Fade Settings (手动调整)")]
    public float ambientFarDistance = 15f;
    public float ambientNearDistance = 1.0f;
    public float ambientFalloff = 1.5f;

    private AudioSource seagullAudio;
    private AudioSource bubbleAudio;

    // Public property to access the breath value (0.0 to 1.0)
    public float BreathValue { get; private set; }

    public bool IsCalibrating => isCalibrating;
    public float CalibrationTimeRemaining => Mathf.Max(0, calibrationDuration - calibrationTimer);

    private AudioClip microphoneClip;
    private string microphoneDevice;
    private float[] audioSamples = new float[128];
    private float smoothedRms = 0f;
    
    // Calibration variables
    private float minRms = 1000f; // Start high
    private float maxRms = 0.001f; // Start low (avoid divide by zero)
    private bool isCalibrating = true;
    private float calibrationTimer = 0f;

    void Start()
    {
        InitializeMicrophone();
        SetupOceanAudio();
    }

    void SetupOceanAudio()
    {
        // 启动随机音效循环
        StartCoroutine(RandomSeagullRoutine());
        StartCoroutine(RandomBubbleRoutine());
    }

    private IEnumerator RandomSeagullRoutine()
    {
        while (true)
        {
            float wait = Random.Range(minSeagullInterval, maxSeagullInterval);
            yield return new WaitForSeconds(wait);

            if (seagullClips != null && seagullClips.Length > 0)
            {
                // 随机选一个非空的 Clip
                List<AudioClip> validClips = new List<AudioClip>();
                foreach (var c in seagullClips) if (c != null) validClips.Add(c);

                if (validClips.Count > 0)
                {
                    AudioClip clip = validClips[Random.Range(0, validClips.Count)];
                    PlayAmbient3DSound(clip, seagullVolume, 5f, "SeagullEmitter_Temp");
                    // ★ 核心修复：确保海鸥叫完之后，才开始倒数 6-9 秒间隔，否则就变成了重叠加频！
                    yield return new WaitForSeconds(clip.length);
                }
            }
        }
    }

    private IEnumerator RandomBubbleRoutine()
    {
        while (true)
        {
            float wait = Random.Range(minBubbleInterval, maxBubbleInterval);
            yield return new WaitForSeconds(wait);

            if (bubbleClip != null)
            {
                PlayAmbient3DSound(bubbleClip, bubbleVolume, 2f, "BubbleEmitter_Temp");
                // 同理，等待水泡播放完毕
                yield return new WaitForSeconds(bubbleClip.length);
            }
        }
    }

    private void PlayAmbient3DSound(AudioClip clip, float volume, float dist, string emitterName)
    {
        if (clip == null) return;
        
        GameObject emitter = new GameObject(emitterName);
        // 让它始终跟随相机，防止跑远了没声
        emitter.transform.SetParent(Camera.main.transform, false);
        emitter.transform.localPosition = Random.onUnitSphere * dist;

        AudioSource src = emitter.AddComponent<AudioSource>();
        src.clip = clip;
        src.volume = volume;
        src.spatialBlend = 0f; // ★ 用户要求 Constant Volume，强制 2D 播放
        src.ignoreListenerPause = true; // ★ 防止传送中断
        src.playOnAwake = false;
        src.Play();
        
        Destroy(emitter, clip.length + 1f);
        Debug.Log($"[OceanSound] Played {emitterName} (2D Constant): {clip.name} Vol: {volume}");
    }

    void InitializeMicrophone()
    {
        // 1. Check Android Permissions
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
        }
#endif

        // 2. Start Microphone
        if (Microphone.devices.Length > 0)
        {
            // Log all devices
            for (int i = 0; i < Microphone.devices.Length; i++)
            {
                Debug.Log($"Microphone Device {i}: {Microphone.devices[i]}");
            }

            microphoneDevice = Microphone.devices[0];
            Debug.Log($"Selected Microphone: {microphoneDevice}");

            try
            {
                // Start recording, loop = true, 10s buffer, 44100Hz
                microphoneClip = Microphone.Start(microphoneDevice, true, 10, 44100);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to start microphone: {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning("No microphone found! Using Debug Key (Space).");
        }
    }

    void Update()
    {
        // 3. Signal Processing
        float targetRms = 0f;

        // Check if recording is actually active
        if (microphoneClip != null && Microphone.IsRecording(microphoneDevice))
        {
            targetRms = PrepareRMS();
        }
        else
        {
            // Fallback for debugging without mic
            if (Input.GetKey(debugKey))
            {
                targetRms = 0.5f; // Simulates half breath
            }
        }

        // 4. Smoothing
        smoothedRms = Mathf.Lerp(smoothedRms, targetRms, smoothness);

        // 5. Calibration & Normalization
        if (isCalibrating)
        {
            calibrationTimer += Time.deltaTime;
            
            // Adjust min/max during calibration
            if (smoothedRms < minRms) minRms = smoothedRms;
            if (smoothedRms > maxRms) maxRms = smoothedRms;

            if (calibrationTimer >= calibrationDuration)
            {
                isCalibrating = false;
                Debug.Log($"Calibration Complete. Min: {minRms}, Max: {maxRms}");
            }
        }

        // Normalize
        // Ensure we don't divide by zero or negative range
        float range = maxRms - minRms;
        if (range <= 0.0001f) range = 0.0001f;

        BreathValue = Mathf.Clamp01((smoothedRms - minRms) / range);

        // 6. Debug Visualization
        if (debugCube != null)
        {
            // Scale cube based on breath value (1 = base scale, up to 2x or similar)
            float scale = 1.0f + BreathValue; 
            debugCube.transform.localScale = Vector3.one * scale;
        }
    }

    float PrepareRMS()
    {
        // Get position logic to read latest data
        int position = Microphone.GetPosition(microphoneDevice);
        
        // Handle wrap-around or negative index
        // We want the last 128 samples
        int startPos = position - 128;
        if (startPos < 0) return 0; // Simple safety for very first frame

        microphoneClip.GetData(audioSamples, startPos);

        float sum = 0f;
        for (int i = 0; i < audioSamples.Length; i++)
        {
            sum += audioSamples[i] * audioSamples[i]; // Square
        }

        return Mathf.Sqrt(sum / audioSamples.Length); // Root Mean Square
    }
}
