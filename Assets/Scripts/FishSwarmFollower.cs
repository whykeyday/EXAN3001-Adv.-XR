using UnityEngine;

/// <summary>
/// Attach to the Fish Container (FBX model). 
/// It slowly follows the player's hands.
/// </summary>
public class FishSwarmFollower : MonoBehaviour
{
    public float followSpeed = 0.5f;
    public float rotationSpeed = 2f;
    
    // Hand tracking logic
    private Transform[] hands;

    void Start()
    {
        // Try to find player hands by tag
        GameObject[] handObjs = GameObject.FindGameObjectsWithTag("PlayerHand");
        hands = new Transform[handObjs.Length];
        for(int i=0; i<handObjs.Length; i++) hands[i] = handObjs[i].transform;
        
        // 建议用户在此模型上先用 ParticleContainerTool 生成粒子系统，再挂载本脚本，使其跟随
    }

    void Update()
    {
        // If hands not found yet (spawned later/dynamically by XR Rig), try finding them
        if (hands == null || hands.Length == 0)
        {
            GameObject[] handObjs = GameObject.FindGameObjectsWithTag("PlayerHand");
            if (handObjs.Length > 0)
            {
                hands = new Transform[handObjs.Length];
                for(int i=0; i<handObjs.Length; i++) hands[i] = handObjs[i].transform;
            }
        }

        if (hands != null && hands.Length > 0)
        {
            // Calculate center point of all hands
            Vector3 targetPos = Vector3.zero;
            int validCount = 0;
            foreach(var h in hands)
            {
                if (h != null)
                {
                    targetPos += h.position;
                    validCount++;
                }
            }
            if (validCount > 0)
            {
                targetPos /= validCount;
                
                // Add a little offset so fish swim around hands, not directly inside them
                targetPos += Camera.main != null ? Camera.main.transform.forward * 0.4f : Vector3.forward * 0.4f;

                // Move slowly towards target
                transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * followSpeed);

                // Rotate towards movement direction smoothly
                Vector3 dir = (targetPos - transform.position).normalized;
                if (dir.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(dir);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSpeed);
                }
            }
        }
    }
}
