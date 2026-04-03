using UnityEngine;
using System.Collections;
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

    [Header("Ocean Audio — 海鸥/水泡 (全局随机环境音)")]
    [Tooltip("海鸥叫声")]
    public AudioClip seagullClip;
    [Tooltip("水泡声")]
    public AudioClip bubbleClip;
    [Range(0f, 1f)] public float seagullVolume = 0.6f;
    [Range(0f, 1f)] public float bubbleVolume = 0.5f;

    private AudioSource seagullAudio;
    private AudioSource bubbleAudio;

    // Public property to access the breath value (0.0 to 1.0)
    public float BreathValue { get; private set; }

    public bool IsCalibrating => isCalibrating;
    public float CalibrationTimeRemaining => Mathf.Max(0, calibrationDuration - calibrationTimer);

    private AudioClip microphoneClip;
    private string microphoneDevice;
    private float[] audioSamples = new float[128];
    private float currentRms = 0f;
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
        if (seagullClip != null)
        {
            seagullAudio = gameObject.AddComponent<AudioSource>();
            seagullAudio.clip = seagullClip;
            seagullAudio.spatialBlend = 0f; // 2D全局可听
            seagullAudio.volume = seagullVolume;
            seagullAudio.playOnAwake = false;
            StartCoroutine(RandomSeagullRoutine());
        }
        if (bubbleClip != null)
        {
            bubbleAudio = gameObject.AddComponent<AudioSource>();
            bubbleAudio.clip = bubbleClip;
            bubbleAudio.spatialBlend = 0f; // 2D全局可听
            bubbleAudio.volume = bubbleVolume;
            bubbleAudio.playOnAwake = false;
            StartCoroutine(RandomBubbleRoutine());
        }
    }

    private IEnumerator RandomSeagullRoutine()
    {
        yield return new WaitForSeconds(Random.Range(2f, 5f));

        while (true)
        {
            if (seagullAudio != null)
            {
                seagullAudio.pitch = Random.Range(0.9f, 1.1f);
                seagullAudio.volume = seagullVolume * Random.Range(0.7f, 1f);
                seagullAudio.Play();
            }

            yield return new WaitForSeconds(Random.Range(4f, 7f));
        }
    }

    private IEnumerator RandomBubbleRoutine()
    {
        yield return new WaitForSeconds(Random.Range(1f, 3f));

        while (true)
        {
            if (bubbleAudio != null)
            {
                bubbleAudio.pitch = Random.Range(0.85f, 1.15f);
                bubbleAudio.volume = bubbleVolume * Random.Range(0.5f, 1f);
                bubbleAudio.Play();
            }

            yield return new WaitForSeconds(Random.Range(4f, 7f));
        }
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
