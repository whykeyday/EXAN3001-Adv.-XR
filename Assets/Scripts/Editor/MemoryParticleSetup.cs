using UnityEngine;
using UnityEditor;

public class MemoryParticleSetup : EditorWindow
{
    [MenuItem("Tools/Setup Memory Particles")]
    public static void SetupParticles()
    {
        CreateForestMemory();
        CreateOceanMemory();
        CreateCatMemory();
    }

    [MenuItem("Tools/Setup Hub Fragments")]
    public static void SetupHubFragments()
    {
        CreateHubFragment(new Vector3(-2, 1.5f, 0), 0, new Color(0.4f, 0.9f, 0.4f, 0.6f), "Fragment_Forest");
        CreateHubFragment(new Vector3(0, 1.5f, 0), 1, new Color(0.3f, 0.85f, 1f, 0.6f), "Fragment_Ocean");
        CreateHubFragment(new Vector3(2, 1.5f, 0), 2, new Color(1f, 0.85f, 0.5f, 0.6f), "Fragment_Cat");
    }

    [MenuItem("Tools/Setup Gathering Effects")]
    public static void SetupGatheringEffects()
    {
        CreateGatheringEffect(new Vector3(0, 1.5f, 0), new Color(0.4f, 0.9f, 0.4f, 0.8f), "Gathering_Forest");
        CreateGatheringEffect(new Vector3(5, 1.5f, 0), new Color(0.3f, 0.85f, 1f, 0.8f), "Gathering_Ocean");
        CreateGatheringEffect(new Vector3(-5, 1.5f, 0), new Color(1f, 0.85f, 0.5f, 0.8f), "Gathering_Cat");
    }

    [MenuItem("Tools/Setup Ocean Corals")]
    public static void SetupOceanCorals()
    {
        // Create multiple coral formations in the ocean area
        CreateCoralFormation(new Vector3(6, 0, 2), "Coral_1");
        CreateCoralFormation(new Vector3(4, 0, -2), "Coral_2");
        CreateCoralFormation(new Vector3(7, 0, -1), "Coral_3");
        CreateCoralFormation(new Vector3(3, 0, 1), "Coral_4");
        CreateCoralFormation(new Vector3(5.5f, 0, 3), "Coral_5");
    }

    private static Material GetMetallicMaterial(Color color)
    {
        // Try URP Lit first (for URP projects)
        Shader s = Shader.Find("Universal Render Pipeline/Lit");
        if (s == null) s = Shader.Find("Standard");
        if (s == null) s = Shader.Find("Particles/Standard Unlit");
        if (s == null) return null;
        
        Material mat = new Material(s);
        
        // Enable transparency
        // URP: Set Surface Type to Transparent
        mat.SetFloat("_Surface", 1); // 0 = Opaque, 1 = Transparent
        mat.SetFloat("_Blend", 0); // 0 = Alpha, 1 = Premultiply, 2 = Additive
        mat.SetFloat("_AlphaClip", 0);
        
        // Standard Shader: Set rendering mode to Transparent
        mat.SetFloat("_Mode", 3); // 0=Opaque, 1=Cutout, 2=Fade, 3=Transparent
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
        
        // Semi-transparent color (若隐若现)
        Color transparentColor = new Color(color.r, color.g, color.b, 0.4f);
        mat.SetColor("_BaseColor", transparentColor); // URP
        mat.SetColor("_Color", transparentColor); // Standard
        mat.color = transparentColor;
        
        // Mirror metallic finish (镜面金属感)
        mat.SetFloat("_Metallic", 0.9f);
        mat.SetFloat("_Smoothness", 0.95f);
        mat.SetFloat("_Glossiness", 0.95f);
        
        return mat;
    }

    private static void SetupMeshRenderer(ParticleSystemRenderer psr, Color color)
    {
        psr.renderMode = ParticleSystemRenderMode.Mesh;
        // Use Cube mesh for geometric look
        GameObject tempCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        psr.mesh = tempCube.GetComponent<MeshFilter>().sharedMesh;
        DestroyImmediate(tempCube);

        psr.material = GetMetallicMaterial(color);
    }

    // ============ FOREST (Christmas Tree) ============
    private static void CreateForestMemory()
    {
        GameObject root = new GameObject("Memory_Forest");
        root.transform.position = Vector3.zero;

        CreateTreeLayer(root, new Vector3(0, 0.5f, 0), 1.5f, 1.5f, "Layer_Bottom");
        CreateTreeLayer(root, new Vector3(0, 1.5f, 0), 1.2f, 1.2f, "Layer_Middle");
        CreateTreeLayer(root, new Vector3(0, 2.5f, 0), 0.8f, 1.0f, "Layer_Top");
        CreateTrunk(root);
    }

    private static void CreateTreeLayer(GameObject parent, Vector3 localPos, float radius, float height, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform);
        go.transform.localPosition = localPos;

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psr = go.GetComponent<ParticleSystemRenderer>();

