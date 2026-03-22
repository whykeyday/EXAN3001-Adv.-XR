using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Tree healing interaction - Tree starts withered (brown, low particles)
/// and becomes alive (green/gold, high particles) when touched.
/// Energy particles fly from hand to tree.
/// </summary>
public class TreeHealer : MonoBehaviour
{
    // ============ REFERENCES ============
    [Header("References")]
    public ParticleSystem treeParticles;
    public Transform treeCenter;
    public Transform playerHand;
    public ParticleSystem energyParticles;

    [Header("New Effects (Auto-generated if null)")]
    public ParticleSystem yellowScarfParticles;
    public ParticleSystem pinkPetals;
    public ParticleSystem butterflyParticles;
    public ParticleSystem soilParticles;

    [Header("Textures")]
    [Tooltip("Drag Assets/tree/butterflies.png here")]
    public Texture2D butterflyTexture;

    [Header("Audio (Placeholders)")]
    public AudioSource birdAudio;
    public AudioSource chimeAudio;

    // ============ HEALING SETTINGS ============
    [Header("Healing Settings")]
    public float healingDistance = 0.5f;
    public float healingRate = 0.05f; // SLOWED DOWN: ~20 seconds to fully heal
    public float decayRate = 0.02f;

    // ============ TREE APPEARANCE ============
    [Header("Tree Appearance - Withered (Start)")]
    public Color witheredColor = new Color(0.2f, 0.15f, 0.02f, 0.9f); // Darker Brown
    public float witheredEmissionRate = 20f;
    public float witheredSize = 0.04f;

    [Header("Tree Appearance - Alive (Healed)")]
    public Color aliveColor = new Color(0.1f, 0.9f, 0.2f, 0.95f); // Bright Emerald Green
    public Color goldHighlight = new Color(1f, 0.85f, 0.3f, 1f);  
    public float aliveEmissionRate = 120f;
    public float aliveSize = 0.08f;

    // ============ STATE ============
    [Header("State (Read Only)")]
    [Range(0f, 1f)]
    public float energyLevel = 0f;

    private bool isHealing = false;
    private ParticleSystem.MainModule treeMain;
    private ParticleSystem.EmissionModule treeEmission;
    
    private float lastTouchTime = -1f;
    private List<Renderer> treeRenderers = new List<Renderer>();
    private bool fullyHealedTriggered = false;

    private void Start()
    {
        if (treeCenter == null) treeCenter = transform;

        CreateMissingEffects();

        if (treeParticles != null)
        {
            treeMain = treeParticles.main;
            treeEmission = treeParticles.emission;
            
            // Fix tree particles material to make them glow instead of default squares
            var psr = treeParticles.GetComponent<ParticleSystemRenderer>();
            if (psr != null && psr.sharedMaterial != null && psr.sharedMaterial.name == "Default-Material")
            {
                psr.material = ParticleUtils.GetGlowingSphereMaterial();
            }
            
            // Add noise for "leaves swaying in the wind"
            var noise = treeParticles.noise;
            noise.enabled = true;
            noise.strength = 0.15f;
            noise.frequency = 0.5f;
            noise.scrollSpeed = 0.2f;
        }

        energyLevel = 0f;
        ApplyTreeState(0f);
    }

    private void CreateMissingEffects()
    {
        if (energyParticles == null) CreateEnergyParticles();
        if (yellowScarfParticles == null) CreateYellowScarf();
        if (pinkPetals == null) CreatePinkPetals();
        if (butterflyParticles == null) CreateButterflies();
        if (soilParticles == null) CreateSoil();
    }

    private void Update()
    {
        if (playerHand == null)
        {
            GameObject handObj = GameObject.FindGameObjectWithTag("PlayerHand");
            if (handObj != null) playerHand = handObj.transform;
        }

        if (playerHand != null)
        {
            float distance = Vector3.Distance(playerHand.position, treeCenter.position);
            if (distance < healingDistance) ReceiveTouch();
        }
        
        if (Time.time - lastTouchTime > 0.2f)
        {
            energyLevel -= decayRate * Time.deltaTime;
            energyLevel = Mathf.Clamp01(energyLevel);

            if (isHealing)
            {
                isHealing = false;
                StopHealing();
            }
        }

        ApplyTreeState(energyLevel);

        // Transition logic: Yellow scarf swirls while healing
        if (energyLevel > 0f && energyLevel < 0.99f && isHealing)
        {
            if (yellowScarfParticles != null && !yellowScarfParticles.isPlaying) yellowScarfParticles.Play();
        }
        else
        {
            if (yellowScarfParticles != null && yellowScarfParticles.isPlaying) yellowScarfParticles.Stop();
        }

        // Fully Healed Triggers
        if (energyLevel >= 1.0f && !fullyHealedTriggered)
        {
            fullyHealedTriggered = true;
            TriggerFullyHealedEvents();
        }
        else if (energyLevel < 0.9f && fullyHealedTriggered)
        {
            fullyHealedTriggered = false;
            if (pinkPetals != null) pinkPetals.Stop();
            if (butterflyParticles != null) butterflyParticles.Stop();
        }
    }

