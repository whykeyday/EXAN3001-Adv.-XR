using UnityEngine;
using UnityEngine.VFX;

/// <summary>
/// Bridges BreathInputManager to VFX Graph.
/// Sets BreathIntensity parameter on the VFX Graph based on breath value.
/// Also controls bubble emission when breath is high.
/// </summary>
public class VFXBreathBridge : MonoBehaviour
{
    // ============ REFERENCES ============
    [Header("References")]
    [Tooltip("Reference to the BreathInputManager")]
    public BreathInputManager breathInput;

    [Tooltip("The main Ocean VFX Graph")]
    public VisualEffect oceanVFX;

    [Tooltip("Optional: Bubble VFX for high breath")]
    public VisualEffect bubbleVFX;

    // ============ VFX PARAMETER NAMES ============
    [Header("VFX Parameter Names")]
    [Tooltip("Name of the float parameter in VFX Graph for breath intensity")]
    public string breathIntensityParam = "BreathIntensity";

    [Tooltip("Name of the turbulence strength parameter")]
    public string turbulenceStrengthParam = "TurbulenceStrength";

    [Tooltip("Name of the turbulence frequency parameter")]
    public string turbulenceFrequencyParam = "TurbulenceFrequency";

    [Tooltip("Name of the particle drag parameter")]
    public string dragParam = "Drag";

    // ============ TURBULENCE MAPPING ============
    [Header("Turbulence Mapping (Calm -> Chaotic)")]
    [Tooltip("Turbulence strength when calm (breath = 0)")]
    public float calmTurbulenceStrength = 0.3f;

    [Tooltip("Turbulence strength when heavy breathing (breath = 1)")]
    public float chaosTurbulenceStrength = 2.5f;

    [Tooltip("Turbulence frequency when calm")]
    public float calmTurbulenceFrequency = 0.5f;

    [Tooltip("Turbulence frequency when chaotic")]
    public float chaosTurbulenceFrequency = 2.0f;

    [Tooltip("Drag when calm (slow drift)")]
    public float calmDrag = 0.8f;

    [Tooltip("Drag when chaotic (faster response)")]
    public float chaosDrag = 0.3f;

    // ============ BUBBLE SETTINGS ============
    [Header("Bubble Settings")]
    [Tooltip("Breath threshold to start emitting bubbles")]
    [Range(0f, 1f)]
    public float bubbleThreshold = 0.6f;

    [Tooltip("Bubble spawn rate at max breath")]
    public float maxBubbleRate = 50f;

    // ============ SMOOTHING ============
    [Header("Smoothing")]
    [Tooltip("How quickly values transition")]
    [Range(0.01f, 1f)]
    public float smoothingSpeed = 0.15f;

    // ============ PRIVATE ============
    private float smoothedBreathValue = 0f;

    private void Start()
    {
        // Auto-find BreathInputManager if not assigned
        if (breathInput == null)
        {
            breathInput = FindObjectOfType<BreathInputManager>();
            if (breathInput == null)
            {
                Debug.LogError("VFXBreathBridge: BreathInputManager not found!");
            }
        }

        // Auto-find VFX if not assigned
        if (oceanVFX == null)
        {
            oceanVFX = GetComponent<VisualEffect>();
        }

        // Initialize with calm state
        ApplyBreathToVFX(0f);
    }

    private void Update()
    {
        if (breathInput == null || oceanVFX == null) return;

        // Get current breath value
        float currentBreath = breathInput.BreathValue;

        // Smooth the value
        smoothedBreathValue = Mathf.Lerp(smoothedBreathValue, currentBreath, smoothingSpeed);

        // Apply to VFX
        ApplyBreathToVFX(smoothedBreathValue);

        // Handle bubbles
        HandleBubbles(smoothedBreathValue);
    }

    private void ApplyBreathToVFX(float breathValue)
    {
        // Set the main breath intensity parameter
        if (oceanVFX.HasFloat(breathIntensityParam))
        {
            oceanVFX.SetFloat(breathIntensityParam, breathValue);
        }

        // Calculate mapped values
        float turbStrength = Mathf.Lerp(calmTurbulenceStrength, chaosTurbulenceStrength, breathValue);
        float turbFrequency = Mathf.Lerp(calmTurbulenceFrequency, chaosTurbulenceFrequency, breathValue);
        float drag = Mathf.Lerp(calmDrag, chaosDrag, breathValue);

        // Set turbulence parameters
        if (oceanVFX.HasFloat(turbulenceStrengthParam))
        {
            oceanVFX.SetFloat(turbulenceStrengthParam, turbStrength);
        }

        if (oceanVFX.HasFloat(turbulenceFrequencyParam))
        {
            oceanVFX.SetFloat(turbulenceFrequencyParam, turbFrequency);
        }

        if (oceanVFX.HasFloat(dragParam))
        {
            oceanVFX.SetFloat(dragParam, drag);
        }
    }

    private void HandleBubbles(float breathValue)
    {
        if (bubbleVFX == null) return;

        if (breathValue > bubbleThreshold)
        {
            if (!bubbleVFX.gameObject.activeInHierarchy)
            {
                bubbleVFX.gameObject.SetActive(true);
            }

            // Map breath to bubble rate
            float bubbleIntensity = (breathValue - bubbleThreshold) / (1f - bubbleThreshold);
            
            if (bubbleVFX.HasFloat("SpawnRate"))
            {
                bubbleVFX.SetFloat("SpawnRate", bubbleIntensity * maxBubbleRate);
            }
        }
        else
        {
            if (bubbleVFX.gameObject.activeInHierarchy && bubbleVFX.aliveParticleCount == 0)
            {
                bubbleVFX.gameObject.SetActive(false);
            }
        }
    }

    // ============ PUBLIC API ============

    /// <summary>
    /// Manually set breath intensity (for testing)
    /// </summary>
    public void SetBreathIntensity(float value)
    {
        smoothedBreathValue = Mathf.Clamp01(value);
        ApplyBreathToVFX(smoothedBreathValue);
    }

    /// <summary>
    /// Reset to calm state
    /// </summary>
    public void ResetToCalm()
    {
        smoothedBreathValue = 0f;
        ApplyBreathToVFX(0f);
    }
}
