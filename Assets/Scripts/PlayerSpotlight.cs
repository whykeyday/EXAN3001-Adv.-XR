using UnityEngine;

/// <summary>
/// 玩家脚下渐变自发光光晕：跟随 XR 摄像机的 XZ 位置。
/// 使用 Additive 模式 (黑色 = 透明)，彻底解决白框问题。
/// 已优化：海洋探测（从胸部向下），尺寸和亮度回正。
/// </summary>
public class PlayerSpotlight : MonoBehaviour
{
    [Header("Glow Aesthetics")]
    public Color lightColor = new Color(1.0f, 0.7f, 0.25f, 1.0f); 
    public float glowRadius = 0.42f;      
    public float glowIntensity = 0.4f; 
    public float groundOffset = 0.015f;

    [Header("Follow Settings")]
    public Transform followTarget;

    private SpriteRenderer glowRenderer;
    private float currentYVelocity; 
    private float lastValidGroundY = 0f;
    private Sprite generatedSprite;

    void Start()
    {
        CreateGlowSprite();
        lastValidGroundY = transform.position.y - 1.6f;
    }

    void CreateGlowSprite()
    {
        GameObject glowObj = new GameObject("GroundGlow_ZeroFailure");
        glowObj.transform.SetParent(transform, false);
        glowRenderer = glowObj.AddComponent<SpriteRenderer>();
        
        int texSize = 128; 
        Texture2D tex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;

        float center = texSize / 2f;
        for (int y = 0; y < texSize; y++)
        {
            for (int x = 0; x < texSize; x++)
            {
                float dx = (x - center) / (center);
                float dy = (y - center) / (center);
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                // ★ 叠加混合逻辑：背景必须是纯黑色 (0,0,0,0)
                float falloff = Mathf.Exp(-dist * dist * 10f); 
                float brightness = Mathf.Clamp01(falloff);
                if (dist > 0.85) brightness *= Mathf.Clamp01((1.0f - dist) / 0.15f);
                if (dist > 0.99f) brightness = 0f;

                // 使用琥珀色预乘，Additive 模式下，黑色即透明
                Color finalC = lightColor * brightness * glowIntensity;
                finalC.a = 1.0f; // Additive 混合下 Alpha 通常设为 1
                tex.SetPixel(x, y, finalC);
            }
        }
        tex.Apply();

        generatedSprite = Sprite.Create(tex, new Rect(0, 0, texSize, texSize), new Vector2(0.5f, 0.5f), 100f);
        glowRenderer.sprite = generatedSprite;
        glowObj.transform.localScale = Vector3.one * (glowRadius * 2.5f);
        glowObj.transform.localRotation = Quaternion.Euler(90, 0, 0);

        // ★ 给 URP 用的确定性 Additive 混合方法
        Shader s = Shader.Find("Universal Render Pipeline/Unlit");
        if (s == null) s = Shader.Find("Sprites/Default");
        
        Material mat = new Material(s);
        // 关键：强制改为 Additive 混合 (One / One)
        // 这种模式下，Shader 不看 Alpha 通道，只看颜色。黑色 = 100% 透明，完全解决白方块。
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        mat.SetInt("_ZWrite", 0);
        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1); // Transparent
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white); // 贴图已自带颜色

        glowRenderer.material = mat;
        glowRenderer.color = Color.white; 
        glowRenderer.sortingOrder = 1000; // 海洋最顶层
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

        // ★ 核心改进：射线的起点设在胸部下方 (Camera 往下 0.6m)
        // 这会跳过你头顶的海平面 Plane，直接刺向脚底的沙滩
        Vector3 rayStart = followTarget.position + Vector3.down * 0.6f;
        float targetY = lastValidGroundY;

        // 排除干扰层级
        int playerLayer = LayerMask.NameToLayer("Player");
        int ignoreMask = (1 << 2) | (1 << 5); // Ignore Raycast & UI
        if (playerLayer != -1) ignoreMask |= (1 << playerLayer);

        // 使用普通的 Raycast，但在胸部以下探测
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 30f, ~ignoreMask))
        {
            targetY = hit.point.y + groundOffset;
            lastValidGroundY = targetY;
        }
        else
        {
            // 海底兜底高度：相机下方 1.7 米
            targetY = followTarget.position.y - 1.7f; 
        }

        float currentY = (glowRenderer != null) ? glowRenderer.transform.position.y : lastValidGroundY;
        // 顺滑移动
        float smoothY = Mathf.SmoothDamp(currentY, targetY, ref currentYVelocity, 0.08f);
        
        Vector3 targetPos = new Vector3(followTarget.position.x, smoothY, followTarget.position.z);
        if (glowRenderer != null)
        {
            glowRenderer.transform.position = targetPos;
            glowRenderer.transform.rotation = Quaternion.identity; 
            glowRenderer.transform.Rotate(90, 0, 0); // 确保平铺在地板上
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
