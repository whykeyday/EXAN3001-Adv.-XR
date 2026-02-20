using UnityEngine;

/// <summary>
/// Maps BreathValue from BreathInputManager to particle turbulence and audio feedback.
/// Attach to the Ocean scene or a manager object.
/// </summary>
public class BreathReactor : MonoBehaviour
{
    // ============ REFERENCES ============
    [Header("References")]
    [Tooltip("Reference to the BreathInputManager")]
    public BreathInputManager breathInput;

    [Tooltip("The main Ocean particle system to control")]
    public ParticleSystem oceanParticles;

    [Tooltip("Audio source for ocean waves")]
    public AudioSource oceanAudio;

    // ============ NOISE SETTINGS ============
    [Header("Noise Control (Turbulence)")]
    [Tooltip("Minimum noise strength when breath is calm (0)")]
    public float minNoiseStrength = 0.05f;

    [Tooltip("Maximum noise strength when breath is high (1)")]
    public float maxNoiseStrength = 0.5f;

    [Tooltip("Minimum scroll speed when calm")]
    public float minScrollSpeed = 0.02f;

    [Tooltip("Maximum scroll speed when anxious")]
    public float maxScrollSpeed = 0.3f;

    [Tooltip("Minimum noise frequency when calm")]
    public float minNoiseFrequency = 0.05f;

    [Tooltip("Maximum noise frequency when anxious")]
    public float maxNoiseFrequency = 0.4f;

    // ============ COLOR SETTINGS ============
    [Header("Color Control")]
    [Tooltip("Color when calm (deep blue/cyan)")]
    public Color calmColor = new Color(0.1f, 0.4f, 0.8f, 0.9f);

    [Tooltip("Color when anxious (grey/white foam)")]
    public Color anxiousColor = new Color(0.8f, 0.85f, 0.9f, 0.9f);

    // ============ AUDIO SETTINGS ============
    [Header("Audio Control")]
    [Tooltip("Minimum volume when calm")]
    [Range(0f, 1f)]
    public float minVolume = 0.2f;

    [Tooltip("Maximum volume when anxious")]
    [Range(0f, 1f)]
    public float maxVolume = 1.0f;

    [Tooltip("Minimum pitch when calm")]
    public float minPitch = 0.8f;

    [Tooltip("Maximum pitch when anxious")]
    public float maxPitch = 1.3f;

    // ============ SMOOTHING ============
    [Header("Smoothing")]
    [Tooltip("How quickly values transition (lower = smoother)")]
    [Range(0.01f, 1f)]
    public float smoothingSpeed = 0.1f;

    // ============ PRIVATE ============
    private float smoothedBreathValue = 0f;
    private ParticleSystem.NoiseModule noiseModule;
    private ParticleSystem.MainModule mainModule;

    private void Start()
    {
        // Auto-find BreathInputManager if not assigned
        if (breathInput == null)
        {
            breathInput = FindObjectOfType<BreathInputManager>();
            if (breathInput == null)
            {
                Debug.LogError("BreathReactor: BreathInputManager not found!");
            }
        }

        // Cache particle modules
        if (oceanParticles != null)
        {
            noiseModule = oceanParticles.noise;
            mainModule = oceanParticles.main;
        }
    }

    private void Update()
    {
        if (breathInput == null) return;

        // Get current breath value (0-1)
        float currentBreath = breathInput.BreathValue;

        // Smooth the value for gradual transitions
        smoothedBreathValue = Mathf.Lerp(smoothedBreathValue, currentBreath, smoothingSpeed);

        // Apply turbulence control
        ApplyTurbulence(smoothedBreathValue);

        // Apply audio feedback
        ApplyAudioFeedback(smoothedBreathValue);
    }

    private void ApplyTurbulence(float breathValue)
    {
        if (oceanParticles == null) return;

        // Map breath value to noise parameters
        float noiseStrength = Mathf.Lerp(minNoiseStrength, maxNoiseStrength, breathValue);
        float scrollSpeed = Mathf.Lerp(minScrollSpeed, maxScrollSpeed, breathValue);
        float frequency = Mathf.Lerp(minNoiseFrequency, maxNoiseFrequency, breathValue);

        // Apply to noise module
        noiseModule.enabled = true;
        noiseModule.strength = noiseStrength;
        noiseModule.scrollSpeed = scrollSpeed;
        noiseModule.frequency = frequency;

        // Apply color change
        Color currentColor = Color.Lerp(calmColor, anxiousColor, breathValue);
        mainModule.startColor = currentColor;

        // Optional: Increase Y-axis noise more for vertical "churning" effect
        if (noiseModule.separateAxes)
        {
            noiseModule.strengthY = noiseStrength * 1.5f; // Extra vertical chaos
        }
    }

    private void ApplyAudioFeedback(float breathValue)
    {
        if (oceanAudio == null) return;

        // Map breath to volume and pitch
        oceanAudio.volume = Mathf.Lerp(minVolume, maxVolume, breathValue);
        oceanAudio.pitch = Mathf.Lerp(minPitch, maxPitch, breathValue);
    }

    // ============ PUBLIC API ============

    /// <summary>
    /// Manually set the breath value (for testing or external control)
    /// </summary>
    public void SetBreathValue(float value)
    {
        if (breathInput != null)
        {
            // This would require BreathInputManager to expose a setter
            Debug.Log($"BreathReactor: Manual breath value set to {value}");
        }
        smoothedBreathValue = Mathf.Clamp01(value);
    }

    /// <summary>
    /// Reset to calm state
    /// </summary>
    public void ResetToCalm()
    {
        smoothedBreathValue = 0f;
        ApplyTurbulence(0f);
        ApplyAudioFeedback(0f);
    }
}
