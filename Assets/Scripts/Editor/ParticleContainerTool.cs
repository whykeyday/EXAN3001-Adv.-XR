using UnityEngine;
using UnityEditor;

public class ParticleContainerTool
{
    [MenuItem("Tools/Make Particle Container")]
    public static void MakeContainer()
    {
        GameObject[] selectedObjects = Selection.gameObjects;
        if (selectedObjects.Length == 0)
        {
            Debug.LogWarning("Please select at least one GameObject with a MeshFilter.");
            return;
        }

        // 提前预加载或创建统一的透明玻璃材质 Asset，以供替换
        Material transparentMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/HoloTransparentGlass.mat");
        if (transparentMat == null)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Materials"))
                AssetDatabase.CreateFolder("Assets", "Materials");
                
            transparentMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            transparentMat.SetFloat("_Surface", 1); 
            transparentMat.SetFloat("_Blend", 0); 
            transparentMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            transparentMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            transparentMat.SetInt("_ZWrite", 0);
            transparentMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            transparentMat.renderQueue = 3000;
            transparentMat.SetColor("_BaseColor", new Color(1f, 1f, 1f, 0.05f)); // 5%的白色，极度透明
            transparentMat.SetFloat("_Smoothness", 0f);
            transparentMat.SetFloat("_Metallic", 0f);
            AssetDatabase.CreateAsset(transparentMat, "Assets/Materials/HoloTransparentGlass.mat");
        }

        System.Collections.Generic.HashSet<GameObject> processed = new System.Collections.Generic.HashSet<GameObject>();

        foreach (GameObject rootObj in selectedObjects)
        {
            MeshFilter[] mfs = rootObj.GetComponentsInChildren<MeshFilter>();
            if (mfs.Length == 0)
            {
                Debug.LogWarning($"Skipped {rootObj.name}: No MeshFilter found in object or children.");
                continue;
            }

            foreach (MeshFilter mf in mfs)
            {
                if (mf.sharedMesh == null) continue;
                
                GameObject obj = mf.gameObject;
                if (processed.Contains(obj)) continue;
                processed.Add(obj);

                // 将模型外壳材质强行替换为统一下的极度透明玻璃幽灵结构
                Renderer renderer = obj.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Undo.RecordObject(renderer, "Change Material to Glass");
                    renderer.sharedMaterial = transparentMat;
                }

                // 给模型添加粒子系统，将其约束在模型网格形状内
                ParticleSystem ps = obj.GetComponent<ParticleSystem>();
                if (ps == null) ps = obj.AddComponent<ParticleSystem>();
                Undo.RecordObject(ps, "Add Particle System");

                var main = ps.main;
                main.loop = true;
                main.prewarm = true; // 开启预热，一开始就填满
                main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 3.5f);
                main.startSpeed = 0f; // 粒子在容器表面静止，通过闪烁营造活力
                main.startSize = 0.05f;
                main.maxParticles = 8000;
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
                
                // 发光随机色
                main.startColor = new ParticleSystem.MinMaxGradient(
                    new Color(1f, 1f, 1f, 0.5f), 
                    new Color(1f, 1f, 1f, 1f)
                );

                // 持续发射，此消彼伏
                var emission = ps.emission;
                emission.rateOverTime = 800f; // 持续稳定生成
                emission.SetBursts(new ParticleSystem.Burst[0]); // 清除所有 Burst

                // 形状设为按照 Mesh 发射
                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Mesh;
                shape.meshShapeType = ParticleSystemMeshShapeType.Triangle;
                shape.mesh = mf.sharedMesh;
                shape.scale = Vector3.one;

                // 存活期间颜色渐变（闪烁效果，像星星一样）
                var colorOverLife = ps.colorOverLifetime;
                colorOverLife.enabled = true;
                Gradient grad = new Gradient();
                grad.SetKeys(
                    new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                    new GradientAlphaKey[] { 
                        new GradientAlphaKey(0f, 0f), 
                        new GradientAlphaKey(1f, 0.2f),
                        new GradientAlphaKey(0.8f, 0.8f),
                        new GradientAlphaKey(0f, 1f) 
                    }
                );
                colorOverLife.color = grad;

                // 尺寸随生命周期稍微变化
                var sizeOverLife = ps.sizeOverLifetime;
                sizeOverLife.enabled = true;
                AnimationCurve sizeCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.2f, 1), new Keyframe(0.8f, 1), new Keyframe(1, 0));
                sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

                // 分配 URP 发光未光照材质，使用柔和球形
                ParticleSystemRenderer psr = obj.GetComponent<ParticleSystemRenderer>();
                psr.material = ParticleUtils.GetGlowingSphereMaterial();
                psr.renderMode = ParticleSystemRenderMode.Billboard;

                Debug.Log($"[ParticleContainerTool] 成功将 {obj.name} 转换为粒子容器模型！可以在 Inspector 中调整粒子形态、颜色和材质。");
            } // end inner foreach
        } // end outer foreach
    }
}
