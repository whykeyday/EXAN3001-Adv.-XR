using UnityEngine;

public class CatSceneNightAtmosphere : MonoBehaviour
{
    [Header("Night Settings")]
    public Color nightFogColor = new Color(0.002f, 0.005f, 0.012f); // 极其深邃的纯黑/午夜蓝
    public float fogDensity = 0.02f; // 浓度适中
    public float nightAmbientIntensity = 0.05f; // 基本关掉所有的环境反射光

    void Start()
    {
        // 1. 自动寻找并关闭场景里的太阳光 (Directional Light)
        Light[] lights = FindObjectsOfType<Light>();
        foreach (Light l in lights)
        {
            if (l.type == LightType.Directional)
            {
                l.intensity = 0f; // 彻底关掉太阳，让粒子和火焰成为主角
            }
        }

        // 2. 开启迷雾 (模仿海洋场景)
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = nightFogColor;
        RenderSettings.fogDensity = fogDensity;

        // 3. 调暗环境光
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = nightFogColor * nightAmbientIntensity;
        
        // 4. 清理天空盒，变回纯色背景
        RenderSettings.skybox = null; 
    }
}
