using UnityEngine;

/// <summary>
/// 玩家脚下渐变自发光光晕：跟随 XR 摄像机的 XZ 位置。
/// 最终回归版：高斯三重发光结构 (Core 5%, Halo 20%, Outer 100%)。
/// 规格：半径 0.15, 亮度 0.4, 优先级 5。
/// </summary>
public class PlayerSpotlight : MonoBehaviour
{
    [Header("Glow Aesthetics (Refined Gaussian)")]
    public Color lightColor = new Color(1.0f, 0.72f, 0.3f, 1.0f); 
    public float glowRadius = 0.05f;      // 半径 0.05 (极小，刚好盖脚)
    public float glowIntensity = 0.10f;   // 亮度 0.1 (极柔)
    public float groundOffset = 0.012f;

    [Header("Follow Target")]
    public Transform followTarget;

    private SpriteRenderer glowRenderer;
    private float currentYVelocity; 
    private float lastValidGroundY = 0f;
    private Sprite generatedSprite;

    void Start()
    {
        CreateHighQualityGlow();
        lastValidGroundY = transform.position.y - 1.6f;
    }

    void CreateHighQualityGlow()
    {
        GameObject glowObj = new GameObject("GroundGlow_Gaussian_Final");
        glowObj.transform.SetParent(transform, false);
        glowRenderer = glowObj.AddComponent<SpriteRenderer>();
        
        int texSize = 256; 
        Texture2D tex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;

        float center = texSize / 2f;
        Color baseColor = lightColor;
        
        for (int y = 0; y < texSize; y++)
        {
            for (int x = 0; x < texSize; x++)
            {
                float dx = (x - center) / (center);
                float dy = (y - center) / (center);
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                // ★ 纳米级精度模型：内芯(1%) + 中环(10%) + 外晕(20%)
                // 核心亮度减半：从 0.95f -> 0.45f
                float core = Mathf.Exp(-dist * dist * 4000f) * 0.45f; 
                float halo = Mathf.Exp(-dist * dist * 200f) * 0.45f;
                float outer = Mathf.Exp(-dist * dist * 50f) * 0.15f;

                float alpha = Mathf.Clamp01(core + halo + outer);
                
                // 强制在 20% 范围外极速消失，防止任何散光
                if (dist > 0.50f) alpha *= Mathf.Clamp01((1.0f - dist) / 0.5f);
                if (dist > 0.95f) alpha = 0f;

                // 预烘焙颜色入像素，彻底防白框
                Color finalC = baseColor;
                finalC.a = alpha * glowIntensity;
                tex.SetPixel(x, y, finalC);
            }
        }
        tex.Apply();

        generatedSprite = Sprite.Create(tex, new Rect(0, 0, texSize, texSize), new Vector2(0.5f, 0.5f), 100f);
        glowRenderer.sprite = generatedSprite;
        
        // 物理缩放修正 (锁定半径)
        float spriteWorldWidth = texSize / 100f;
        float finalScale = (glowRadius * 2.0f) / spriteWorldWidth;
        glowObj.transform.localScale = Vector3.one * finalScale;
        glowObj.transform.localRotation = Quaternion.Euler(90, 0, 0);

        // 使用最稳健的 Additive 混合 (One-One)
        Shader s = Shader.Find("Sprites/Default");
        if (s != null)
        {
            Material mat = new Material(s);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_ZWrite", 0);
            glowRenderer.material = mat;
        }
        
        // 优先级设为 5：能浮于地板之上，但会被大多数物体遮挡
        glowRenderer.sortingOrder = 5;
    }

    void LateUpdate()
    {
        if (followTarget == null)
        {
            Camera cam = Camera.main;
            if (cam == null) cam = FindObjectOfType<Camera>();
            if (cam != null) followTarget = cam.transform;
            if (followTarget == null) return;
        }

        // 排除干扰层级
        int playerLayer = LayerMask.NameToLayer("Player");
        int ignoreMask = (1 << 2) | (1 << 5); 
        if (playerLayer != -1) ignoreMask |= (1 << playerLayer);

        // 射线的起点设在胸部下方 (0.7m) 穿透海平面
        Vector3 rayStart = followTarget.position + Vector3.down * 0.7f;
        float targetY = lastValidGroundY;

        RaycastHit[] hits = Physics.RaycastAll(rayStart, Vector3.down, 40f, ~ignoreMask);
        float bestY = -999f;
        bool found = false;

        foreach (var hit in hits)
        {
            // 过滤掉头部的碰撞或过高的平面
            if (hit.point.y > followTarget.position.y - 0.35f) continue;
            if (hit.point.y > bestY)
            {
                bestY = hit.point.y;
                found = true;
            }
        }

        if (found)
        {
            targetY = bestY + groundOffset;
            lastValidGroundY = targetY;
        }
        else
        {
            targetY = followTarget.position.y - 1.7f; 
        }

        float currentY = (glowRenderer != null) ? glowRenderer.transform.position.y : lastValidGroundY;
        float smoothY = Mathf.SmoothDamp(currentY, targetY, ref currentYVelocity, 0.08f);
        
        Vector3 targetPos = new Vector3(followTarget.position.x, smoothY, followTarget.position.z);
        if (glowRenderer != null)
        {
            glowRenderer.transform.position = targetPos;
            glowRenderer.transform.rotation = Quaternion.Euler(90, 0, 0);
        }
    }

    void OnDestroy()
    {
        if (generatedSprite != null && generatedSprite.texture != null)
        {
            Destroy(generatedSprite.texture);
            Destroy(generatedSprite);
        }
    }
}
