using UnityEngine;

/// <summary>
/// Reads BreathValue from BreathInputManager and maps it to visual changes in the scene.
/// Demonstrates the "Interactive System" for breathing-driven environment effects.
/// </summary>
public class BreathReactor : MonoBehaviour
{
    [Header("Reference")]
    [Tooltip("Drag the BreathInputManager component here.")]
    public BreathInputManager breathInput;

    [Header("Fog Effect (Ocean Mood)")]
    public bool enableFog = true;
    [Tooltip("If true, higher breath = MORE fog. If false, higher breath = LESS fog.")]
    public bool invertFog = false;
    [Range(0f, 0.1f)]
    public float minFogDensity = 0.01f;
    [Range(0f, 0.1f)]
    public float maxFogDensity = 0.05f;

    [Header("Growth Effect (Forest)")]
    [Tooltip("Assign the object (e.g., tree) to scale with breath.")]
    public Transform targetObject;
    public Vector3 minScale = Vector3.one;
    public Vector3 maxScale = new Vector3(1.5f, 1.5f, 1.5f);

    [Header("Light Effect (Pulse)")]
    [Tooltip("Assign a scene light to pulse with breath.")]
    public Light sceneLight;
    public float minIntensity = 0.5f;
    public float maxIntensity = 2f;

    void Start()
    {
        // Enable fog in render settings if using fog effect
        if (enableFog)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
        }
    }

    void Update()
    {
        if (breathInput == null)
        {
            Debug.LogWarning("BreathReactor: BreathInputManager reference is missing!");
            return;
        }

        float breath = breathInput.BreathValue;

        // 1. Fog Effect (Ocean Mood)
        if (enableFog)
        {
            float fogValue = Mathf.Lerp(minFogDensity, maxFogDensity, breath);
            if (invertFog)
            {
                fogValue = Mathf.Lerp(maxFogDensity, minFogDensity, breath);
            }
            RenderSettings.fogDensity = fogValue;
        }

        // 2. Growth Effect (Forest)
        if (targetObject != null)
        {
            targetObject.localScale = Vector3.Lerp(minScale, maxScale, breath);
        }

        // 3. Light Effect (Pulse)
        if (sceneLight != null)
        {
            sceneLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, breath);
        }
    }
}
