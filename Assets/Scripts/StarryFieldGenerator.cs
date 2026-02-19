using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(ParticleSystem))]
public class StarryFieldGenerator : MonoBehaviour
{
    [Header("Settings")]
    public int maxParticles = 600; 
    public Vector3 boxSize = new Vector3(20, 8, 20); 
    
    // Size Logic: Medium is most common
    public float starSizeMin = 0.02f;
    public float starSizeMax = 0.6f; // User requested 0.6 max
    
    [Header("Exclusion Zones")]
    [Tooltip("Particles inside these areas will be removed.")]
    public List<Transform> exclusionZones = new List<Transform>();
    public float exclusionRadius = 2.0f; // Radius around shards to clear

    [Header("Visuals")]
    public List<Material> particleMaterials = new List<Material>();

    private ParticleSystem[] _systems;
    private ParticleSystem.Particle[] _particles;

    void Start()
    {
        // cleanup legacy
        var children = GetComponentsInChildren<Transform>();
        foreach (var child in children)
        {
            if (child != transform && (child.name.Contains("StarryField_Visuals") || child.name.StartsWith("StarryLayer_")))
            {
                Destroy(child.gameObject);
            }
        }
        
        // Auto-find Shards if list is empty
        if (exclusionZones.Count == 0)
        {
            var renderers = FindObjectsOfType<Renderer>();
            foreach(var r in renderers)
            {
                // Simple heuristic: if name contains "Shard" or "Glass", avoid it
                if (r.name.Contains("Shard") || r.name.Contains("Glass"))
                {
                    exclusionZones.Add(r.transform);
                }
            }
        }

        SetupStarryField();
    }

    void SetupStarryField()
    {
        // Materials setup
        List<Material> materialsToUse = new List<Material>();
        foreach(var m in particleMaterials) if(m != null) materialsToUse.Add(m);
        if (materialsToUse.Count == 0) materialsToUse.Add(CreateDefaultMaterial());

        int layerCount = materialsToUse.Count;
        int countPerLayer = Mathf.Max(1, maxParticles / layerCount);
        
        _systems = new ParticleSystem[layerCount];

        for (int i = 0; i < layerCount; i++)
        {
            ParticleSystem ps;
            if (i == 0) ps = GetComponent<ParticleSystem>();
            else
            {
                GameObject childGO = new GameObject($"StarryLayer_{i}");
                childGO.transform.parent = transform;
                childGO.transform.localPosition = Vector3.zero;
                childGO.transform.localScale = Vector3.one;
                ps = childGO.AddComponent<ParticleSystem>();
            }
            _systems[i] = ps;
            ConfigureSystem(ps, materialsToUse[i], countPerLayer);
        }
    }

    void ConfigureSystem(ParticleSystem ps, Material mat, int count)
    {
        // IMPORTANT: Stop before config to prevent "Cannot set duration while playing" errors
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        
        ParticleSystemRenderer psr = ps.GetComponent<ParticleSystemRenderer>();
        if (psr == null) psr = ps.gameObject.AddComponent<ParticleSystemRenderer>();
        
        psr.renderMode = ParticleSystemRenderMode.Billboard;
        if (mat != null) { psr.material = mat; psr.trailMaterial = mat; }

        var main = ps.main;
        main.duration = 60.0f;
        main.loop = true;
        main.prewarm = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(10.0f, 60.0f); // Longer life
        main.startSpeed = 0.0f;
        
        // SIZE DISTRIBUTION: Medium is common
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0.0f, 0.05f); // Tiny 
        sizeCurve.AddKey(0.15f, 0.4f); // Medium-Large 
        // Use normalized curve scaled by max
        main.startSize = new ParticleSystem.MinMaxCurve(starSizeMax, sizeCurve);

        // EXTRA BRIGHTNESS: Use HDR color or full Alpha
        // Multiplied by intensity for Bloom
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 1f, 1f, 0.6f) * 1.5f, // Dim stars
            new Color(1f, 1f, 1f, 1.0f) * 3.0f  // Very bright stars
        );
        main.maxParticles = count;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.cullingMode = ParticleSystemCullingMode.Automatic;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;

        var emission = ps.emission;
        emission.rateOverTime = new ParticleSystem.MinMaxCurve(count / 10.0f); // Slower emission

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = boxSize;

        // VELOCITY: ALMOST STATIC
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        // Extremely tiny drift, effectively static
        vel.x = new ParticleSystem.MinMaxCurve(-0.002f, 0.002f);
        vel.y = new ParticleSystem.MinMaxCurve(-0.001f, 0.001f);
        vel.z = new ParticleSystem.MinMaxCurve(-0.002f, 0.002f);

        // NOISE: STATIC
        var noise = ps.noise;
        noise.enabled = false; // Disable noise for "fixed" star look, or very very low
        
        // COLOR FLASH
        var colLine = ps.colorOverLifetime;
        colLine.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0.0f), new GradientColorKey(Color.white, 1.0f) },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(0.0f, 0.0f), 
                new GradientAlphaKey(1.0f, 0.1f), 
                new GradientAlphaKey(1.0f, 0.9f), 
                new GradientAlphaKey(0.0f, 1.0f) 
            }
        );
        colLine.color = grad;
        
        // Restart system
        ps.Play();
    }

    // EXCLUSION LOGIC (Manual Culling)
    void LateUpdate()
    {
        if (exclusionZones.Count == 0 || _systems == null) return;

        if (_particles == null || _particles.Length < maxParticles)
            _particles = new ParticleSystem.Particle[maxParticles];

        foreach (var ps in _systems)
        {
            if (ps == null) continue;
            
            int count = ps.GetParticles(_particles);
            bool changes = false;

            for (int i = 0; i < count; i++)
            {
                // Check distance to all exclusion zones
                Vector3 pos = _particles[i].position; // World space because SimulationSpace is World
                
                bool inside = false;
                foreach(var zone in exclusionZones)
                {
                    if (zone != null && Vector3.Distance(pos, zone.position) < exclusionRadius)
                    {
                        inside = true;
                        break;
                    }
                }

                if (inside)
                {
                    _particles[i].remainingLifetime = -1f; // Kill particle
                    changes = true;
                }
            }

            if (changes) ps.SetParticles(_particles, count);
        }
    }
    
    // Visualize Exclusion Zones
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        foreach(var zone in exclusionZones)
        {
            if (zone != null) Gizmos.DrawWireSphere(zone.position, exclusionRadius);
        }
        
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(transform.position, boxSize);
    }

    Material CreateDefaultMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) shader = Shader.Find("Legacy Shaders/Particles/Additive");

        if (shader != null)
        {
            Material mat = new Material(shader);
            mat.name = "RuntimeStarMaterial";
            mat.SetFloat("_Surface", 1.0f); 
            mat.SetFloat("_Blend", 1.0f);   
            mat.SetInt("_ZWrite", 0);
            
            Texture defaultTex = Resources.GetBuiltinResource<Texture2D>("Default-Particle.psd");
            if (defaultTex != null)
            {
                mat.mainTexture = defaultTex;
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", defaultTex);
            }
            return mat;
        }
        return null;
    }
    
    void Reset()
    {
        if (GetComponent<ParticleSystem>() == null)
            gameObject.AddComponent<ParticleSystem>();
    }
}