    private void TriggerFullyHealedEvents()
    {
        if (pinkPetals != null) pinkPetals.Play();
        if (butterflyParticles != null) butterflyParticles.Play();
        
        if (birdAudio != null) birdAudio.Play();
        else Debug.Log("[Placeholder] Bird Audio plays here. User: please attach AudioSource with bird clip.");

        if (chimeAudio != null) chimeAudio.Play();
        else Debug.Log("[Placeholder] Wind Chime Audio plays here. User: please attach AudioSource with chime clip.");
    }

    private void StartHealing()
    {
        if (energyParticles != null) { energyParticles.gameObject.SetActive(true); energyParticles.Play(); }
    }

    private void StopHealing()
    {
        if (energyParticles != null) energyParticles.Stop();
    }

    private void ApplyTreeState(float energy)
    {
        if (treeParticles == null) return;

        Color currentColor;
        if (energy < 0.7f) currentColor = Color.Lerp(witheredColor, aliveColor, energy / 0.7f);
        else currentColor = Color.Lerp(aliveColor, goldHighlight, (energy - 0.7f) / 0.3f * 0.5f);

        treeMain.startColor = currentColor;
        treeEmission.rateOverTime = Mathf.Lerp(witheredEmissionRate, aliveEmissionRate, energy);
        treeMain.startSize = Mathf.Lerp(witheredSize, aliveSize, energy);
        
        foreach (var r in treeRenderers)
        {
            if (r != null && r.material != null)
            {
                if (r.material.HasProperty("_BaseColor")) r.material.SetColor("_BaseColor", currentColor);
                else r.material.color = currentColor;
                
                if (r.material.IsKeywordEnabled("_EMISSION"))
                {
                    r.material.SetColor("_EmissionColor", currentColor * Mathf.Lerp(1.5f, 6.0f, energy));
                }
            }
        }
    }

    public void AddTreeRenderer(Renderer r)
    {
        if (r != null && !treeRenderers.Contains(r)) treeRenderers.Add(r);
    }

    public void ReceiveTouch()
    {
        lastTouchTime = Time.time;
        energyLevel += healingRate * Time.deltaTime;
        energyLevel = Mathf.Clamp01(energyLevel);

        if (!isHealing)
        {
            isHealing = true;
            StartHealing();
        }
    }

    private void CreateEnergyParticles()
    {
        GameObject effectObj = new GameObject("EnergyParticles");
        effectObj.transform.SetParent(transform);
        effectObj.transform.localPosition = Vector3.zero;
        effectObj.SetActive(false);
        energyParticles = effectObj.AddComponent<ParticleSystem>();
        
        var main = energyParticles.main;
        main.startSize = new ParticleSystem.MinMaxCurve(0.01f, 0.025f);
        main.startSpeed = 2f;
        main.startLifetime = 0.8f;
        main.maxParticles = 100;
        main.startColor = new Color(1f, 0.9f, 0.4f, 1f);

        var shape = energyParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.05f;

        var velocity = energyParticles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        
        energyParticles.GetComponent<ParticleSystemRenderer>().material = ParticleUtils.GetGlowingSphereMaterial();
    }

    private void CreateYellowScarf()
    {
        GameObject scarf = new GameObject("YellowScarfParticles");
        scarf.transform.SetParent(transform);
        scarf.transform.localPosition = Vector3.up * 0.5f; // Center of trunk

        yellowScarfParticles = scarf.AddComponent<ParticleSystem>();
        var main = yellowScarfParticles.main;
        main.loop = true;
        main.startLifetime = 2.5f;
        main.startSpeed = 0f;
        main.startSize = 0.06f;
        main.startColor = new Color(1f, 0.9f, 0.2f, 1f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = yellowScarfParticles.emission;
        emission.rateOverTime = 80f; // Dense to look like a scarf

        var shape = yellowScarfParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.6f;      
        shape.arcMode = ParticleSystemShapeMultiModeValue.Loop;
        
        var vel = yellowScarfParticles.velocityOverLifetime;
        vel.enabled = true;
        vel.orbitalY = 3.0f; // Spin around Y
        vel.y = new ParticleSystem.MinMaxCurve(0.1f, 0.35f); // Float up
        
        var colorOL = yellowScarfParticles.colorOverLifetime;
        colorOL.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(0f, 1f) }
        );
        colorOL.color = grad;

