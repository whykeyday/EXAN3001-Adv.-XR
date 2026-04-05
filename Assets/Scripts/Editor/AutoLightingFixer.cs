using UnityEngine;
using UnityEditor;

public class AutoLightingFixer : EditorWindow
{
    [MenuItem("Tools/魔法一键修灯光！ (Magic Light Fix)")]
    public static void FixLighting()
    {
        // 1. 强制重置环境光为亮白色（消除所有死黑阴影）
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.8f, 0.8f, 0.8f);

        // ★ 致命真凶定位：清除 Unity 默认的那层巨丑无比的灰蓝色！
        // 彻底丢弃天空盒，全靠下面强制刷黑的摄像机底色来垫底！
        RenderSettings.skybox = null;

        // 暴力遍历所有摄像机（尤其是 VR 头显的摄像机），把默认的灰蓝底色强制涂黑！
        foreach (Camera cam in Camera.allCameras)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
        }

        // 2. 自动修正主光，确保照射到碎片正面
        Light[] allLights = FindObjectsOfType<Light>();
        bool hasMainSun = false;
        foreach (Light l in allLights)
        {
            if (l.type == LightType.Directional)
            {
                // 强制将太阳光的旋转调成斜向下并且对准摄像机/碎片
                l.transform.rotation = Quaternion.Euler(30f, -30f, 0f);
                l.intensity = 1.5f; // URP 需要更强的阳光
                l.color = new Color(1f, 0.95f, 0.9f);
                hasMainSun = true;
                Debug.Log("已修正已有太阳光的角度和亮度！");
            }
        }

        // 3. 如果没太阳，强制生成一个太阳
        if (!hasMainSun)
        {
            GameObject sun = new GameObject("Directional Light");
            Light l = sun.AddComponent<Light>();
            l.type = LightType.Directional;
            l.transform.rotation = Quaternion.Euler(30f, -30f, 0f);
            l.intensity = 1.5f;
            Debug.Log("未检测到太阳光，已自动生成了一盏新太阳！");
        }

        // 4. 将水晶碎片材质直接在编辑器中改色，摆脱原生蓝冰
        ShardInteraction[] shards = FindObjectsOfType<ShardInteraction>();
        foreach (var shard in shards)
        {
            Renderer r = shard.GetComponent<Renderer>();
            if (r != null)
            {
                foreach (Material mat in r.sharedMaterials)
                {
                    if (mat.HasProperty("_BaseColor"))
                        mat.SetColor("_BaseColor", shard.portalColor);
                    
                    if (mat.HasProperty("_EmissionColor"))
                        mat.SetColor("_EmissionColor", shard.portalColor * shard.baseEmissionStrength);
                }
            }
        }

        Debug.Log("✨ 一键魔法修光完毕！请直接看 Scene 视图，不再是暗蓝黑了！");
    }
}
