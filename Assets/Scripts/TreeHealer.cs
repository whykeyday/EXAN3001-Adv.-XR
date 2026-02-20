using UnityEngine;

/// <summary>
/// Tree healing interaction - Tree starts withered (brown, low particles)
/// and becomes alive (green/gold, high particles) when touched.
/// Energy particles fly from hand to tree.
/// </summary>
public class TreeHealer : MonoBehaviour
{
    // ============ REFERENCES ============
    [Header("References")]
    [Tooltip("The main tree particle system to heal")]
    public ParticleSystem treeParticles;

    [Tooltip("Transform marking the center of the tree")]
    public Transform treeCenter;

    [Tooltip("Transform of the player's hand (auto-found if tagged 'PlayerHand')")]
    public Transform playerHand;

    [Tooltip("Energy particles that fly from hand to tree")]
    public ParticleSystem energyParticles;

    // ============ HEALING SETTINGS ============
    [Header("Healing Settings")]
    [Tooltip("Distance threshold to start healing (meters)")]
    public float healingDistance = 0.5f;

    [Tooltip("How fast energy level increases when close")]
    public float healingRate = 0.3f;

    [Tooltip("How fast energy decays when not healing")]
    public float decayRate = 0.1f;

    // ============ TREE APPEARANCE ============
    [Header("Tree Appearance - Withered (Start)")]
    public Color witheredColor = new Color(0.4f, 0.25f, 0.1f, 0.9f); // Brown
    public float witheredEmissionRate = 20f;
    public float witheredSize = 0.03f;

    [Header("Tree Appearance - Alive (Healed)")]
    public Color aliveColor = new Color(0.3f, 0.9f, 0.3f, 0.95f); // Green
    public Color goldHighlight = new Color(1f, 0.85f, 0.3f, 1f);  // Gold accent
    public float aliveEmissionRate = 150f;
    public float aliveSize = 0.05f;

    // ============ STATE ============
    [Header("State (Read Only)")]
    [Range(0f, 1f)]
    public float energyLevel = 0f;

    private bool isHealing = false;
    private ParticleSystem.MainModule treeMain;
    private ParticleSystem.EmissionModule treeEmission;

    private void Start()
    {
        // Auto-find tree center if not assigned
        if (treeCenter == null)
        {
            treeCenter = transform;
        }

        // Cache particle modules
        if (treeParticles != null)
        {
            treeMain = treeParticles.main;
            treeEmission = treeParticles.emission;
        }

        // Auto-create energy particles if not assigned
        if (energyParticles == null)
        {
            CreateEnergyParticles();
        }

        // Start in withered state
        energyLevel = 0f;
        ApplyTreeState(0f);
    }

    private void Update()
    {
        // Try to find player hand if not assigned
        if (playerHand == null)
        {
            GameObject handObj = GameObject.FindGameObjectWithTag("PlayerHand");
            if (handObj != null)
            {
                playerHand = handObj.transform;
            }
            else
            {
                return; // No hand found
            }
        }

        // Calculate distance to tree
        float distance = Vector3.Distance(playerHand.position, treeCenter.position);

        // Check if within healing range
        if (distance < healingDistance)
        {
            // Increase energy
            energyLevel += healingRate * Time.deltaTime;
            energyLevel = Mathf.Clamp01(energyLevel);

            if (!isHealing)
            {
                isHealing = true;
                StartHealing();
            }
        }
        else
        {
            // Decay energy slowly when not healing
            energyLevel -= decayRate * Time.deltaTime;
            energyLevel = Mathf.Clamp01(energyLevel);

            if (isHealing)
            {
                isHealing = false;
                StopHealing();
            }
        }

        // Apply visual changes based on energy level
        ApplyTreeState(energyLevel);
    }

    private void StartHealing()
    {
        Debug.Log("TreeHealer: Healing started!");

        // Start energy particles flying to tree
        if (energyParticles != null)
        {
            energyParticles.gameObject.SetActive(true);
            energyParticles.Play();
        }
    }

    private void StopHealing()
    {
        Debug.Log("TreeHealer: Healing stopped.");

        // Stop energy particles
        if (energyParticles != null)
        {
            energyParticles.Stop();
        }
    }

    private void ApplyTreeState(float energy)
    {
        if (treeParticles == null) return;

        // Interpolate color: Brown -> Green -> Gold highlight at max
        Color currentColor;
        if (energy < 0.7f)
        {
            // Brown to Green
            currentColor = Color.Lerp(witheredColor, aliveColor, energy / 0.7f);
        }
        else
        {
            // Green to Gold highlight
            float t = (energy - 0.7f) / 0.3f;
            currentColor = Color.Lerp(aliveColor, goldHighlight, t * 0.5f);
        }

        treeMain.startColor = currentColor;

        // Interpolate emission rate
        float currentRate = Mathf.Lerp(witheredEmissionRate, aliveEmissionRate, energy);
        treeEmission.rateOverTime = currentRate;

        // Interpolate particle size
        float currentSize = Mathf.Lerp(witheredSize, aliveSize, energy);
        treeMain.startSize = currentSize;
    }

    private void CreateEnergyParticles()
    {
        GameObject effectObj = new GameObject("EnergyParticles");
        effectObj.transform.SetParent(transform);
        effectObj.transform.localPosition = Vector3.zero;
        effectObj.SetActive(false);

        ParticleSystem ps = effectObj.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psr = effectObj.GetComponent<ParticleSystemRenderer>();

        var main = ps.main;
        main.startSize = new ParticleSystem.MinMaxCurve(0.01f, 0.025f);
        main.startSpeed = 2f;
        main.startLifetime = 0.8f;
        main.maxParticles = 100;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.loop = true;

        // Golden energy color
        main.startColor = new Color(1f, 0.9f, 0.4f, 1f);

        var emission = ps.emission;
        emission.rateOverTime = 30f;

        // Emit from small sphere (will be positioned at hand)
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.05f;

        // Fly towards tree center (needs custom attraction script or use sub-emitter)
        // For now, just emit upward towards tree direction
        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;

        // Size shrinks
        var sizeOverLife = ps.sizeOverLifetime;
        sizeOverLife.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 1f);
        sizeCurve.AddKey(1f, 0.2f);
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // Fade out
        var colorOverLife = ps.colorOverLifetime;
        colorOverLife.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 0.9f, 0.4f), 0f),
                new GradientColorKey(new Color(0.3f, 0.9f, 0.3f), 1f) // Turns green as it reaches tree
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLife.color = gradient;

        Shader shader = Shader.Find("Particles/Standard Unlit");
        if (shader != null)
        {
            psr.material = new Material(shader);
        }

        energyParticles = ps;
    }

    private void LateUpdate()
    {
        // Position energy particles at player hand
        if (energyParticles != null && playerHand != null && isHealing)
        {
            energyParticles.transform.position = playerHand.position;

            // Point towards tree center
            Vector3 dirToTree = (treeCenter.position - playerHand.position).normalized;
            if (dirToTree.magnitude > 0.01f)
            {
                energyParticles.transform.rotation = Quaternion.LookRotation(dirToTree);
            }
        }
    }

    // ============ PUBLIC API ============

    /// <summary>
    /// Instantly fully heal the tree
    /// </summary>
    public void FullyHeal()
    {
        energyLevel = 1f;
        ApplyTreeState(1f);
        Debug.Log("TreeHealer: Tree fully healed!");
    }

    /// <summary>
    /// Reset tree to withered state
    /// </summary>
    public void ResetToWithered()
    {
        energyLevel = 0f;
        ApplyTreeState(0f);
        Debug.Log("TreeHealer: Tree reset to withered.");
    }
}
