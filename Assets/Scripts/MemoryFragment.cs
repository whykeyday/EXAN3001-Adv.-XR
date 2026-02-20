using UnityEngine;

/// <summary>
/// Attach this to each Memory Fragment in the Hub.
/// When the player's hand touches it, triggers the scene assembly.
/// </summary>
[RequireComponent(typeof(Collider))]
public class MemoryFragment : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Which scene does this fragment represent? 0=Forest, 1=Ocean, 2=Cat")]
    public int sceneIndex = 0;

    [Tooltip("Reference to the SceneAssembler in the scene")]
    public SceneAssembler sceneAssembler;

    private void Start()
    {
        // Ensure collider is set to trigger
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        // Auto-find SceneAssembler if not assigned
        if (sceneAssembler == null)
        {
            sceneAssembler = FindObjectOfType<SceneAssembler>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check for player hand
        if (other.CompareTag("PlayerHand"))
        {
            Debug.Log($"MemoryFragment: Hand touched fragment {sceneIndex}!");
            
            if (sceneAssembler != null)
            {
                sceneAssembler.SelectScene(sceneIndex);
            }
            else
            {
                Debug.LogError("MemoryFragment: SceneAssembler reference is missing!");
            }
        }
    }
}
