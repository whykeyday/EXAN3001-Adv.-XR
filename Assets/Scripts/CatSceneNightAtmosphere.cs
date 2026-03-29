using UnityEngine;

public class CatSceneNightAtmosphere : MonoBehaviour
{
    [Header("Night Settings")]
    public Color nightFogColor = new Color(0f, 0.15f, 0.4f); // 极其深邃的纯黑/午夜蓝（照搬海洋环境）
    public float fogDensity = 0.04f; // 与海域一样的迷雾浓度
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
        
        // 5. 暴力关闭所有方向光，防止把物体和地平线照亮
        Light[] lights = FindObjectsOfType<Light>();
        foreach(var l in lights)
        {
            if (l.type == LightType.Directional) {
                l.enabled = false;
                l.intensity = 0f;
            }
        }

        // 6. 把摄像机的背景强制换成深海死黑蓝，杜绝 URP 默认亮蓝色
        Camera cam = Camera.main;
        if (cam == null) cam = FindObjectOfType<Camera>();
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = nightFogColor;
        }
    }
}
