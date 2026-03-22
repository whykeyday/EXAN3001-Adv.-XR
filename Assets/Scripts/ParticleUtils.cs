using UnityEngine;

public static class ParticleUtils
{
    private static Texture2D _cachedCircleTex;

    /// <summary>
    /// 生成一个柔和、发光的球状粒子材质。
    /// 使用程序化生成的软圆贴图，确保在所有平台（包括 Quest 3）上都不会显示为方块。
    /// </summary>
    public static Material GetGlowingSphereMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
        
        Material mat = new Material(shader);
        mat.name = "GlowingSphereParticle";
        
        mat.SetFloat("_Surface", 1.0f); // 1 = Transparent
        mat.SetFloat("_Blend", 0.0f);   // 0 = Alpha
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha); // 恢复 Alpha Blend，保留真实的暗褐色
        mat.SetInt("_ZWrite", 0);
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.EnableKeyword("SOFTPARTICLES_ON");

        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
        if (mat.HasProperty("_EmissionColor")) 
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", Color.white * 3.0f); // HDR 发光
        }

        // Load built-in default particle texture for perfect soft spheres on Quest
        // Unity sometimes throws annoying Console Errors if it can't find this exact PSD in URP/XR
        Texture2D defaultTex = GetSoftCircleTexture();

        mat.mainTexture = defaultTex;
        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", defaultTex);
        
        return mat;
    }

    /// <summary>
    /// 程序化生成一个 64x64 的柔和圆形纹理。
    /// 中心亮，边缘渐变为透明，替代 Default-Particle.psd（Quest 上可能加载不到）。
    /// </summary>
    public static Texture2D GetSoftCircleTexture()
    {
        if (_cachedCircleTex != null) return _cachedCircleTex;

        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.name = "ProceduralSoftCircle";
        float center = size / 2f;
        float maxRadius = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center + 0.5f;
                float dy = y - center + 0.5f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float normalizedDist = dist / maxRadius;

                // 柔和衰减：中心为1，边缘到0
                float alpha = Mathf.Clamp01(1.0f - normalizedDist);
                alpha = alpha * alpha; // 平方衰减，让边缘更柔和

                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();
        _cachedCircleTex = tex;
        return tex;
    }
}
