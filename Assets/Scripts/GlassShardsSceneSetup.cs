using UnityEngine;

// Enum outside for better inspector support
public enum ShardContent { Ocean, Tree, Cat, None }

public class GlassShardsSceneSetup : MonoBehaviour
{
    [Header("Content Settings")]
    [SerializeField]
    public ShardContent contentType = ShardContent.None;
    
    [Header("Inner World Adjustments")]
    public Vector3 contentOffset = Vector3.zero;
    public Vector3 contentRotation = Vector3.zero;
    public float contentScale = 0.35f;
    
    [Header("Leave empty to auto-find")]
    public Shader glassShader;

    void Start()
    {
        SetupShard();
    }

    void SetupShard()
    {
        if (glassShader == null)
            glassShader = Shader.Find("Custom/BlueGlassAmberRim");

        // Apply Glass Material
        if (glassShader != null)
        {
            Renderer r = GetComponent<Renderer>();
            if (r != null)
            {
                r.enabled = true; // Ensure it is visible
                r.material = new Material(glassShader);
                r.material.SetColor("_BaseColor", new Color(0.2f, 0.6f, 1.0f, 0.05f)); 
                r.material.SetColor("_RimColor", new Color(1.0f, 0.6f, 0.0f)); 
                r.material.SetFloat("_RimPower", 2.0f); 
                r.material.SetFloat("_RimIntensity", 1.5f);
            }
        }
        
        // Remove any previous "thick glass" visual hack if it exists
        Transform thickVisual = transform.Find("GlassThicknessVisual");
        if (thickVisual != null) DestroyImmediate(thickVisual.gameObject);

        // Inner World Setup
        Transform oldInner = transform.Find("InnerWorld");
        if (oldInner != null) DestroyImmediate(oldInner.gameObject);

        if (contentType == ShardContent.None) return;

        GameObject innerWorld = new GameObject("InnerWorld");
        innerWorld.transform.parent = transform;
        
        // Center calculation + User Offset
        Renderer ren = GetComponent<Renderer>();
        Vector3 centerPos = Vector3.zero; // Default local center
        if (ren != null)
        {
             // Try to center based on bounds, but let user override
             if (contentOffset != Vector3.zero)
                centerPos = contentOffset;
             else
                centerPos = transform.InverseTransformPoint(ren.bounds.center);
        }
        innerWorld.transform.localPosition = centerPos;
        
        // Rotation + User Rotation
        innerWorld.transform.localRotation = Quaternion.Euler(contentRotation);
        
        // Scale
        innerWorld.transform.localScale = Vector3.one * contentScale; 

        switch (contentType)
        {
            case ShardContent.Ocean: CreateOceanWorld(innerWorld.transform); break;
            case ShardContent.Tree: CreateTreeWorld(innerWorld.transform); break;
            case ShardContent.Cat: CreateCatWorld(innerWorld.transform); break;
        }

        // --- Interaction Components ---
        // Ensure Collider for Raycast
        if (GetComponent<Collider>() == null)
        {
            // Default to SphereCollider, user can replace with MeshCollider if needed
            SphereCollider sc = gameObject.AddComponent<SphereCollider>();
            sc.radius = 0.5f; // Approximate size for interaction
        }

        // Ensure Interaction Script (Visual Feedback & Selection)
        if (GetComponent<ShardInteraction>() == null)
        {
            gameObject.AddComponent<ShardInteraction>();
        }
    }
    
    // --- Procedural Art Generators ---
    
    // Helper to create glowing "suspended" particles
    GameObject CreateParticle(Transform parent, Vector3 localPos, Vector3 scale, Color color, float emissionIntensity = 2.0f)
    {
        GameObject p = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        p.transform.parent = parent;
        p.transform.localPosition = localPos;
        p.transform.localScale = scale;
        
        Shader s = Shader.Find("Universal Render Pipeline/Lit");
        if (s == null) s = Shader.Find("Universal Render Pipeline/Simple Lit");
        if (s == null) s = Shader.Find("Standard");

        Material mat = new Material(s);
        // Ensure color is set correctly for URP
        mat.color = color;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);

