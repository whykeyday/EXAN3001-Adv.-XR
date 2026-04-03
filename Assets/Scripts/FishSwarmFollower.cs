using UnityEngine;

/// <summary>
/// Attach to the Fish Container (FBX model). 
/// It slowly follows the player's hands (controllers OR ghost hand fingertips).
/// Supports hand tracking — when controllers are put down, fish follow ghost hand instead.
/// </summary>
public class FishSwarmFollower : MonoBehaviour
{
    public float followSpeed = 0.5f;
    public float rotationSpeed = 2f;
    [Tooltip("鱼群悬浮在手前方的偏移距离")]
    public float forwardOffset = 0.4f;
    
    private Transform[] hands;
    private float searchInterval = 0.5f;
    private float nextSearchTime = 0f;

    void Start()
    {
        FindHands();
    }

    void FindHands()
    {
        // 搜索所有标记为 PlayerHand 的物体
        // 包括：XR 手柄上的控制器、GhostHandVisualizer 创建的指尖碰撞体
        GameObject[] handObjs = GameObject.FindGameObjectsWithTag("PlayerHand");
        
        if (handObjs.Length > 0)
        {
            hands = new Transform[handObjs.Length];
            for (int i = 0; i < handObjs.Length; i++)
            {
                hands[i] = handObjs[i].transform;
            }
        }
    }

    void Update()
    {
        // 周期性重新搜索手（手可能在手柄/手追踪之间切换）
        if (Time.time > nextSearchTime)
        {
            FindHands();
            nextSearchTime = Time.time + searchInterval;
        }

        if (hands == null || hands.Length == 0) return;

        // 计算所有有效手的中心点
        Vector3 targetPos = Vector3.zero;
        int validCount = 0;
        foreach (var h in hands)
        {
            if (h != null && h.gameObject.activeInHierarchy)
            {
                targetPos += h.position;
                validCount++;
            }
        }

        if (validCount > 0)
        {
            targetPos /= validCount;
            
            // 鱼群在手前方一点点游动
            Camera cam = Camera.main;
            if (cam != null)
            {
                targetPos += cam.transform.forward * forwardOffset;
            }
            else
            {
                targetPos += Vector3.forward * forwardOffset;
            }

            // 平滑跟随
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * followSpeed);

            // 朝移动方向旋转
            Vector3 dir = (targetPos - transform.position).normalized;
            if (dir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSpeed);
            }
        }
    }
}
