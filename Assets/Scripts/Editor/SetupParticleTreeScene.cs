using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// This script automatically configures the TreeScene inside a Single Flat Node
/// without nested child models to support Single Pass Stereoscopic rendering stability on Quest 3.
/// </summary>
public class SetupParticleTreeScene : Editor
{
    [MenuItem("Tools/Rebuild Transparent Particle Tree Scene")]
    public static void ManualSetupScene()
    {
        bool isTreeSceneLoaded = false;
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
        {
            if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).name == "TreeScene") isTreeSceneLoaded = true;
        }
        if (!isTreeSceneLoaded)
        {
            Debug.LogWarning("Please load TreeScene first!");
            return;
        }

        // 1. Fetch meshes from FBX assets
        GameObject deadFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/dead-tree/source/Tree/Tree/Tree.FBX");
        GameObject greenFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/tree-gn/source/TreeGen.fbx");

        if (deadFbx == null || greenFbx == null)
        {
            Debug.LogError("Could not find either Dead or Alive Mesh FBX Files. Check paths in Assets folder!");
            return;
        }

        Mesh deadMesh = deadFbx.GetComponentInChildren<MeshFilter>().sharedMesh;
        Mesh greenMesh = greenFbx.GetComponentInChildren<MeshFilter>().sharedMesh;

        if (deadMesh == null || greenMesh == null)
        {
            Debug.LogError("Failed to extract Mesh streams from FBX prefabs.");
            return;
        }

        // 2. Clear Old Systems
        EradicateOldSystems();

        // 3. Carry forward manual editor position setups
        Vector3 lastPos = new Vector3(-0.08571f, 0.95f, 12.559f); 
        Vector3 lastScale = Vector3.one * 137.0332f;
        Quaternion lastRot = Quaternion.Euler(0, 18.326f, 0);

        GameObject existing = GameObject.Find("TransparentParticleTree");
        if (existing != null)
        {
            lastPos = existing.transform.position;
            lastScale = existing.transform.localScale;
            lastRot = existing.transform.rotation;
        }

        // 4. Create single target root
        GameObject particleTree = new GameObject("TransparentParticleTree");
        UnityEngine.SceneManagement.Scene treeScene = UnityEngine.SceneManagement.SceneManager.GetSceneByName("TreeScene");
        if (treeScene.isLoaded) UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(particleTree, treeScene);
        particleTree.transform.position = lastPos;
        particleTree.transform.localScale = lastScale;
        particleTree.transform.rotation = lastRot;

        EnableReadWriteOnFbx("Assets/dead-tree/source/Tree/Tree/Tree.FBX");
        EnableReadWriteOnFbx("Assets/tree-gn/source/TreeGen.fbx");

        // 5. Config Healer
        ParticleTreeHealer healer = particleTree.AddComponent<ParticleTreeHealer>();
        healer.witheredMesh = deadMesh;
        healer.aliveMesh = greenMesh;

        // --- Configure visual parameters ---
        healer.canopyMaxHeight = deadMesh.bounds.size.y;
        healer.healingRate = 0.05f;
        healer.decayRate = 0.02f;
        healer.aliveMeshScaleMultiplier = 0.8f;
        healer.aliveMeshRotationOffset = new Vector3(-90, 0, 0);

        // Note: The new ParticleTreeHealer creates all particle systems at runtime in Start().
        // No need to create materials or configure particle sizes here.

        // 7. Bind Physics and Rendering directly to the root
        MeshFilter mf = particleTree.AddComponent<MeshFilter>();
        mf.sharedMesh = deadMesh; // starts as Dead for bounding-boxes wireframes setups

        MeshRenderer mr = particleTree.AddComponent<MeshRenderer>();
        mr.enabled = false; // INVISIBLE container directly on root

        Rigidbody rb = particleTree.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        MeshCollider mc = particleTree.AddComponent<MeshCollider>();
        mc.sharedMesh = deadMesh;
        mc.convex = true;
        mc.isTrigger = true;

        SphereCollider sc = particleTree.AddComponent<SphereCollider>();
        sc.isTrigger = true;
        sc.radius = 1.6f;

        // --- USER FIX: Make Plane Semi-Transparent ---
        GameObject plane = GameObject.Find("Plane");
        if (plane != null)
        {
            Renderer r = plane.GetComponent<Renderer>();
            if (r != null && r.sharedMaterial != null)
            {
                Material mat = new Material(r.sharedMaterial);
                mat.name = "Plane_Transparent";
                mat.SetFloat("_Surface", 1.0f); 
                mat.SetFloat("_Blend", 0.0f);   
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

                Color c = new Color(0.42f, 0.29f, 0.16f, 0.35f); 
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
                r.material = mat;
            }
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("✅ Transparent Particle Tree Re-Generated inside a SINGLE FLAT NODE successfully!");
    }

    private static void EradicateOldSystems()
    {
        string[] namesToDelete = { "TransparentParticleTree", "GreenModelNode", "DeadModelNode", "VFX_DeadTree", "Procedural", "ParticleTree", "Tree" };
        foreach (string name in namesToDelete)
        {
            GameObject go = GameObject.Find(name);
            while (go != null)
            {
                DestroyImmediate(go);
                go = GameObject.Find(name); 
            }
        }
    }

    private static void EnableReadWriteOnFbx(string assetPath)
    {
        ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
        if (importer != null && !importer.isReadable)
        {
            importer.isReadable = true;
            importer.SaveAndReimport();
        }
    }

    private static void SetupParticles(GameObject node, Mesh treeMesh)
    {
        ParticleSystem ps = node.GetComponent<ParticleSystem>();
        if (ps == null) ps = node.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.duration = 5.0f;
        main.startLifetime = 100000f; // --- TWEAK: Infinite lifetime to prevent continuous cycle pop pops ---
        main.startSpeed = 0f;
        main.startSize = 0.003f;
        main.maxParticles = 10000;
        main.simulationSpace = ParticleSystemSimulationSpace.Local; // --- ROLLBACK: Keep local mode to avoid coordinate scaling conflicts ---
        main.scalingMode = ParticleSystemScalingMode.Hierarchy; 
        main.playOnAwake = true;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0; // Turn off continuous spawning
        
        // Burst 4000 particles at time 0
        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0.0f, 4500, 4500, 1, 0.01f)
        });

        // --- AUTOMATIC TWEAK 3: Disable Internal Noise to prevent scale-multiplication drifts ---
        var noise = ps.noise;
        noise.enabled = false; 

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Mesh;
        shape.meshShapeType = ParticleSystemMeshShapeType.Triangle; // --- TWEAK: Spawn on faces to prevent vertex overlay pops ---
        shape.mesh = treeMesh;

        ParticleSystemRenderer r = ps.GetComponent<ParticleSystemRenderer>();
        if (r != null)
        {
            // --- AUTOMATIC TWEAK 4: Use 3D Mesh particles so they surround you from every direction in VR ---
            r.renderMode = ParticleSystemRenderMode.Mesh;
            
            GameObject tempObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            r.mesh = tempObj.GetComponent<MeshFilter>().sharedMesh;
            DestroyImmediate(tempObj);
            
            // --- AUTOMATIC TWEAK 4: Transparent Particle Material enabling Vertex Colors ---
            Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Lit");
            if (particleShader != null)
            {
                Material pMat = new Material(particleShader);
                pMat.SetFloat("_Surface", 1.0f); // Transparent
                pMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                pMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                pMat.SetInt("_ZWrite", 0);
                pMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                
                AssetDatabase.CreateAsset(pMat, "Assets/Materials/TreeParticle_Lit_Transparent.mat");
                r.material = pMat;
            }
            else
            {
                Material safeMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/VRTemplateAssets/Materials/Particles/ConfettiParticles.mat");
                if (safeMaterial != null) r.material = safeMaterial;
            }
            
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
}
