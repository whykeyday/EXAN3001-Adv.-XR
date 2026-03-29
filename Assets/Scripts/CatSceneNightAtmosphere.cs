using UnityEngine;

public class CatSceneNightAtmosphere : MonoBehaviour
{
    [Header("Night Settings")]
    public Color nightFogColor = new Color(0.01f, 0.04f, 0.12f); // 深色调（类似海洋场景）
    public float fogDensity = 0.015f; // 调淡一些，防止把远处的树遮没
    public float nightAmbientIntensity = 0.2f;

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
