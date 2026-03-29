using UnityEngine;

public class CatSceneNightAtmosphere : MonoBehaviour
{
    [Header("Night Settings")]
    public Color nightFogColor = Color.black; // 纯碎的死黑
    public float fogDensity = 0.035f; 
    public float nightAmbientIntensity = 0f; // 彻底无环境光

    void Start()
    {
        RenderSettings.fog = true;
        RenderSettings.fogColor = nightFogColor;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = fogDensity;

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = Color.black; // 连地平线的散光都抹杀
        RenderSettings.ambientIntensity = nightAmbientIntensity;
        
        // 4. 清理天空盒，变回纯色背景
        RenderSettings.skybox = null; 
    }
}