        var main = ps.main;
        main.startSize = 0.05f; // Larger for mesh visibility
        main.startSpeed = 0f;
        main.maxParticles = 800; // Reduced density
        main.startLifetime = 30f;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startColor = new Color(0.4f, 0.9f, 0.4f, 1f);
        main.prewarm = true;

        var emission = ps.emission;
        emission.rateOverTime = 30f; // Lower rate

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 30f;
        shape.radius = radius;
        shape.length = height;
        shape.radiusThickness = 1f;

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.02f;
        noise.frequency = 0.1f;
        noise.scrollSpeed = 0.02f;

        SetupMeshRenderer(psr, new Color(0.4f, 0.9f, 0.4f, 1f));
    }

    private static void CreateTrunk(GameObject parent)
    {
        GameObject go = new GameObject("Trunk");
        go.transform.SetParent(parent.transform);
        go.transform.localPosition = Vector3.zero;

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psr = go.GetComponent<ParticleSystemRenderer>();

        var main = ps.main;
        main.startSize = 0.04f;
        main.startSpeed = 0f;
        main.maxParticles = 200; // Reduced
        main.startLifetime = 30f;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startColor = new Color(0.6f, 0.4f, 0.2f, 1f);
        main.prewarm = true;

        var emission = ps.emission;
        emission.rateOverTime = 8f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(0.2f, 0.5f, 0.2f);

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.01f;
        noise.frequency = 0.1f;
        noise.scrollSpeed = 0.01f;

        SetupMeshRenderer(psr, new Color(0.6f, 0.4f, 0.2f, 1f));
    }

    // ============ OCEAN ============
    private static void CreateOceanMemory()
    {
        GameObject go = new GameObject("Memory_Ocean");
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psr = go.GetComponent<ParticleSystemRenderer>();
        go.transform.position = new Vector3(5, 0, 0);

        var main = ps.main;
        main.startSize = 0.04f;
        main.startSpeed = 0f;
        main.maxParticles = 4000; // Reduced from 15000
        main.startLifetime = 15f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = new Color(0.3f, 0.85f, 1f, 1f);
        main.prewarm = true;

        var emission = ps.emission;
        emission.rateOverTime = 300f; // Reduced from 1000

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(8f, 0.01f, 8f);
        shape.position = Vector3.zero;

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = 0f;
        velocity.y = 0f;
        velocity.z = 0.25f;

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.1f; // Reduced
        noise.frequency = 0.1f;
        noise.scrollSpeed = 0.05f;
        noise.separateAxes = true;
        noise.strengthX = 0.03f;
        noise.strengthY = 0.08f; // Much lower waves
        noise.strengthZ = 0.03f;

        SetupMeshRenderer(psr, new Color(0.3f, 0.85f, 1f, 1f));
    }

    // ============ CAT ============
    private static void CreateCatMemory()
    {
        GameObject root = new GameObject("Memory_Cat");
        root.transform.position = new Vector3(-5, 0, 0);

        CreateCatPart(root, new Vector3(0, 0.6f, 0), new Vector3(1.2f, 1f, 0.8f), "Body");
        CreateCatPart(root, new Vector3(0, 1.4f, 0.5f), new Vector3(0.7f, 0.7f, 0.7f), "Head");
        CreateCatPart(root, new Vector3(-0.25f, 1.75f, 0.5f), new Vector3(0.2f, 0.3f, 0.1f), "Ear_L");
        CreateCatPart(root, new Vector3(0.25f, 1.75f, 0.5f), new Vector3(0.2f, 0.3f, 0.1f), "Ear_R");
        CreateCatTail(root);
    }

    private static void CreateCatPart(GameObject parent, Vector3 localPos, Vector3 scale, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform);
        go.transform.localPosition = localPos;

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psr = go.GetComponent<ParticleSystemRenderer>();

        var main = ps.main;
        main.startSize = 0.05f;
        main.startSpeed = 0f;
        main.maxParticles = 800; // Reduced from 3000
        main.startLifetime = 30f;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startColor = new Color(1f, 0.85f, 0.5f, 1f);
        main.prewarm = true;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.5f;
        shape.scale = scale;

        var emission = ps.emission;
        emission.rateOverTime = 30f; // Reduced from 100

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.015f;
        noise.frequency = 0.08f;
        noise.scrollSpeed = 0.015f;

        SetupMeshRenderer(psr, new Color(1f, 0.85f, 0.5f, 1f));
    }

    private static void CreateCatTail(GameObject parent)
    {
        GameObject go = new GameObject("Tail");
        go.transform.SetParent(parent.transform);
        go.transform.localPosition = new Vector3(0, 0.5f, -0.5f);
        go.transform.localRotation = Quaternion.Euler(-45, 0, 0);

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psr = go.GetComponent<ParticleSystemRenderer>();

        var main = ps.main;
        main.startSize = 0.04f;
        main.startSpeed = 0f;
        main.maxParticles = 400; // Reduced
        main.startLifetime = 30f;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startColor = new Color(1f, 0.85f, 0.5f, 1f);
        main.prewarm = true;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 5f;
        shape.radius = 0.1f;
        shape.length = 1.5f;

        var emission = ps.emission;
        emission.rateOverTime = 15f; // Reduced

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.02f;
        noise.frequency = 0.1f;
        noise.scrollSpeed = 0.02f;

        SetupMeshRenderer(psr, new Color(1f, 0.85f, 0.5f, 1f));
    }

    // ============ HUB FRAGMENTS ============
    private static void CreateHubFragment(Vector3 position, int sceneIndex, Color color, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.position = position;

        // Add particle system (small cluster)
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psr = go.GetComponent<ParticleSystemRenderer>();

        var main = ps.main;
        main.startSize = 0.03f;
        main.startSpeed = 0f;
        main.maxParticles = 200;
        main.startLifetime = 30f;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.prewarm = true;

        var emission = ps.emission;
        emission.rateOverTime = 10f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.3f;

        // Floating motion
        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.03f;
        noise.frequency = 0.2f;
        noise.scrollSpeed = 0.05f;

        SetupMeshRenderer(psr, color);

        // Add Box Collider for hand interaction
        BoxCollider col = go.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = new Vector3(0.6f, 0.6f, 0.6f);

        // Add MemoryFragment script (if it exists)
        // Note: This requires MemoryFragment.cs to be compiled first
        // go.AddComponent<MemoryFragment>().sceneIndex = sceneIndex;
    }

    // ============ GATHERING EFFECT (Implosion) ============
    private static void CreateGatheringEffect(Vector3 position, Color color, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.position = position;
        go.SetActive(false); // Start disabled, SceneAssembler will enable it

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psr = go.GetComponent<ParticleSystemRenderer>();

        var main = ps.main;
        main.startSize = 0.025f;
        main.startSpeed = 0f; // Velocity module will handle movement
        main.maxParticles = 800;
        main.startLifetime = 2f;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.loop = false; // One-shot effect
        main.playOnAwake = false;

        // Burst emission (all at once)
        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0f, 500, 500, 1, 0.01f)
        });

        // Spawn from large sphere
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 4f; // Large radius
        shape.radiusThickness = 0.1f; // Spawn on surface

        // KEY: Negative radial velocity = flying INWARD
        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.radial = -3f; // Negative = towards center

        // Fade in then out
        var colorOverLife = ps.colorOverLifetime;
        colorOverLife.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(color, 0f),
                new GradientColorKey(color, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),   // Start invisible
                new GradientAlphaKey(0.8f, 0.3f), // Fade in
                new GradientAlphaKey(0.8f, 0.7f), // Stay visible
                new GradientAlphaKey(0f, 1f)   // Fade out
            }
        );
        colorOverLife.color = gradient;

        // Size shrinks as it approaches center
        var sizeOverLife = ps.sizeOverLifetime;
        sizeOverLife.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 1f);
        sizeCurve.AddKey(1f, 0.2f);
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        SetupMeshRenderer(psr, color);
    }

    // ============ CORAL FORMATIONS ============
    private static void CreateCoralFormation(Vector3 position, string name)
    {
        // Purple color for coral
        Color coralColor = new Color(0.7f, 0.3f, 0.8f, 0.85f); // Purple

        GameObject root = new GameObject(name);
        root.transform.position = position;

        // Create main coral stem
        CreateCoralBranch(root, Vector3.zero, 0.4f, 0.8f, "Stem");
        
        // Create branching corals
        CreateCoralBranch(root, new Vector3(0.15f, 0.5f, 0), 0.25f, 0.5f, "Branch_1");
        CreateCoralBranch(root, new Vector3(-0.12f, 0.4f, 0.1f), 0.2f, 0.4f, "Branch_2");
        CreateCoralBranch(root, new Vector3(0.05f, 0.6f, -0.1f), 0.18f, 0.35f, "Branch_3");

        // Add sphere collider for interaction
        SphereCollider col = root.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 0.6f;
        col.center = new Vector3(0, 0.4f, 0);

        // Note: CoralInteraction.cs script needs to be added manually
        // as Editor scripts cannot directly add runtime scripts
    }

    private static void CreateCoralBranch(GameObject parent, Vector3 localPos, float radius, float height, string name)
    {
        Color coralColor = new Color(0.7f, 0.3f, 0.8f, 0.85f); // Purple

        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform);
        go.transform.localPosition = localPos;

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psr = go.GetComponent<ParticleSystemRenderer>();

        var main = ps.main;
        main.startSize = 0.025f;
        main.startSpeed = 0f;
        main.maxParticles = 400;
        main.startLifetime = 30f;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.prewarm = true;

        var emission = ps.emission;
        emission.rateOverTime = 15f;

        // Cone shape for coral branch
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 15f; // Narrow cone = coral branch shape
        shape.radius = radius * 0.3f;
        shape.length = height;
        shape.radiusThickness = 1f; // Fill the volume

        // Gentle sway motion
        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.015f;
        noise.frequency = 0.15f;
        noise.scrollSpeed = 0.03f;

        SetupMeshRenderer(psr, coralColor);
    }
}
