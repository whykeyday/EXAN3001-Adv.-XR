using UnityEngine;
using UnityEditor;

/// <summary>
/// Tools menu shortcuts to set up each individual scene in EXAN3001-Adv.-XR.
/// Open the target scene first, then run the matching menu item.
/// </summary>
public class SceneSetupTools : EditorWindow
{
    // ─────────────────────────────────────────────────────────────────────────
    // OCEAN SCENE
    // ─────────────────────────────────────────────────────────────────────────
    [MenuItem("Tools/Scene Setup/Setup OceanScene (Corals + BreathReactor)")]
    public static void SetupOceanScene()
    {
        // Blue ocean water particles
        CreateOceanWaterParticles();

        // 5 Coral formations
        CreateCoralFormation(new Vector3( 1.5f, 0,  1.0f), "Coral_1");
        CreateCoralFormation(new Vector3(-1.0f, 0,  1.5f), "Coral_2");
        CreateCoralFormation(new Vector3( 2.0f, 0, -0.5f), "Coral_3");
        CreateCoralFormation(new Vector3(-0.5f, 0, -1.5f), "Coral_4");
        CreateCoralFormation(new Vector3( 0.5f, 0,  2.5f), "Coral_5");

        // BreathManager
        SetupBreathSystem();

        // OceanAudio placeholder
        SetupOceanAudio();

        // Link BreathReactor to Audio and Particles
        GameObject breathGO = GameObject.Find("BreathManager");
        GameObject audioGO  = GameObject.Find("OceanAudio");
        GameObject partsGO  = GameObject.Find("Memory_Ocean");

        if (breathGO != null)
        {
            BreathReactor reactor = breathGO.GetComponent<BreathReactor>();
            if (reactor != null)
            {
                if (audioGO != null) reactor.targetAudio = audioGO.GetComponent<AudioSource>();
                if (partsGO != null) reactor.targetParticles = partsGO.GetComponent<ParticleSystem>();
                
                // Configure for ocean
                reactor.minEmission = 100f;
                reactor.maxEmission = 500f; // More particles when breathing hard
                reactor.minSpeed = 0.5f;
                reactor.maxSpeed = 2.5f;    // Faster flow
            }
        }

        Debug.Log("[SceneSetupTools] OceanScene setup complete! Assign ocean_waves.wav to OceanAudio, then save scene.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CAT SCENE
    // ─────────────────────────────────────────────────────────────────────────
    [MenuItem("Tools/Scene Setup/Setup CatScene (Cat Particle + CatInteract)")]
    public static void SetupCatScene()
    {
        // Full cat particle body from EXAN3001 MemoryParticleSetup
        GameObject catRoot = new GameObject("Memory_Cat");
        catRoot.transform.position = new Vector3(0, 0, 1.5f);

        CreateCatPart(catRoot, new Vector3(0, 0.6f, 0),      new Vector3(1.2f, 1f, 0.8f),  "Body");
        CreateCatPart(catRoot, new Vector3(0, 1.4f, 0.5f),   new Vector3(0.7f, 0.7f, 0.7f),"Head");
        CreateCatPart(catRoot, new Vector3(-0.25f, 1.75f, 0.5f), new Vector3(0.2f, 0.3f, 0.1f),"Ear_L");
        CreateCatPart(catRoot, new Vector3( 0.25f, 1.75f, 0.5f), new Vector3(0.2f, 0.3f, 0.1f),"Ear_R");
        CreateCatTail(catRoot);

        // Add SphereCollider + CatInteract on the Head part
        Transform head = catRoot.transform.Find("Head");
        if (head != null)
        {
            SphereCollider sc = head.gameObject.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 0.65f;

            Rigidbody rb = head.gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            head.gameObject.AddComponent<CatInteract>();
        }

        // cataudio placeholder
        SetupCatAudio();

        Debug.Log("[SceneSetupTools] CatScene setup complete! Remember to:\n" +
                  "1. Assign cat.wav clip to cataudio AudioSource\n" +
                  "2. Drag that AudioSource into CatInteract → Purr Audio\n" +
                  "3. Hand GameObjects need tag 'PlayerHand'");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TREE SCENE
    // ─────────────────────────────────────────────────────────────────────────
    [MenuItem("Tools/Scene Setup/Setup TreeScene (Forest Memory + TreeHealer)")]
    public static void SetupTreeScene()
    {
        // Full tree from EXAN3001 MemoryParticleSetup
        GameObject treeRoot = new GameObject("Memory_Forest");
        treeRoot.transform.position = new Vector3(0, 0, 1.5f);

        CreateTreeLayer(treeRoot, new Vector3(0, 0.5f, 0), 1.5f, 1.5f, "Layer_Bottom");
        CreateTreeLayer(treeRoot, new Vector3(0, 1.5f, 0), 1.2f, 1.2f, "Layer_Middle");
        CreateTreeLayer(treeRoot, new Vector3(0, 2.5f, 0), 0.8f, 1.0f, "Layer_Top");
        CreateTrunk(treeRoot);

        // TreeHealer on root
        TreeHealer healer = treeRoot.AddComponent<TreeHealer>();
        // Try to auto-assign the first particle system found
        ParticleSystem ps = treeRoot.GetComponentInChildren<ParticleSystem>();
        if (ps != null) healer.treeParticles = ps;

        Debug.Log("[SceneSetupTools] TreeScene setup complete! Remember to:\n" +
                  "1. Assign all layer ParticleSystems to TreeHealer if needed\n" +
                  "2. Hand GameObjects need tag 'PlayerHand'");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // HELPERS — Ocean water particles
    // ─────────────────────────────────────────────────────────────────────────
    static void CreateOceanWaterParticles()
    {
        if (GameObject.Find("Memory_Ocean") != null) return;

        GameObject go = new GameObject("Memory_Ocean");
        go.transform.position = new Vector3(0, 0, 1.5f);

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psr = go.GetComponent<ParticleSystemRenderer>();

        var main = ps.main;
        main.startSize       = 0.04f;
        main.startSpeed      = 0f;
        main.maxParticles    = 4000;
        main.startLifetime   = 15f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor      = new Color(0.3f, 0.85f, 1f, 1f); // Ocean blue
        main.prewarm         = true;

        var emission = ps.emission;
        emission.rateOverTime = 300f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale     = new Vector3(8f, 0.01f, 8f);

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space   = ParticleSystemSimulationSpace.World;
        velocity.z       = new ParticleSystem.MinMaxCurve(0.1f, 0.3f); // Gentle drift

        var noise = ps.noise;
        noise.enabled    = true;
        noise.strength   = 0.1f;
        noise.frequency  = 0.1f;
        noise.scrollSpeed = 0.05f;
        noise.separateAxes = true;
        noise.strengthX  = 0.03f;
        noise.strengthY  = 0.08f;
        noise.strengthZ  = 0.03f;

        SetupMeshRenderer(psr, new Color(0.3f, 0.85f, 1f, 1f));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // HELPERS — Breath system
    // ─────────────────────────────────────────────────────────────────────────
    static void SetupBreathSystem()
    {
        // Check if BreathManager already exists
        if (GameObject.Find("BreathManager") != null) return;

        GameObject bmGO = new GameObject("BreathManager");
        bmGO.AddComponent<BreathInputManager>();
        bmGO.AddComponent<BreathReactor>();
    }

    static void SetupOceanAudio()
    {
        if (GameObject.Find("OceanAudio") != null) return;
        GameObject go = new GameObject("OceanAudio");
        AudioSource src = go.AddComponent<AudioSource>();
        src.loop = true;
        src.spatialBlend = 0f; // 2D sound
        src.volume = 0.5f;
        src.playOnAwake = true;
        // User needs to assign the clip in Inspector
    }

    static void SetupCatAudio()
    {
        if (GameObject.Find("cataudio") != null) return;
        GameObject go = new GameObject("cataudio");
        AudioSource src = go.AddComponent<AudioSource>();
        src.loop = true;
        src.spatialBlend = 0f;
        src.volume = 0f; // CatInteract controls volume
        src.playOnAwake = false;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // HELPERS — Corals (from EXAN3001 MemoryParticleSetup)
    // ─────────────────────────────────────────────────────────────────────────
    static void CreateCoralFormation(Vector3 position, string name)
    {
        Color coralColor = new Color(0.7f, 0.3f, 0.8f, 0.85f);
        GameObject root = new GameObject(name);
        root.transform.position = position;

        CreateCoralBranch(root, Vector3.zero,                    0.4f, 0.8f, "Stem");
        CreateCoralBranch(root, new Vector3(0.15f,  0.5f,  0),  0.25f, 0.5f, "Branch_1");
        CreateCoralBranch(root, new Vector3(-0.12f, 0.4f,  0.1f), 0.2f, 0.4f, "Branch_2");
        CreateCoralBranch(root, new Vector3(0.05f,  0.6f, -0.1f), 0.18f, 0.35f,"Branch_3");

        SphereCollider col = root.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 0.6f;
        col.center = new Vector3(0, 0.4f, 0);

        // Add CoralInteraction runtime script
        root.AddComponent<CoralInteraction>();
    }

    static void CreateCoralBranch(GameObject parent, Vector3 localPos, float radius, float height, string name)
    {
        Color coralColor = new Color(0.7f, 0.3f, 0.8f, 0.85f);
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform);
        go.transform.localPosition = localPos;

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psr = go.GetComponent<ParticleSystemRenderer>();

        var main = ps.main;
        main.startSize     = 0.025f;
        main.startSpeed    = 0f;
        main.maxParticles  = 400;
        main.startLifetime = 30f;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.prewarm       = true;

        var emission = ps.emission;
        emission.rateOverTime = 15f;

        var shape = ps.shape;
        shape.shapeType       = ParticleSystemShapeType.Cone;
        shape.angle           = 15f;
        shape.radius          = radius * 0.3f;
        shape.length          = height;
        shape.radiusThickness = 1f;

        var noise = ps.noise;
        noise.enabled     = true;
        noise.strength    = 0.015f;
        noise.frequency   = 0.15f;
        noise.scrollSpeed = 0.03f;

        SetupMeshRenderer(psr, coralColor);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // HELPERS — Cat (from EXAN3001 MemoryParticleSetup)
    // ─────────────────────────────────────────────────────────────────────────
    static void CreateCatPart(GameObject parent, Vector3 localPos, Vector3 scale, string name)
    {
        Color catColor = new Color(1f, 0.85f, 0.5f, 1f);
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform);
        go.transform.localPosition = localPos;

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psr = go.GetComponent<ParticleSystemRenderer>();

        var main = ps.main;
        main.startSize     = 0.05f;
        main.startSpeed    = 0f;
        main.maxParticles  = 800;
        main.startLifetime = 30f;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startColor    = catColor;
        main.prewarm       = true;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius    = 0.5f;
        shape.scale     = scale;

        var emission = ps.emission;
        emission.rateOverTime = 30f;

        var noise = ps.noise;
        noise.enabled     = true;
        noise.strength    = 0.015f;
        noise.frequency   = 0.08f;
        noise.scrollSpeed = 0.015f;

        SetupMeshRenderer(psr, catColor);
    }

    static void CreateCatTail(GameObject parent)
    {
        Color catColor = new Color(1f, 0.85f, 0.5f, 1f);
        GameObject go = new GameObject("Tail");
        go.transform.SetParent(parent.transform);
        go.transform.localPosition = new Vector3(0, 0.5f, -0.5f);
        go.transform.localRotation = Quaternion.Euler(-45, 0, 0);

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psr = go.GetComponent<ParticleSystemRenderer>();

        var main = ps.main;
        main.startSize     = 0.04f;
        main.startSpeed    = 0f;
        main.maxParticles  = 400;
        main.startLifetime = 30f;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startColor    = catColor;
        main.prewarm       = true;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle     = 5f;
        shape.radius    = 0.1f;
        shape.length    = 1.5f;

        var emission = ps.emission;
        emission.rateOverTime = 15f;

        var noise = ps.noise;
        noise.enabled     = true;
        noise.strength    = 0.02f;
        noise.frequency   = 0.1f;
        noise.scrollSpeed = 0.02f;

        SetupMeshRenderer(psr, catColor);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // HELPERS — Tree (from EXAN3001 MemoryParticleSetup)
    // ─────────────────────────────────────────────────────────────────────────
    static void CreateTreeLayer(GameObject parent, Vector3 localPos, float radius, float height, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform);
        go.transform.localPosition = localPos;

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psr = go.GetComponent<ParticleSystemRenderer>();

        var main = ps.main;
        main.startSize     = 0.05f;
        main.startSpeed    = 0f;
        main.maxParticles  = 800;
        main.startLifetime = 30f;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startColor    = new Color(0.4f, 0.9f, 0.4f, 1f);
        main.prewarm       = true;

        var emission = ps.emission;
        emission.rateOverTime = 30f;

        var shape = ps.shape;
        shape.shapeType       = ParticleSystemShapeType.Cone;
        shape.angle           = 30f;
        shape.radius          = radius;
        shape.length          = height;
        shape.radiusThickness = 1f;

        var noise = ps.noise;
        noise.enabled     = true;
        noise.strength    = 0.02f;
        noise.frequency   = 0.1f;
        noise.scrollSpeed = 0.02f;

        SetupMeshRenderer(psr, new Color(0.4f, 0.9f, 0.4f, 1f));
    }

    static void CreateTrunk(GameObject parent)
    {
        GameObject go = new GameObject("Trunk");
        go.transform.SetParent(parent.transform);
        go.transform.localPosition = Vector3.zero;

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psr = go.GetComponent<ParticleSystemRenderer>();

        var main = ps.main;
        main.startSize     = 0.04f;
        main.startSpeed    = 0f;
        main.maxParticles  = 200;
        main.startLifetime = 30f;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startColor    = new Color(0.6f, 0.4f, 0.2f, 1f);
        main.prewarm       = true;

        var emission = ps.emission;
        emission.rateOverTime = 8f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale     = new Vector3(0.2f, 0.5f, 0.2f);

        var noise = ps.noise;
        noise.enabled     = true;
        noise.strength    = 0.01f;
        noise.frequency   = 0.1f;
        noise.scrollSpeed = 0.01f;

        SetupMeshRenderer(psr, new Color(0.6f, 0.4f, 0.2f, 1f));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // HELPERS — Material
    // ─────────────────────────────────────────────────────────────────────────
    static void SetupMeshRenderer(ParticleSystemRenderer psr, Color color)
    {
        psr.renderMode = ParticleSystemRenderMode.Mesh;
        GameObject tempCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        psr.mesh = tempCube.GetComponent<MeshFilter>().sharedMesh;
        DestroyImmediate(tempCube);
        psr.material = GetMetallicMaterial(color);
    }

    static Material GetMetallicMaterial(Color color)
    {
        Shader s = Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Standard");
        if (s == null) return null;

        Material mat = new Material(s);
        mat.SetFloat("_Surface",    1);
        mat.SetFloat("_Blend",      0);
        mat.SetFloat("_AlphaClip",  0);
        mat.SetFloat("_Mode",       3);
        mat.SetInt("_SrcBlend",     (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend",     (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite",       0);
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.renderQueue = 3000;

        Color c = new Color(color.r, color.g, color.b, 0.4f);
        mat.SetColor("_BaseColor", c);
        mat.SetColor("_Color",     c);
        mat.color = c;
        mat.SetFloat("_Metallic",   0.9f);
        mat.SetFloat("_Smoothness", 0.95f);
        mat.SetFloat("_Glossiness", 0.95f);
        return mat;
    }
}
