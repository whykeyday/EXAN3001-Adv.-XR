using UnityEngine;

/// <summary>
/// Cat petting interaction - Detects hand movement over the cat's head
/// and plays purring audio with gentle particle vibration.
/// </summary>
[RequireComponent(typeof(Collider))]
public class CatInteract : MonoBehaviour
{
    // ============ REFERENCES ============
    [Header("References")]
    [Tooltip("The cat's particle system (for vibration effect)")]
    public ParticleSystem catParticles;

    [Tooltip("AudioSource for purring sound")]
    public AudioSource purrAudio;

    // ============ PETTING DETECTION ============
    [Header("Petting Detection")]
    [Tooltip("Minimum hand velocity to count as petting (m/s)")]
    public float minPettingVelocity = 0.1f;

    [Tooltip("How quickly the purr builds up")]
    public float purrBuildupRate = 2f;

    [Tooltip("How quickly the purr fades out")]
    public float purrFadeRate = 1.5f;

    // ============ PURR AUDIO SETTINGS ============
    [Header("Purr Audio")]
    [Tooltip("Maximum purr volume")]
    [Range(0f, 1f)]
    public float maxPurrVolume = 0.8f;

    [Tooltip("Base pitch of purr")]
    public float basePitch = 0.9f;

    [Tooltip("Pitch increase when petting intensifies")]
    public float maxPitch = 1.1f;

    // ============ VIBRATION SETTINGS ============
    [Header("Particle Vibration")]
    [Tooltip("Noise strength when purring")]
    public float purrNoiseStrength = 0.03f;

    [Tooltip("Noise frequency when purring")]
    public float purrNoiseFrequency = 3f;

    // ============ STATE ============
    [Header("State (Read Only)")]
    [Range(0f, 1f)]
    public float purrLevel = 0f;

    private bool handInside = false;
    private Vector3 lastHandPosition;
    private Transform currentHand;
    private float handVelocity = 0f;
    private ParticleSystem.NoiseModule noiseModule;

    private void Start()
    {
        // Ensure collider is trigger
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        // Cache noise module
        if (catParticles != null)
        {
            noiseModule = catParticles.noise;
        }

        // Setup audio
        if (purrAudio != null)
        {
            purrAudio.loop = true;
            purrAudio.volume = 0f;
            purrAudio.playOnAwake = false;
        }

        // Start with no purr
        purrLevel = 0f;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform.IsChildOf(transform.root)) return;
        handInside = true;
        currentHand = other.transform;
        lastHandPosition = currentHand.position;
        Debug.Log("CatInteract: Hand entered!");
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.transform.IsChildOf(transform.root)) return;
        handInside = true;
        currentHand = other.transform;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.transform.IsChildOf(transform.root)) return;
        handInside = false;
        currentHand = null;
        Debug.Log("CatInteract: Hand left.");
    }

    private void Update()
    {
        // Calculate hand velocity
        if (handInside && currentHand != null)
        {
            Vector3 currentPos = currentHand.position;
            handVelocity = (currentPos - lastHandPosition).magnitude / Time.deltaTime;
            lastHandPosition = currentPos;

            // Check if petting (hand moving fast enough)
            if (handVelocity > minPettingVelocity)
            {
                // Build up purr
                purrLevel += purrBuildupRate * Time.deltaTime;
                purrLevel = Mathf.Clamp01(purrLevel);
            }
            else
            {
                // Hand inside but not moving = slow fade
                purrLevel -= purrFadeRate * 0.3f * Time.deltaTime;
                purrLevel = Mathf.Clamp01(purrLevel);
            }
        }
        else
        {
            // Hand not inside = normal fade
            purrLevel -= purrFadeRate * Time.deltaTime;
            purrLevel = Mathf.Clamp01(purrLevel);
        }

        // Apply purr effects
        ApplyPurrEffects(purrLevel);
    }

    private void ApplyPurrEffects(float purr)
    {
        // Audio feedback
        if (purrAudio != null)
        {
            if (purr > 0.01f && !purrAudio.isPlaying)
            {
                purrAudio.Play();
            }
            else if (purr <= 0.01f && purrAudio.isPlaying)
            {
                purrAudio.Stop();
            }

            purrAudio.volume = Mathf.Lerp(0f, maxPurrVolume, purr);
            purrAudio.pitch = Mathf.Lerp(basePitch, maxPitch, purr);
        }

        // Particle vibration
        if (catParticles != null)
        {
            noiseModule.enabled = purr > 0.01f;

            if (purr > 0.01f)
            {
                // Gentle vibration that increases with purr level
                noiseModule.strength = Mathf.Lerp(0.01f, purrNoiseStrength, purr);
                noiseModule.frequency = Mathf.Lerp(1f, purrNoiseFrequency, purr);
            }
        }
    }

    // ============ PUBLIC API ============

    /// <summary>
    /// Check if the cat is currently being petted
    /// </summary>
    public bool IsBeingPetted()
    {
        return purrLevel > 0.3f;
    }

    /// <summary>
    /// Get the current purr intensity (0-1)
    /// </summary>
    public float GetPurrLevel()
    {
        return purrLevel;
    }

    /// <summary>
    /// Force start purring (for testing)
    /// </summary>
    public void StartPurring()
    {
        purrLevel = 1f;
        ApplyPurrEffects(1f);
    }

    /// <summary>
    /// Force stop purring
    /// </summary>
    public void StopPurring()
    {
        purrLevel = 0f;
        ApplyPurrEffects(0f);
    }
}
