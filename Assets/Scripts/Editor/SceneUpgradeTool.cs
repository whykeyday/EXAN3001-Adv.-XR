using UnityEngine;
using UnityEditor;

/// <summary>
/// 一键升级编辑器工具
/// 直接修改场景中已有对象，无需手动删除和重建。
/// 打开对应场景后点击菜单即可生效，然后 Ctrl+S 保存场景。
/// </summary>
public class SceneUpgradeTool
{
    // ═══════════════════════════════════════════════════════════════════
    //  MENU SCENE (SampleScene)
    // ═══════════════════════════════════════════════════════════════════
    [MenuItem("Tools/Upgrade Scenes/1. Upgrade Menu Scene (Glass + StarField)")]
    public static void UpgradeMenuScene()
    {
        int fixed_count = 0;

        // ── Fix Glass Shards ──
        // Find all GlassShardsSceneSetup components and force realistic glass
        var shards = Object.FindObjectsByType<GlassShardsSceneSetup>(FindObjectsSortMode.None);
        foreach (var shard in shards)
        {
            Renderer r = shard.GetComponent<Renderer>();
            if (r == null) continue;

            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null) { Debug.LogError("URP Lit shader not found!"); continue; }

            Material mat = new Material(urpLit);
            mat.name = "RealisticGlass";
            
            // Transparent setup
            mat.SetFloat("_Surface", 1);
            mat.SetFloat("_Blend", 0);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = 3000;
            
            // Realistic glass look
            mat.SetColor("_BaseColor", new Color(0.7f, 0.85f, 1.0f, 0.15f));
            mat.SetFloat("_Smoothness", 0.98f);
            mat.SetFloat("_Metallic", 0.25f);
            
            // Subtle rim emission
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", new Color(0.15f, 0.35f, 0.7f) * 0.6f);

            r.sharedMaterial = mat; // sharedMaterial so it saves to scene
            fixed_count++;
            Debug.Log($"[Upgrade] Fixed glass on: {shard.name}");
        }

        // ── Fix StarField ──
        var starFields = Object.FindObjectsByType<StarryFieldGenerator>(FindObjectsSortMode.None);
        foreach (var sf in starFields)
        {
            // Fix all child particle systems to use glowing sphere material
            var allPS = sf.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in allPS)
            {
                var psr = ps.GetComponent<ParticleSystemRenderer>();
                if (psr != null)
                {
                    // Switch from Mesh/Cube to Billboard (round glowing dots)
                    psr.renderMode = ParticleSystemRenderMode.Billboard;
                    
                    // Create glowing material
                    Material mat = ParticleUtils.GetGlowingSphereMaterial();
                    mat.SetColor("_EmissionColor", Color.white * 5.0f); // Extra bright for stars
                    psr.sharedMaterial = mat;
                    
                    fixed_count++;
                }
                
                // Add twinkling via colorOverLifetime
                var col = ps.colorOverLifetime;
                col.enabled = true;
                Gradient grad = new Gradient();
                grad.SetKeys(
                    new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                    new GradientAlphaKey[] { 
                        new GradientAlphaKey(0.0f, 0.0f), 
                        new GradientAlphaKey(1.0f, 0.15f), 
                        new GradientAlphaKey(0.1f, 0.35f), 
                        new GradientAlphaKey(1.0f, 0.55f), 
                        new GradientAlphaKey(0.15f, 0.75f), 
                        new GradientAlphaKey(0.0f, 1.0f) 
                    }
                );
                col.color = grad;
            }

            // Also replace materials in the inspector list
            sf.particleMaterials.Clear();
            sf.particleMaterials.Add(ParticleUtils.GetGlowingSphereMaterial());
            
