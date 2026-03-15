using UnityEngine;
using UnityEditor;
using UnityEngine.VFX;

public class SetupVFXTreeScene : Editor
{
    [MenuItem("Tools/Fix Tree Scene (Remove Probe & Setup Tree)")]
    [InitializeOnLoadMethod]
    public static void FixScene()
    {
        // Only run if we are in TreeScene
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "TreeScene") return;
        
        // 1. Setup the tree
        GameObject deadTree = GameObject.Find("VFX_DeadTree");
        if (deadTree != null)
        {
            VFXTreeHealer healer = deadTree.GetComponent<VFXTreeHealer>();
            if (healer == null)
            {
                healer = deadTree.AddComponent<VFXTreeHealer>();
            }
            
            healer.treeVFX = deadTree.GetComponent<VisualEffect>();
            
            Transform treeMeshTrans = deadTree.transform.Find("TreeMesh");
            if (treeMeshTrans != null)
            {
                healer.treeMesh = treeMeshTrans.GetComponent<MeshRenderer>();
                
                // Add collider if missing so the hands can touch it
                MeshCollider mc = treeMeshTrans.GetComponent<MeshCollider>();
                if (mc == null)
                {
                    mc = treeMeshTrans.gameObject.AddComponent<MeshCollider>();
                    mc.convex = true;
                    mc.isTrigger = true;
                    
                    // Add Rigidbody as well to ensure physics triggers work with standard XR hands
                    Rigidbody rb = treeMeshTrans.gameObject.AddComponent<Rigidbody>();
                    rb.isKinematic = true;
                    rb.useGravity = false;
                }
            }
            
            Debug.Log("✅ Tree Healer setup complete on VFX_DeadTree!");
        }
        else
        {
            Debug.LogWarning("⚠️ VFX_DeadTree not found in the scene.");
        }

        // 2. Remove the Reflection Probe safely
        ReflectionProbe[] probes = FindObjectsOfType<ReflectionProbe>();
        int removedCount = 0;
        foreach (var p in probes)
        {
            if (p != null)
            {
                // Only destroy the Component itself, or the GameObject if it's explicitly a dummy object
                if (p.gameObject.name == "Reflection Probe")
                {
                    DestroyImmediate(p.gameObject); // It's just an empty probe object
                }
                else
                {
                    DestroyImmediate(p); // Just rip the component off whatever it's attached to
                }
                removedCount++;
            }
        }

        if (removedCount > 0)
        {
            Debug.Log($"✅ Removed {removedCount} Reflection Probe(s) from the scene safely.");
        }
        
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
    }
}
