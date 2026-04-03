using UnityEngine;

/// <summary>
/// 玩家脚下渐变自发光光晕：跟随 XR 摄像机的 XZ 位置。
/// 使用粒子系统创建中心亮、边缘渐变透明的柔和光圈。
/// </summary>
public class PlayerSpotlight : MonoBehaviour
{
    [Header("Glow Settings")]
    public Color lightColor = new Color(1f, 0.98f, 0.95f, 1f);
    [Tooltip("光圈半径")]
    public float glowRadius = 1.5f;
    [Tooltip("光圈亮度（HDR 倍数）")]
    public float glowIntensity = 3f;
    [Tooltip("光圈距地面高度")]
    public float groundOffset = 0.02f;

    [Header("Spot Light (补充)")]
    [Tooltip("是否同时加一个真实 SpotLight")]
    public bool useSpotLight = true;
    public float spotHeight = 4f;
    public float spotIntensity = 8f;
    public float spotAngle = 50f;
    public float spotRange = 10f;

    [Header("Follow")]
    [Tooltip("留空则自动找 Main Camera")]
    public Transform followTarget;

    private ParticleSystem glowPS;
    private Light spotLight;

    void Start()
    {
        CreateGlowParticles();

        if (useSpotLight)
        {
            GameObject lightObj = new GameObject("PlayerFootSpotlight");
            spotLight = lightObj.AddComponent<Light>();
            spotLight.type = LightType.Spot;
            spotLight.color = lightColor;
            spotLight.intensity = spotIntensity;
            spotLight.spotAngle = spotAngle;
            spotLight.innerSpotAngle = spotAngle * 0.4f;
            spotLight.range = spotRange;
            spotLight.shadows = LightShadows.None;
            lightObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }
    }

    void CreateGlowParticles()
    {
        // 创建粒子系统作为渐变光圈
        GameObject glowObj = new GameObject("GroundGlow");
        glowObj.transform.SetParent(transform, false);

        glowPS = glowObj.AddComponent<ParticleSystem>();
        glowPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = glowPS.main;
        main.loop = true;
        main.duration = 1f;
        main.startLifetime = 999f; // 永不消失
        main.startSpeed = 0f;
        main.startSize = glowRadius * 2f;
        main.maxParticles = 1; // 只要一个粒子
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = GetGlowColor();
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.startRotation = 0f;
        main.gravityModifier = 0f;

        var emission = glowPS.emission;
        emission.rateOverTime = 1f;

        var shape = glowPS.shape;
        shape.enabled = false; // 不需要形状，就一个粒子在原点

        // 禁用所有不需要的模块
        var vel = glowPS.velocityOverLifetime;
        vel.enabled = false;
        var noise = glowPS.noise;
        noise.enabled = false;
        var col = glowPS.colorOverLifetime;
        col.enabled = false;

        // 修改渲染器
        var psr = glowPS.GetComponent<ParticleSystemRenderer>();
        psr.renderMode = ParticleSystemRenderMode.HorizontalBillboard; // 平铺在地面
        psr.material = CreateGlowMaterial();

        glowPS.Play();
    }

    Color GetGlowColor()
    {
        // HDR 亮色
        return new Color(
            lightColor.r * glowIntensity,
            lightColor.g * glowIntensity,
            lightColor.b * glowIntensity,
            0.4f
        );
    }

    Material CreateGlowMaterial()
    {
        // 创建程序化的径向渐变纹理 — 中心亮，边缘透明
        int texSize = 128;
        Texture2D tex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        float center = texSize / 2f;
        float maxRadius = texSize / 2f;

        for (int y = 0; y < texSize; y++)
        {
            for (int x = 0; x < texSize; x++)
            {
                float dx = (x - center) / maxRadius;
                float dy = (y - center) / maxRadius;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                // 2D Gaussian-like smooth falloff for a 'premium' glow effect
                // Exp(-dist^2 * constant) creates a much softer edge than linear/cubic
                float falloff = Mathf.Exp(-dist * dist * 4.5f);
                float alpha = Mathf.Clamp01(falloff);

                // Avoid hard cutoff at the edges
                if (dist > 1.0f) alpha = 0f;

                // Set pixel color with subtle transparency
                // Brightness follows falloff, but capped for softness
                float b = alpha;
                tex.SetPixel(x, y, new Color(b, b, b, alpha * 0.5f));
            }
        }
        tex.Apply();

        // 创建 Additive 混合材质
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) shader = Shader.Find("Legacy Shaders/Particles/Additive");

        Material mat;
        if (shader != null)
        {
            mat = new Material(shader);
            mat.name = "GroundGlowGradient";

            // Additive 混合
            mat.SetFloat("_Surface", 1.0f);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_ZWrite", 0);

            Color hdrColor = lightColor * glowIntensity;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", hdrColor);
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            mat.mainTexture = tex;

            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", hdrColor * 0.5f);
            }
        }
        else
        {
            mat = new Material(Shader.Find("Sprites/Default"));
            mat.mainTexture = tex;
        }

        return mat;
    }

    void LateUpdate()
    {
        if (followTarget == null)
        {
            Camera cam = Camera.main;
            if (cam == null) cam = FindObjectOfType<Camera>();
            if (cam != null) followTarget = cam.transform;
        }

        if (followTarget != null)
        {
            Vector3 xzPos = new Vector3(followTarget.position.x, 0f, followTarget.position.z);

            if (glowPS != null)
            {
                glowPS.transform.position = xzPos + Vector3.up * groundOffset;
            }

            if (spotLight != null)
            {
                spotLight.transform.position = xzPos + Vector3.up * spotHeight;
            }
        }
    }
}
