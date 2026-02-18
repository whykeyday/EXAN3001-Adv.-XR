using UnityEngine;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

public class BreathInputManager : MonoBehaviour
{
    [Header("Settings")]
    public float smoothness = 0.1f;
    public KeyCode debugKey = KeyCode.Space;

    [Header("Calibration")]
    public float calibrationDuration = 5.0f;
    
    [Header("Debug")]
    public GameObject debugCube;
    
    // Public property to access the breath value (0.0 to 1.0)
    public float BreathValue { get; private set; }

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
            microphoneDevice = Microphone.devices[0];
            microphoneClip = Microphone.Start(microphoneDevice, true, 10, 44100);
            Debug.Log($"Microphone started: {microphoneDevice}");
        }
        else
        {
            Debug.LogWarning("No microphone found! Using Debug Key.");
        }
    }

    void Update()
    {
        // 3. Signal Processing
        float targetRms = 0f;

        if (Microphone.IsRecording(microphoneDevice))
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
        int position = Microphone.GetPosition(microphoneDevice) - 128;
        if (position < 0) return 0;

        microphoneClip.GetData(audioSamples, position);

        float sum = 0f;
        for (int i = 0; i < audioSamples.Length; i++)
        {
            sum += audioSamples[i] * audioSamples[i]; // Square
        }

        return Mathf.Sqrt(sum / audioSamples.Length); // Root Mean Square
    }
}
