using UnityEngine;

/// <summary>
/// Attach to the Fish Container (FBX model).
/// Fish are summoned by waving your hand (left-right gesture detection).
/// Once summoned, they follow with a delayed tailing feel using a position buffer.
/// Supports hand tracking — when controllers are put down, fish follow ghost hand instead.
/// </summary>
public class FishSwarmFollower : MonoBehaviour
{
    [Header("Follow Settings")]
    public float followSpeed = 2.5f; // 提高平滑插值速度，避免过慢
    public float rotationSpeed = 3.5f;
    [Tooltip("鱼群悬浮在手前方的偏移距离")]
    public float forwardOffset = 0.35f;

    [Header("Wave Gesture (挥手召唤)")]
    [Tooltip("降低阈值，让召唤更容易")]
    public int waveThreshold = 2; // 2次转向即可触发
    public float waveTimeout = 1.0f;
    public float summonDuration = 20f; // 延长跟随时间
    [Tooltip("降低位移判定，更敏感")]
    public float waveMinDelta = 0.015f; 

    [Header("Position Buffer (延迟拖尾)")]
    [Tooltip("平衡缓冲，让拖尾更灵动")]
    public int bufferSize = 45; 

    private enum SwarmState { Idle, Summoned }
    private SwarmState state = SwarmState.Idle;

    private Transform targetHand; // 锁定当前召唤的那只手
    // Wave detection
    private Vector3 lastHandPos;
    private int waveDirectionChanges = 0;
    private float firstChangeTime = 0f;
    private float lastMoveX = 0f; 
    private bool hasLastDirection = false;

    // Summon timer
    private float summonTimer = 0f;

    // Position ring buffer
    private Vector3[] positionBuffer;
    private int bufferWriteIndex = 0;
    private bool bufferFilled = false;

    void Start()
    {
        positionBuffer = new Vector3[bufferSize];
        for (int i = 0; i < bufferSize; i++)
            positionBuffer[i] = transform.position;
    }

    void Update()
    {
        // 定期寻找手
        if (targetHand == null || !targetHand.gameObject.activeInHierarchy)
        {
            GameObject[] handObjs = GameObject.FindGameObjectsWithTag("PlayerHand");
            if (handObjs.Length > 0)
            {
                // 优先选正在动的手
                targetHand = handObjs[0].transform;
            }
        }

        if (targetHand == null) return;

        Vector3 currentHandPos = targetHand.position;

        switch (state)
        {
            case SwarmState.Idle:
                DetectWaveGesture(currentHandPos);
                // 闲置时在周围缓慢游动
                transform.Rotate(Vector3.up, 10f * Time.deltaTime);
                break;

            case SwarmState.Summoned:
                summonTimer -= Time.deltaTime;
                if (summonTimer <= 0f)
                {
                    state = SwarmState.Idle;
                    break;
                }

                // 目标位置：手的前方
                Vector3 targetPos = currentHandPos + targetHand.forward * forwardOffset;

                // 写入缓冲区
                positionBuffer[bufferWriteIndex] = targetPos;
                bufferWriteIndex = (bufferWriteIndex + 1) % positionBuffer.Length;
                if (bufferWriteIndex == 0) bufferFilled = true;

                int readIndex = bufferFilled ? (bufferWriteIndex + 1) % positionBuffer.Length : 0;
                Vector3 delayedTarget = positionBuffer[readIndex];

                // 核心移动：平滑跟随
                transform.position = Vector3.Lerp(transform.position, delayedTarget, Time.deltaTime * followSpeed);

                // 旋转：平滑看向移动方向
                Vector3 moveDir = (delayedTarget - transform.position).normalized;
                if (moveDir.sqrMagnitude > 0.001f)
                {
                    Quaternion lookRot = Quaternion.LookRotation(moveDir);
                    transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * rotationSpeed);
                }
                break;
        }
    }

    void DetectWaveGesture(Vector3 currentHandPos)
    {
        if (!hasLastDirection)
        {
            lastHandPos = currentHandPos;
            hasLastDirection = true;
            return;
        }

        float deltaX = currentHandPos.x - lastHandPos.x;
        if (Mathf.Abs(deltaX) < waveMinDelta) return;

        float currentDir = Mathf.Sign(deltaX);
        if (lastMoveX != 0f && currentDir != lastMoveX)
        {
            if (waveDirectionChanges == 0) firstChangeTime = Time.time;
            waveDirectionChanges++;

            if (Time.time - firstChangeTime > waveTimeout)
            {
                ResetWaveDetection();
                return;
            }

            if (waveDirectionChanges >= waveThreshold)
            {
                state = SwarmState.Summoned;
                summonTimer = summonDuration;
                ResetWaveDetection();
                
                // 重置缓冲区防止瞬移
                for (int i = 0; i < positionBuffer.Length; i++)
                    positionBuffer[i] = transform.position;
            }
        }

        lastMoveX = currentDir;
        lastHandPos = currentHandPos;
    }

    void ResetWaveDetection()
    {
        waveDirectionChanges = 0;
        lastMoveX = 0f;
        hasLastDirection = false;
    }
}
