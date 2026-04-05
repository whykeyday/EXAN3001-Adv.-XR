using UnityEngine;
using UnityEditor;

public class AutoLightingFixer : MonoBehaviour
{
    [MenuItem("Tools/魔法一键修灯光！ (Magic Light Fix)")]
    public static void FixLighting()
    {
        // === 1. 修复全局环境光 (消除背光死黑) ===
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.8f, 0.8f, 0.8f);

        // === 2. 恢复隐形天空盒反射 (解决删地板后水晶全黑的问题) ===
        Material defaultSky = Resources.GetBuiltinResource<Material>("Default-Skybox.mat");
        if (defaultSky != null) {
            RenderSettings.skybox = defaultSky; 
        }

        // === 3. 抹杀所有镜头的背景色 (干掉灰蓝滤镜！！！) ===
        foreach (Camera cam in Camera.allCameras)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
        }

        Debug.Log("🎉 [光驱成功] 烦人的灰蓝背景已被全境封黑！水晶获得了隐藏的物理反光！");
    }
}