        // MATTE FINISH: Prevent white specular highlights
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.0f);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.0f);
        
        // Only enable emission if intensity > 0
        if (emissionIntensity > 0.01f)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * emissionIntensity);
        }
        else
        {
            mat.DisableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", Color.black);
        }
        
        if (color.a < 1.0f) 
        {
            mat.SetFloat("_Surface", 1);
            mat.SetFloat("_Blend", 0);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = 3000;
        }

        p.GetComponent<Renderer>().material = mat;
        Destroy(p.GetComponent<Collider>());
        
        return p;
    }

    void CreateOceanWorld(Transform parent)
    {
        // Re-implementing the nicer "Rising Bubbles" cluster (from previous better version)
        // Ensure randomness per shard (since Tree might fix seed)
        Random.InitState(GetInstanceID() + (int)(Time.realtimeSinceStartup * 1000));

        // Layer 1: Large Faint Bubbles (Dreamy Background)
        for (int i = 0; i < 8; i++)
        {
            Vector3 pos = Random.insideUnitSphere * 0.35f;
            pos.y = Random.Range(-0.3f, 0.3f);
            
            float size = Random.Range(0.08f, 0.15f);
            Color col = new Color(0.0f, 0.5f, 1.0f, 0.3f); // Blue-er
            CreateParticle(parent, pos, Vector3.one * size, col, 1.5f); // Slightly brighter background
        }

        // Layer 2: Medium Bubbles (Standard) - Boosting brightness + Blue Mix
        for (int i = 0; i < 20; i++)
        {
            Vector3 pos = Random.insideUnitSphere * 0.4f;
            pos.y = Random.Range(-0.4f, 0.4f);
            
            float size = Random.Range(0.04f, 0.07f);
            // Mix: More Blue, Less Cyan
            Color col;
            if (Random.value > 0.4f) 
                col = new Color(0.0f, 0.7f, 1.0f, 0.8f); // Sky Blue
            else 
                col = new Color(0.0f, 0.2f, 1.0f, 0.9f); // Deep Blue
            
            CreateParticle(parent, pos, Vector3.one * size, col, 5.5f); // Reduced slightly (was 8.0)
        }

        // Layer 3: Tiny Glitter Bubbles (Dense) - Boosting brightness + Blue/Colorful Mix
        for (int i = 0; i < 40; i++)
        {
            Vector3 pos = Random.insideUnitSphere * 0.45f; 
            pos.y = Random.Range(-0.45f, 0.45f);
            
            float size = Random.Range(0.015f, 0.03f);
            
            // Mix: Electric Blue focus
            Color col;
            if (Random.value > 0.3f)
                col = new Color(0.3f, 0.8f, 1.0f, 0.95f); // Bright Blue
            else
                col = new Color(0.0f, 0.4f, 1.0f, 0.95f); // Electric Blue
                
            CreateParticle(parent, pos, Vector3.one * size, col, 8.5f); // Reduced slightly (was 12.0)
        }
    }

    void CreateTreeWorld(Transform parent)
    {
        // Tree Logic (Simple upright structure)
        // Fix random seed so tree structure is consistent and doesn't "rotate" randomly
        Random.InitState(12345); 

        // User requested "slightly rotate 30 degrees to my left"
        GameObject treeContainer = new GameObject("TreeContainer");
        treeContainer.transform.parent = parent;
        treeContainer.transform.localPosition = Vector3.zero;
        treeContainer.transform.localScale = Vector3.one;
        // Rotate 30 on Y - confirming this is the "just now" angle user liked
        treeContainer.transform.localRotation = Quaternion.Euler(0, 30, 0);

        // Darker Black-Brown (Slightly brightened for glow)
        Color woodCol = new Color(0.2f, 0.12f, 0.05f, 1.0f); 
        float treeGlow = 6.0f; // Significantly boosted for visibility
        
        // Trunk (Shorter & Lower)
        for (int i = 0; i < 5; i++) 
        {
            Vector3 pos = new Vector3(Random.Range(-0.02f, 0.02f), -0.35f + i * 0.07f, Random.Range(-0.02f, 0.02f));
            float scale = 0.12f * (1.0f - (i/7.0f));
            CreateParticle(treeContainer.transform, pos, Vector3.one * scale, woodCol, treeGlow);
        }

        // Branches (Fine, Many, 3D spread)
        int branchCount = 12; 
        float yStart = 0.0f;
        
        for (int i = 0; i < branchCount; i++)
        {
            // Spherical spread, but maybe bias X slightly if user wants "left and right"
            Vector3 dir = Random.onUnitSphere;
            if (dir.y < 0) dir.y *= -1; 
            dir.y += 0.5f; 
            
            // Bias X expansion (left/right) vs Z (depth)
            dir.x *= 1.5f; 
            
            dir.Normalize();

            Vector3 startOs = new Vector3(0, yStart - 0.1f + Random.Range(0, 0.1f), 0);
            Vector3 endPos = startOs + dir * Random.Range(0.25f, 0.45f);
            
            CreateBranch(treeContainer.transform, startOs, endPos, woodCol, 0.04f, treeGlow); 
        }
    }
    
    // Helper for creating tree branches
    void CreateBranch(Transform parent, Vector3 startOffset, Vector3 endPos, Color color, float baseScale = 0.08f, float emission = 1.5f)
    {
        Vector3 direction = (endPos - startOffset).normalized;
        float distance = Vector3.Distance(startOffset, endPos);
        
        int segments = Mathf.CeilToInt(distance / 0.06f); 
        for (int i = 0; i < segments; i++)
        {
            Vector3 pos = startOffset + direction * (i * distance / segments);
            // Tapering
            float scale = baseScale * (1.0f - (i * 0.8f / (float)segments));
            CreateParticle(parent, pos, Vector3.one * scale, color, emission);
        }
    }

    void CreateCatWorld(Transform parent)
    {
        // Cat Logic (Sitting posture)
        GameObject catContainer = new GameObject("CatBody");
        catContainer.transform.parent = parent;
        catContainer.transform.localPosition = Vector3.zero;
        catContainer.transform.localScale = Vector3.one;
        // Rotate 55 on Y as requested previously
        catContainer.transform.localRotation = Quaternion.Euler(0, 55, 0);

        // White Body (Swapped)
        Color catCol = Color.white; 
        float glowIntensity = 1.2f; // Low glow for body
        
        // Body (The "Butt Ball")
        CreateParticle(catContainer.transform, new Vector3(0, -0.15f, 0.15f), Vector3.one * 0.45f, catCol, glowIntensity);
        
        // Head (Smaller, on top/front)
        // Moved head slightly forward (-0.1)
        CreateParticle(catContainer.transform, new Vector3(0, 0.15f, -0.1f), Vector3.one * 0.3f, catCol, glowIntensity);
        
        // Ears (Half-hidden "Semi-circles")
        // Lower Y to sink into head (0.28 instead of 0.32)
        // Slightly wider base (0.12) and shorter (0.08)
        CreateParticle(catContainer.transform, new Vector3(-0.1f, 0.28f, -0.1f), new Vector3(0.12f, 0.08f, 0.05f), catCol, glowIntensity).transform.localRotation = Quaternion.Euler(0, 0, 25);
        CreateParticle(catContainer.transform, new Vector3(0.1f, 0.28f, -0.1f), new Vector3(0.12f, 0.08f, 0.05f), catCol, glowIntensity).transform.localRotation = Quaternion.Euler(0, 0, -25);
        
        // Eyes (Dark Gray + High Emission = Metallic/Electric Gray)
        Color eyeCol = new Color(0.25f, 0.25f, 0.25f, 1.0f);
        CreateParticle(catContainer.transform, new Vector3(-0.08f, 0.18f, -0.22f), Vector3.one * 0.06f, eyeCol, 0.0f); // NO Emission
        CreateParticle(catContainer.transform, new Vector3(0.08f, 0.18f, -0.22f), Vector3.one * 0.06f, eyeCol, 0.0f);
        
        // Whiskers
        for(int i=1; i<=2; i++) {
             CreateParticle(catContainer.transform, new Vector3(0.12f + i*0.05f, 0.12f, -0.2f), Vector3.one * 0.02f, catCol, 1.0f);
             CreateParticle(catContainer.transform, new Vector3(-0.12f - i*0.05f, 0.12f, -0.2f), Vector3.one * 0.02f, catCol, 1.0f);
        }
        
        // Tail (Wrapped around to FRONT)
        int segments = 15; 
        for(int i=0; i<segments; i++)
        {
            float t = i / (float)segments;
            
            // Wrap from Back (Z+) -> Side (X+) -> Front (Z-)
            // Range t: 0 to 1
            // Angle: 0 to PI (180 deg)
            
            float angle = t * Mathf.PI; 
            float radius = 0.32f; // Close to body
            
            // Center around body (0, -0.15, 0.15)
            float x = Mathf.Sin(angle) * radius; 
            float z = Mathf.Cos(angle) * (radius) + 0.15f; // +0.15 to center on body Z
            float y = -0.25f + Mathf.Sin(t * Mathf.PI) * 0.05f; // Slight curve up
            
            // Adjust position so it doesn't clip too much but wraps
            // Bias x slightly to prevent clipping if body is wide
            if (x > 0) x += 0.05f;

            Vector3 pos = new Vector3(x, y, z); 
           
            float scale = Mathf.Lerp(0.11f, 0.04f, t);
            
            CreateParticle(catContainer.transform, pos, Vector3.one * scale, catCol, glowIntensity);
        }
    }
}


// Simple Helper for material transparency
public static class Utils {
    public static void SetTransparent(Material material) {
        material.SetFloat("_Mode", 3);
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = 3000;
    }
}