        yellowScarfParticles.GetComponent<ParticleSystemRenderer>().material = ParticleUtils.GetGlowingSphereMaterial();
        yellowScarfParticles.Stop();
    }

    private void CreatePinkPetals()
    {
        GameObject petals = new GameObject("PinkPetals");
        petals.transform.SetParent(transform);
        petals.transform.localPosition = Vector3.up * 1.5f; // Top of tree

        pinkPetals = petals.AddComponent<ParticleSystem>();
        var main = pinkPetals.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.06f);
        main.startColor = new Color(1f, 0.6f, 0.8f, 0.9f); // Pinkish glowing

        var emission = pinkPetals.emission;
        emission.rateOverTime = 40f;

        var shape = pinkPetals.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 1.2f;

        var force = pinkPetals.forceOverLifetime;
        force.enabled = true;
        force.y = -0.1f; // Gravity fall

        var noise = pinkPetals.noise;
        noise.enabled = true;
        noise.strength = 0.05f;
        noise.frequency = 0.5f;
        
        pinkPetals.GetComponent<ParticleSystemRenderer>().material = ParticleUtils.GetGlowingSphereMaterial();
        pinkPetals.Stop();
    }

    private void CreateButterflies()
    {
        GameObject bf = new GameObject("ButterflyParticles");
        bf.transform.SetParent(transform);
        bf.transform.localPosition = Vector3.up * 0.8f;

        butterflyParticles = bf.AddComponent<ParticleSystem>();
        var main = butterflyParticles.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
        main.startSize = 0.15f; 
        
        var emission = butterflyParticles.emission;
        emission.rateOverTime = 3f;

        var shape = butterflyParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 1.5f;

        var vel = butterflyParticles.velocityOverLifetime;
        vel.enabled = true;
        vel.orbitalY = new ParticleSystem.MinMaxCurve(-0.8f, 0.8f); // flutter

        var texAnim = butterflyParticles.textureSheetAnimation;
        texAnim.enabled = true;
        texAnim.numTilesX = 2; // Basic 2x2 sprite sheet assumption
        texAnim.numTilesY = 2;
        texAnim.animation = ParticleSystemAnimationType.WholeSheet;

        var psr = butterflyParticles.GetComponent<ParticleSystemRenderer>();
        var mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit"));
        mat.SetFloat("_Surface", 1.0f);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        
        if (butterflyTexture != null) mat.mainTexture = butterflyTexture;
        else Debug.LogWarning("TreeHealer: Please assign 'butterflyTexture' in the inspector.");
        
        psr.material = mat;
        butterflyParticles.Stop();
    }

    private void CreateSoil()
    {
        GameObject soil = new GameObject("SoilParticles");
        soil.transform.SetParent(transform);
        soil.transform.localPosition = Vector3.down * 0.1f;

        soilParticles = soil.AddComponent<ParticleSystem>();
        var main = soilParticles.main;
        main.loop = true;
        main.startLifetime = 3f;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.08f);
        main.startColor = new Color(0.25f, 0.15f, 0.05f, 0.8f);

        var emission = soilParticles.emission;
        emission.rateOverTime = 30f;

        var shape = soilParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 1.0f;

        var vel = soilParticles.velocityOverLifetime;
        vel.enabled = true;
        vel.y = new ParticleSystem.MinMaxCurve(-0.01f, 0.02f); // very slight float

        soilParticles.GetComponent<ParticleSystemRenderer>().material = ParticleUtils.GetGlowingSphereMaterial();
        soilParticles.Play(); 
    }

    private void LateUpdate()
    {
        if (energyParticles != null && playerHand != null && isHealing)
        {
            energyParticles.transform.position = playerHand.position;
            Vector3 dirToTree = (treeCenter.position - playerHand.position).normalized;
            if (dirToTree.magnitude > 0.01f)
                energyParticles.transform.rotation = Quaternion.LookRotation(dirToTree);
        }
    }

    public void FullyHeal() { energyLevel = 1f; ApplyTreeState(1f); }
    public void ResetToWithered() { energyLevel = 0f; ApplyTreeState(0f); fullyHealedTriggered = false; }
}
