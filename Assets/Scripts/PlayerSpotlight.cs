using UnityEngine;

/// <summary>
/// 玩家脚下渐变自发光光晕：跟随 XR 摄像机的 XZ 位置。
/// 使用粒子系统创建中心亮、边缘渐变透明的柔和光圈。
/// </summary>
public class PlayerSpotlight : MonoBehaviour
{
    [Header("Glow Settings")]
    public Color lightColor = new Color(1f, 1f, 1.0f, 0.45f); // 调为暖白色，更透明
    [Tooltip("光圈半径")]
    public float glowRadius = 1.2f;
    [Tooltip("光圈亮度 (HDR)")]
    public float glowIntensity = 1.5f; // 降低亮度，避免照片中的“大白球”
    [Tooltip("光圈距地面高度")]
    public float groundOffset = 0.015f;
    [Tooltip("地面检测层级 (建议勾选 Default/Ground，排除 Player 手部层级)")]
    public LayerMask groundMask = -1;

    [Header("Follow")]
    [Tooltip("手动指定跟随目标，否则自动搜索 XR Camera")]
    public Transform followTarget;

    private ParticleSystem glowPS;
    private Light spotLight;

    void Start()
    {
        CreateGlowParticles();
        
        // 自动排除 Player 层级 (假设层级为 3 或名为 Player)
        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer != -1) groundMask &= ~(1 << playerLayer);
        
        // 默认让光圈极其隐形
        if (glowPS != null) glowPS.gameObject.layer = 2; // Ignore Raycast
    }

    void CreateGlowParticles()
    {
        GameObject glowObj = new GameObject("GroundGlow_Diffuse");
        glowObj.transform.SetParent(transform, false);

        glowPS = glowObj.AddComponent<ParticleSystem>();
        glowPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = glowPS.main;
        main.loop = true;
        main.startLifetime = 999f;
        main.startSpeed = 0f;
        main.startSize = glowRadius * 2.2f; // 稍大一点点
        main.maxParticles = 1;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = new Color(lightColor.r, lightColor.g, lightColor.b, 0.35f); 
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;

        var psr = glowPS.GetComponent<ParticleSystemRenderer>();
        psr.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
        psr.material = CreateSofterMaterial();

        glowPS.Play();
    }

    Material CreateSofterMaterial()
    {
        // 极其柔和的 Gaussian 生成，消除照片里的硬边缘
        int texSize = 128;
        Texture2D tex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;

        float center = texSize / 2f;
        for (int y = 0; y < texSize; y++)
        {
            for (int x = 0; x < texSize; x++)
            {
                float dx = (x - center) / (texSize / 2f);
                float dy = (y - center) / (texSize / 2f);
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                // 强化柔化：Exp(-dist * dist * 6.5) 让边缘极度稀释
                float falloff = Mathf.Exp(-dist * dist * 6.5f);
                float alpha = Mathf.Clamp01(falloff);
                if (dist > 0.95f) alpha = 0f;

                tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
            }
        }
        tex.Apply();

        Shader s = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (s == null) s = Shader.Find("Sprites/Default");
        
        Material mat = new Material(s);
        mat.SetTexture("_BaseMap", tex);
        mat.mainTexture = tex;
        mat.SetColor("_BaseColor", lightColor * glowIntensity);
        
        // 强制开启透明 Additive 混合逻辑，但更克制
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        mat.SetInt("_ZWrite", 0);
        return mat;
    }

    void LateUpdate()
    {
        // 黄金法则：在 VR 中，每一帧刷新 Main Camera 的位置
        if (followTarget == null)
        {
            Camera cam = Camera.main;
            if (cam == null) cam = FindObjectOfType<Camera>();
            if (cam != null) followTarget = cam.transform;
        }

        if (followTarget != null)
        {
            Vector3 rayStart = followTarget.position;
            float targetY = 0f;

            // 向下照射 20 米寻找真实地板
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 20f, groundMask))
            {
                targetY = hit.point.y + groundOffset;
            }
            else
            {
                // 如果没扫到地板，至少保证在海平面的默认高度，防止光圈穿帮
                targetY = groundOffset; 
            }

            // 完美的 XZ 位移同步
            Vector3 targetPos = new Vector3(followTarget.position.x, targetY, followTarget.position.z);
            
            // 重要修复：不再移动脚本自身的 Transform (防止挂在玩家身上时拉扯玩家)
            // 直接移动渲染用的粒子和灯光
            if (glowPS != null)
            {
                // 使用平滑跟随，但仅作用于光圈视觉
                glowPS.transform.position = Vector3.Lerp(glowPS.transform.position, targetPos, Time.deltaTime * 15f);
            }

            if (spotLight != null)
            {
                spotLight.transform.position = new Vector3(targetPos.x, targetY + 4f, targetPos.z);
            }
        }
    }
}
