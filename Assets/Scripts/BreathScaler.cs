using UnityEngine;

/// <summary>
/// 将此脚本挂载到需要随呼吸变大的物体（如珊瑚的父物体）上。
/// </summary>
public class BreathScaler : MonoBehaviour
{
    [Tooltip("呼吸到最大时，物体放大的倍数（比如 1.2 表示大 20%）")]
    public float maxScaleMultiplier = 1.15f;
    
    [Tooltip("缩放的平滑度，数字越小越迟缓，越大越跟手")]
    public float smoothness = 4f;

    private Vector3 initialScale;
    private Vector3 initialLocalPos;
    private Vector3 initialCenterLocal;
    private BreathInputManager breathManager;

    void Start()
    {
        initialScale = transform.localScale;
        initialLocalPos = transform.localPosition;
        
        breathManager = FindObjectOfType<BreathInputManager>();

        Renderer rend = GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            // 获取网格中心点相对于自身原点的本地坐标（解决 FBX 锚点便宜的终极方案）
            initialCenterLocal = transform.InverseTransformPoint(rend.bounds.center);
        }
        else
        {
            initialCenterLocal = Vector3.zero;
        }
    }

    void Update()
    {
        float breath = 0f;
        if (breathManager != null)
        {
            breath = breathManager.BreathValue;
        }

        // 计算目标大小
        float currentMultiplier = Mathf.Lerp(1f, maxScaleMultiplier, breath);
        Vector3 targetScale = initialScale * currentMultiplier;

        // 1. 先用平滑过渡求出当前这一帧应该放大到多少
        Vector3 smoothedScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * smoothness);
        
        // 2. 纯数学计算：如果我们直接放大物体，虚拟中心点会被拉扯偏离多少？
        Vector3 originalScaledCenter = Vector3.Scale(initialCenterLocal, initialScale);
        Vector3 newScaledCenter = Vector3.Scale(initialCenterLocal, smoothedScale);
        
        // 考虑物体自身的旋转，算出真正的偏移距离
        Vector3 diff = transform.localRotation * (newScaledCenter - originalScaledCenter);

        // 3. 把位移差距用 LocalPosition 反向补偿回去！
        transform.localScale = smoothedScale;
        transform.localPosition = initialLocalPos - diff;
    }
}
