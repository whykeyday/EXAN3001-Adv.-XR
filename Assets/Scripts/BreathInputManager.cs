using UnityEngine;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

public class BreathInputManager : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Adjusts how quickly the value changes. Lower = Slower/Smoother.")]
    [Range(0.01f, 1f)]
    public float smoothness = 0.1f;
    
    [Header("Calibration")]
    [Tooltip("Time in seconds to calibrate min/max values on start.")]
    public float calibrationDuration = 5f;

    [Header("Debug")]
    [Tooltip("Assign a cube here to visualize the breath value.")]
    public GameObject debugCube;
    [Tooltip("Key to simulate breathing when microphone is not available.")]
    public KeyCode debugKey = KeyCode.Space;

    [Header("Output")]
    [Tooltip("The normalized breath value (0 to 1).")]
    [Range(0f, 1f)]
    public float BreathValue;

    private AudioClip micClip;
    private string micDevice;
    private float[] samples = new float[128];
    private float smoothedRMS;
    private float minRMS = 100f;
    private float maxRMS = 0.01f;
    private bool isCalibrating = false;
    private float calibrationTimer = 0f;

    // Public getters for UI access
    public bool IsCalibrating => isCalibrating;
    public float CalibrationTimeRemaining => calibrationTimer;

    void Start()
    {
        // 1. Android Permissions
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
        }
#endif

        // 2. Microphone Handling
        if (Microphone.devices.Length > 0)
        {
            micDevice = null; // Use default
            micClip = Microphone.Start(micDevice, true, 10, 44100);
            Debug.Log("Microphone started.");
        }
        else
        {
            Debug.LogWarning("BreathInputManager: No microphone found. Using fallback key.");
        }

        // Start calibration automatically
        Calibrate();
    }

    /// <summary>
    /// Resets calibration values and starts the calibration timer.
    /// </summary>
    public void Calibrate()
    {
        isCalibrating = true;
        calibrationTimer = calibrationDuration;
        minRMS = 100f; // Reset to high
        maxRMS = 0.01f; // Reset to low
        Debug.Log("BreathInputManager: Calibration Started.");
    }

    void Update()
    {
        float currentRMS = 0f;

        // 3. Signal Processing
        if (Microphone.IsRecording(micDevice))
        {
            // Get position in the clip
            int pos = Microphone.GetPosition(micDevice);
            int startPos = pos - samples.Length;

            // Check if we have enough data (wrap-around handling simple check)
            // If startPos is negative, we just skip this frame to avoid reading invalid data or needing complex wrap logic for this prototype.
            if (startPos >= 0)
            {
                micClip.GetData(samples, startPos);

                // Calculate RMS
                float sum = 0f;
                for (int i = 0; i < samples.Length; i++)
                {
                    sum += samples[i] * samples[i];
                }
                currentRMS = Mathf.Sqrt(sum / samples.Length);
            }
        }
        else
        {
            // Fallback Input
            if (Input.GetKey(debugKey))
            {
                // Simulate a moderate breath signal
                currentRMS = Mathf.Lerp(minRMS, maxRMS, 0.8f); 
                if (maxRMS <= minRMS) currentRMS = 0.5f; // Default if not calibrated well
            }
            else
            {
                 currentRMS = minRMS;
            }
        }

        // 4. Smoothing
        // Lerp towards the target RMS. 
        // Note: Frame-rate dependent, for better consistency use Time.deltaTime
        smoothedRMS = Mathf.Lerp(smoothedRMS, currentRMS, smoothness);

        // 5. Calibration
        if (isCalibrating)
        {
            if (smoothedRMS < minRMS) minRMS = smoothedRMS;
            if (smoothedRMS > maxRMS) maxRMS = smoothedRMS;

            calibrationTimer -= Time.deltaTime;
            if (calibrationTimer <= 0)
            {
                isCalibrating = false;
                
                // Safety check to ensure we don't divide by zero later
                if (maxRMS <= minRMS) maxRMS = minRMS + 0.01f;
                
                Debug.Log($"BreathInputManager: Calibration Complete. Min: {minRMS}, Max: {maxRMS}");
            }
        }

        // Normalization
        BreathValue = Mathf.InverseLerp(minRMS, maxRMS, smoothedRMS);

        // 6. Debug Visualization
        if (debugCube != null)
        {
            // Scale the cube based on breath value (e.g., 1x to 2x scale)
            debugCube.transform.localScale = Vector3.one * (1f + BreathValue);
        }
    }
}
