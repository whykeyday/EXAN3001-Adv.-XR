using UnityEngine;

/// <summary>
/// Attach to coral particle objects.
/// When player touches the coral, releases yellow particles.
/// </summary>
[RequireComponent(typeof(Collider))]
public class CoralInteraction : MonoBehaviour
{
    [Header("Effect Settings")]
    [Tooltip("Particle system that emits yellow particles on touch")]
    public ParticleSystem releaseEffect;

    [Tooltip("Number of particles to emit on each touch")]
    public int particlesToEmit = 30;

    [Tooltip("Cooldown between emissions (seconds)")]
    public float cooldown = 0.5f;

    [Header("Auto-Create Effect")]
    [Tooltip("If true, automatically creates the release effect if not assigned")]
    public bool autoCreateEffect = true;

    private float lastEmitTime = -999f;

    private void Start()
    {
        // Ensure collider is trigger
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        // Auto-create release effect if needed
        if (releaseEffect == null && autoCreateEffect)
        {
            CreateReleaseEffect();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check for player hand
        if (other.CompareTag("PlayerHand"))
        {
            TryEmitParticles();
        }
    }

    private void OnTriggerStay(Collider other)
    {
        // Continuous emission while hand is in contact
        if (other.CompareTag("PlayerHand"))
        {
            TryEmitParticles();
        }
    }

    private void TryEmitParticles()
    {
        if (releaseEffect == null) return;

        // Check cooldown
        if (Time.time - lastEmitTime < cooldown) return;

        lastEmitTime = Time.time;

        // Emit particles at contact point
        releaseEffect.Emit(particlesToEmit);
        Debug.Log($"CoralInteraction: Released {particlesToEmit} particles!");
    }

    private void CreateReleaseEffect()
    {
        // Create a child object for the release effect
        GameObject effectObj = new GameObject("ReleaseEffect");
        effectObj.transform.SetParent(transform);
        effectObj.transform.localPosition = Vector3.zero;

        ParticleSystem ps = effectObj.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psr = effectObj.GetComponent<ParticleSystemRenderer>();

        var main = ps.main;
        main.startSize = new ParticleSystem.MinMaxCurve(0.015f, 0.03f); // Varying bubble sizes
        main.startSpeed = 0.3f; // Gentle upward drift
        main.startLifetime = 3f; // Longer lifetime for floating
        main.maxParticles = 200;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.loop = false;
        main.playOnAwake = false;
        main.gravityModifier = -0.05f; // Negative = float upward!

        // Light yellow / pale gold color (淡黄色)
        main.startColor = new Color(1f, 0.98f, 0.7f, 0.8f);

        // No continuous emission - only emit via code
        var emission = ps.emission;
        emission.rateOverTime = 0f;

        // Small spawn area around coral
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.15f;

        // Float upward like bubbles
        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.y = new ParticleSystem.MinMaxCurve(0.2f, 0.4f); // Upward velocity

        // Gentle side-to-side wobble (like bubbles)
        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.08f;
        noise.frequency = 0.5f;
        noise.scrollSpeed = 0.3f;
        noise.separateAxes = true;
        noise.strengthX = 0.1f;
        noise.strengthY = 0.02f; // Less vertical wobble
        noise.strengthZ = 0.1f;

        // Size varies slightly over lifetime
        var sizeOverLife = ps.sizeOverLifetime;
        sizeOverLife.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.8f);
        sizeCurve.AddKey(0.5f, 1f);
        sizeCurve.AddKey(1f, 0.3f);
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // Fade out gently
        var colorOverLife = ps.colorOverLifetime;
        colorOverLife.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 0.98f, 0.7f), 0f),   // Light yellow
                new GradientColorKey(new Color(1f, 1f, 0.85f), 0.5f),  // Even lighter
                new GradientColorKey(new Color(1f, 1f, 0.9f), 1f)      // Almost white
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),      // Fade in
                new GradientAlphaKey(0.7f, 0.2f), // Visible
                new GradientAlphaKey(0.7f, 0.7f), // Stay visible
                new GradientAlphaKey(0f, 1f)      // Fade out
            }
        );
        colorOverLife.color = gradient;

        // Simple material
        Shader shader = Shader.Find("Particles/Standard Unlit");
        if (shader != null)
        {
            Material mat = new Material(shader);
            mat.SetFloat("_Mode", 2); // Fade mode for transparency
            psr.material = mat;
        }

        releaseEffect = ps;
    }
}
