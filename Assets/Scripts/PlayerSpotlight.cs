using UnityEngine;

/// <summary>
/// 玩家脚下渐变自发光光晕：跟随 XR 摄像机的 XZ 位置。
/// 使用 SpriteRenderer 保证 100% 圆形，消除方格。
/// 已修复：高度检测逻辑，确保光圈不会浮在空中。
/// </summary>
public class PlayerSpotlight : MonoBehaviour
{
    [Header("Glow Settings")]
    public Color lightColor = new Color(1.0f, 0.75f, 0.3f, 0.45f); // 琥珀色
    public float glowRadius = 1.0f;
    public float glowIntensity = 1.5f; 
    public float groundOffset = 0.012f;

    [Header("Follow")]
    public Transform followTarget;

    private SpriteRenderer glowRenderer;
    private float currentYVelocity; 
    private float lastValidGroundY = 0f;
    private Sprite generatedSprite;

    void Start()
    {
        CreateGlowSprite();
        // 初始高度强制为 0
        lastValidGroundY = 0f;
    }

    void CreateGlowSprite()
    {
        GameObject glowObj = new GameObject("GroundGlow_Sprite");
        glowObj.transform.SetParent(transform, false);
        glowRenderer = glowObj.AddComponent<SpriteRenderer>();
        
        int texSize = 256; 
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

                float falloff = Mathf.Exp(-dist * dist * 10f); 
                float alpha = Mathf.Clamp01(falloff);
                if (dist > 0.85f) alpha *= Mathf.Clamp01((1.0f - dist) / 0.15f);
                if (dist > 0.99f) alpha = 0f;

                tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
            }
        }
        tex.Apply();

        generatedSprite = Sprite.Create(tex, new Rect(0, 0, texSize, texSize), new Vector2(0.5f, 0.5f), 100f);
        glowRenderer.sprite = generatedSprite;
        glowObj.transform.localScale = Vector3.one * (glowRadius * 2.0f);
        glowObj.transform.localRotation = Quaternion.Euler(90, 0, 0);

        Shader s = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (s == null) s = Shader.Find("Sprites/Default");
        glowRenderer.material = new Material(s);
        glowRenderer.color = lightColor * glowIntensity;
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

        // ★ 核心改进：计算排除层级（排除玩家、手部、IgnoreRaycast等）
        int playerLayer = LayerMask.NameToLayer("Player");
        int handLayer = LayerMask.NameToLayer("GrabInteractor");
        int uiLayer = LayerMask.NameToLayer("UI");
        
        // 构造一个包含这些“干扰项”的 Mask
        int ignoreMask = (1 << 2); // 默认忽略 Ignore Raycast 层
        if (playerLayer != -1) ignoreMask |= (1 << playerLayer);
        if (handLayer != -1) ignoreMask |= (1 << handLayer);
        if (uiLayer != -1) ignoreMask |= (1 << uiLayer);

        // 使用 ~ 取反：检测除 ignoreMask 以外的所有物体 (包括 Default, Ground 等)
        Vector3 rayStart = followTarget.position + Vector3.up * 1.0f;
        float targetY = lastValidGroundY;

        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 15f, ~ignoreMask))
        {
            targetY = hit.point.y + groundOffset;
            lastValidGroundY = targetY;
        }
        else
        {
            // 如果连这都扫不到地板，尝试寻找场景最低点作为兜底，绝不回到眼睛高度
            targetY = lastValidGroundY; 
        }

        // 维持脚底高度
        float currentY = (glowRenderer != null) ? glowRenderer.transform.position.y : lastValidGroundY;
        float smoothY = Mathf.SmoothDamp(currentY, targetY, ref currentYVelocity, 0.05f);
        
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
