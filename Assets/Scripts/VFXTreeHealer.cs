using UnityEngine;
using UnityEngine.VFX;

public class VFXTreeHealer : MonoBehaviour
{
    [Header("References")]
    public VisualEffect treeVFX;
    public MeshRenderer treeMesh;
    
    [Header("Healing Settings")]
    public float healingDistance = 1.0f;
    public float healingRate = 0.5f;
    public float decayRate = 0.1f;
    
    [Header("Colors - Withered")]
    public Color witheredColor = new Color(0.4f, 0.25f, 0.1f, 1f); // Brown
    
    [Header("Colors - Alive")]
    public Color aliveColor = new Color(0.1f, 0.9f, 0.2f, 1f); // Emerald Green
    
    [Range(0f, 1f)]
    public float energyLevel = 0f;
    
    private void Start()
    {
        energyLevel = 0f;
        ApplyTreeState();
    }
    
    private void Update()
    {
        bool isHealing = false;
        
        // Find all player hands
        GameObject[] hands = GameObject.FindGameObjectsWithTag("PlayerHand");
        foreach (var hand in hands)
        {
            float dist = Vector3.Distance(hand.transform.position, transform.position);
            // Also check distance to mesh bounds if it exists, to support touching outer branches
            if (treeMesh != null && treeMesh.bounds.Contains(hand.transform.position))
            {
                isHealing = true;
                break;
            }
            if (dist < healingDistance)
            {
                isHealing = true;
                break;
            }
        }
        
        if (isHealing)
        {
            energyLevel += healingRate * Time.deltaTime;
        }
        else
        {
            energyLevel -= decayRate * Time.deltaTime;
        }
        energyLevel = Mathf.Clamp01(energyLevel);
        
        ApplyTreeState();
    }
    
    private void ApplyTreeState()
    {
        Color currentColor = Color.Lerp(witheredColor, aliveColor, energyLevel);
        
        // --- 1. Control VFX Graph (The Green Particles) ---
        if (treeVFX != null)
        {
            // If the tree is completely withered (energy is low), completely hide the green particles
            // Since the VFX Graph colors are hardcoded and unexposed, this is the safest way to ensure it looks "dead"
            if (energyLevel < 0.05f) 
            {
                if (treeVFX.enabled) treeVFX.enabled = false;
            }
            else 
            {
                if (!treeVFX.enabled) 
                {
                    treeVFX.enabled = true;
                    treeVFX.Play(); // Trigger particles to spawn
                }
            }
            
            // Try to set overrides if they happen to exist
            if (treeVFX.HasVector4("Color")) treeVFX.SetVector4("Color", currentColor);
            if (treeVFX.HasVector4("BaseColor")) treeVFX.SetVector4("BaseColor", currentColor);
            if (treeVFX.HasFloat("SpawnRate")) treeVFX.SetFloat("SpawnRate", Mathf.Lerp(10f, 200f, energyLevel));
            if (treeVFX.HasFloat("Size")) treeVFX.SetFloat("Size", Mathf.Lerp(0.02f, 0.08f, energyLevel));
                
            // Force Tint the actual renderer Materials (Fallback)
            Renderer vfxRenderer = treeVFX.GetComponent<Renderer>();
            if (vfxRenderer != null)
            {
                foreach (Material m in vfxRenderer.materials)
                {
                    if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", currentColor);
                    if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", currentColor * Mathf.Lerp(0.5f, 3.0f, energyLevel));
                }
            }
        }
        
        // --- 2. Control Tree Mesh (The Trunk) ---
        if (treeMesh != null && treeMesh.material != null)
        {
            Material mat = treeMesh.material;
            
            // Force Color
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", currentColor);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", currentColor);
            mat.color = currentColor;
            
            // CRITICAL: Strip any textures that might be forcing the trunk to be green
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", null);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", null);
            
            // Control Emission based on energy
            if (energyLevel > 0.1f)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", currentColor * Mathf.Lerp(0.5f, 3.0f, energyLevel));
            }
            else
            {
                mat.DisableKeyword("_EMISSION");
                if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", Color.black);
            }
        }
        
        // Scale and grow upward
        transform.localScale = Vector3.one * Mathf.Lerp(0.8f, 1.2f, energyLevel);
    }
    
    // Commented out to prevent legacy runtime destruction effects
    // [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void AutoSetupTree()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "TreeScene") return;
        
        // Setup Tree
        GameObject deadTree = GameObject.Find("VFX_DeadTree");
        if (deadTree != null)
        {
            VFXTreeHealer healer = deadTree.GetComponent<VFXTreeHealer>();
            if (healer == null) healer = deadTree.AddComponent<VFXTreeHealer>();
            
            healer.treeVFX = deadTree.GetComponent<VisualEffect>();
            
            Transform treeMeshTrans = deadTree.transform.Find("TreeMesh");
            if (treeMeshTrans != null)
            {
                healer.treeMesh = treeMeshTrans.GetComponent<MeshRenderer>();
                
                MeshCollider mc = treeMeshTrans.GetComponent<MeshCollider>();
                if (mc == null)
                {
                    mc = treeMeshTrans.gameObject.AddComponent<MeshCollider>();
                    mc.convex = true;
                    mc.isTrigger = true;
                    
                    Rigidbody rb = treeMeshTrans.gameObject.AddComponent<Rigidbody>();
                    rb.isKinematic = true;
                    rb.useGravity = false;
                }
            }
        }
        
        // Remove Probe
        ReflectionProbe[] probes = FindObjectsOfType<ReflectionProbe>();
        foreach (var p in probes)
        {
            if (p != null && p.gameObject.name == "Reflection Probe") Destroy(p.gameObject);
            else if (p != null) Destroy(p);
        }
    }
}