            Debug.Log($"[Upgrade] Fixed StarField: {sf.name}");
        }

        Debug.Log($"[Upgrade] Menu Scene upgrade complete! Fixed {fixed_count} objects. Please save the scene (Ctrl+S).");
        EditorUtility.DisplayDialog("Menu Upgrade Complete", 
            $"Fixed {fixed_count} objects.\n\n" +
            "• Glass shards → Realistic URP glass\n" +
            "• StarField → Glowing twinkling spheres\n\n" +
            "Press Ctrl+S to save the scene!", "OK");
    }

    // ═══════════════════════════════════════════════════════════════════
    //  TREE SCENE
    // ═══════════════════════════════════════════════════════════════════
    [MenuItem("Tools/Upgrade Scenes/2. Upgrade Tree Scene (Glowing Particles)")]
    public static void UpgradeTreeScene()
    {
        int fixed_count = 0;

        // Find ALL particle systems in the scene and upgrade cube→billboard
        var allPS = Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None);
        foreach (var ps in allPS)
        {
            var psr = ps.GetComponent<ParticleSystemRenderer>();
            if (psr == null) continue;

            // Only fix those using Mesh mode (the old cube renderer)
            if (psr.renderMode == ParticleSystemRenderMode.Mesh)
            {
                psr.renderMode = ParticleSystemRenderMode.Billboard;
                psr.sharedMaterial = ParticleUtils.GetGlowingSphereMaterial();
                fixed_count++;
                Debug.Log($"[Upgrade] Fixed particle: {ps.name} (Cube → Glowing Sphere)");
            }
        }

        // Ensure ParticleTreeHealer has slower healing
        var healers = Object.FindObjectsByType<ParticleTreeHealer>(FindObjectsSortMode.None);
        foreach (var h in healers)
        {
            if (h.healingRate > 0.06f)
            {
                h.healingRate = 0.05f;
                h.decayRate = 0.02f;
                Debug.Log($"[Upgrade] Slowed healing rate on: {h.name}");
                fixed_count++;
            }
            EditorUtility.SetDirty(h);
        }

        Debug.Log($"[Upgrade] Tree Scene upgrade complete! Fixed {fixed_count} objects. Please save (Ctrl+S).");
        EditorUtility.DisplayDialog("Tree Upgrade Complete", 
            $"Fixed {fixed_count} particle systems.\n\n" +
            "• All cube particles → Glowing spheres\n" +
            "• Healing rate slowed to ~20s\n" +
            "• Yellow scarf + butterflies auto-generate at Play\n\n" +
            "Press Ctrl+S to save!", "OK");
    }

    // ═══════════════════════════════════════════════════════════════════
    //  OCEAN SCENE
    // ═══════════════════════════════════════════════════════════════════
    [MenuItem("Tools/Upgrade Scenes/3. Upgrade Ocean Scene (Corals + Fish)")]
    public static void UpgradeOceanScene()
    {
        int fixed_count = 0;

        // ── Fix ALL particle systems (cube → glowing sphere) ──
        var allPS = Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None);
        foreach (var ps in allPS)
        {
            var psr = ps.GetComponent<ParticleSystemRenderer>();
            if (psr != null && psr.renderMode == ParticleSystemRenderMode.Mesh)
            {
                psr.renderMode = ParticleSystemRenderMode.Billboard;
                
                // Check if it's ocean water (blue) or coral (purple)
                var main = ps.main;
                Color startCol = main.startColor.color;
                
                Material mat = ParticleUtils.GetGlowingSphereMaterial();
                
                // Make ocean particles semi-transparent
                if (startCol.b > 0.7f) // Blue-ish = ocean water
                {
                    mat.SetColor("_BaseColor", new Color(0.3f, 0.85f, 1f, 0.5f));
                    mat.SetColor("_EmissionColor", new Color(0.3f, 0.85f, 1f) * 1.5f);
                }
                else // Coral = slightly glowing
                {
                    mat.SetColor("_BaseColor", new Color(0.7f, 0.3f, 0.8f, 0.85f));
                    mat.SetColor("_EmissionColor", new Color(0.7f, 0.3f, 0.8f) * 2.0f);
                }
                
                psr.sharedMaterial = mat;
                fixed_count++;
            }
        }

        // ── Swap CoralInteraction → CoralInteractor on all Coral objects ──
        var oldCorals = Object.FindObjectsByType<CoralInteraction>(FindObjectsSortMode.None);
        foreach (var oldCI in oldCorals)
        {
            GameObject coralObj = oldCI.gameObject;
            
            // Add new CoralInteractor if not already present
            if (coralObj.GetComponent<CoralInteractor>() == null)
            {
                coralObj.AddComponent<CoralInteractor>();
                Debug.Log($"[Upgrade] Added CoralInteractor to: {coralObj.name}");
            }
            
            // Remove old CoralInteraction
            Object.DestroyImmediate(oldCI);
            fixed_count++;
        }

        // ── Add seagull audio placeholder ──
        if (GameObject.Find("SeagullAudio") == null)
        {
            GameObject seagull = new GameObject("SeagullAudio");
            AudioSource src = seagull.AddComponent<AudioSource>();
            src.loop = false;
            src.playOnAwake = false;
            src.spatialBlend = 0f;
            Debug.Log("[Upgrade] Created SeagullAudio placeholder. Assign a seagull clip, set playOnAwake=true.");
            fixed_count++;
        }

        // ── Fish swarm placeholder ──
        if (GameObject.Find("FishSwarm") == null)
        {
            GameObject fishObj = new GameObject("FishSwarm");
            fishObj.transform.position = new Vector3(0, 0.5f, 1.5f);
            fishObj.AddComponent<FishSwarmFollower>();
            Debug.Log("[Upgrade] Created FishSwarm placeholder. Drag fish FBX model as child, then use Tools->Make Particle Container on it.");
            fixed_count++;
        }

        Debug.Log($"[Upgrade] Ocean Scene upgrade complete! Fixed {fixed_count} objects. Please save (Ctrl+S).");
        EditorUtility.DisplayDialog("Ocean Upgrade Complete", 
            $"Fixed {fixed_count} objects.\n\n" +
            "• All cube particles → Glowing spheres\n" +
            "• Corals: CoralInteraction → CoralInteractor (breath-linked)\n" +
            "• SeagullAudio placeholder created\n" +
            "• FishSwarm placeholder created\n\n" +
            "Next steps:\n" +
            "1. Drag fish FBX under FishSwarm, use Make Particle Container\n" +
            "2. Assign seagull clip to SeagullAudio\n\n" +
            "Press Ctrl+S to save!", "OK");
    }

    // ═══════════════════════════════════════════════════════════════════
    //  CAT SCENE  
    // ═══════════════════════════════════════════════════════════════════
    [MenuItem("Tools/Upgrade Scenes/4. Upgrade Cat Scene (Fireplace + Interactions)")]
    public static void UpgradeCatScene()
    {
        int fixed_count = 0;

        // ── Fix ALL particle systems (cube → glowing sphere) ──
        var allPS = Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None);
        foreach (var ps in allPS)
        {
            var psr = ps.GetComponent<ParticleSystemRenderer>();
            if (psr != null && psr.renderMode == ParticleSystemRenderMode.Mesh)
            {
                psr.renderMode = ParticleSystemRenderMode.Billboard;
                psr.sharedMaterial = ParticleUtils.GetGlowingSphereMaterial();
                fixed_count++;
            }
        }

        // ── Add fireplace sparks if not present ──
        if (GameObject.Find("FireSparksParticles") == null)
        {
            // Find a good position (near the edge of the scene)
            GameObject fireplace = new GameObject("Fireplace_Sparks");
            fireplace.transform.position = new Vector3(0, 0.5f, 2.4f);

            GameObject sparks = new GameObject("FireSparksParticles");
            sparks.transform.SetParent(fireplace.transform);
            sparks.transform.localPosition = Vector3.up * 0.5f;

            ParticleSystem ps = sparks.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 3.0f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.0f, 2.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.06f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.4f, 0f, 1f),
                new Color(1f, 0.8f, 0.2f, 1f)
            );

            var emission = ps.emission;
            emission.rateOverTime = 40f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(1.2f, 0.1f, 0.3f);

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.y = new ParticleSystem.MinMaxCurve(1.0f, 2.0f);
            vel.x = new ParticleSystem.MinMaxCurve(-0.5f, 0.5f);

            ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = ParticleUtils.GetGlowingSphereMaterial();
            
            fixed_count++;
            Debug.Log("[Upgrade] Created fireplace sparks effect.");
        }

        // ── Separate cat head/body interaction ──
        // Find existing Memory_Cat or cat-related objects
        var catInteracts = Object.FindObjectsByType<CatInteract>(FindObjectsSortMode.None);
        if (catInteracts.Length > 0)
        {
            // Add body trigger if not present
            foreach (var ci in catInteracts)
            {
                Transform bodyTrigger = ci.transform.parent?.Find("Body");
                if (bodyTrigger != null && bodyTrigger.GetComponent<CatTouchReceiver>() == null)
                {
                    SphereCollider col = bodyTrigger.gameObject.AddComponent<SphereCollider>();
                    col.isTrigger = true;
                    col.radius = 0.65f;
                    
                    if (bodyTrigger.GetComponent<Rigidbody>() == null)
                    {
                        Rigidbody rb = bodyTrigger.gameObject.AddComponent<Rigidbody>();
                        rb.isKinematic = true;
                    }
                    
                    CatTouchReceiver recv = bodyTrigger.gameObject.AddComponent<CatTouchReceiver>();
                    recv.catRole = CatTouchReceiver.CatRole.Purr;
                    
                    fixed_count++;
                    Debug.Log("[Upgrade] Added body touch receiver to cat Body part.");
                }
            }
        }

        Debug.Log($"[Upgrade] Cat Scene upgrade complete! Fixed {fixed_count} objects. Please save (Ctrl+S).");
        EditorUtility.DisplayDialog("Cat Upgrade Complete", 
            $"Fixed {fixed_count} objects.\n\n" +
            "• All cube particles → Glowing spheres\n" +
            "• Fireplace sparks added\n\n" +
            "To complete setup:\n" +
            "1. Assign fire crackling audio to Fireplace AudioSource\n" +
            "2. Assign purr/meow clips to CatTouchReceivers\n\n" +
            "Press Ctrl+S to save!", "OK");
    }

    // ═══════════════════════════════════════════════════════════════════
    //  ALL SCENES AT ONCE
    // ═══════════════════════════════════════════════════════════════════
    [MenuItem("Tools/Upgrade Scenes/★ Upgrade ALL Open Scenes")]
    public static void UpgradeAllScenes()
    {
        UpgradeMenuScene();
        UpgradeTreeScene();
        UpgradeOceanScene();
        UpgradeCatScene();
        
        EditorUtility.DisplayDialog("All Upgrades Complete", 
            "All four scenes have been upgraded!\n\n" +
            "Press Ctrl+S to save all scenes.", "OK");
    }
}
