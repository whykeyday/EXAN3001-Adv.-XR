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

        foreach (GameObject obj in selectedObjects)
        {
            MeshFilter mf = obj.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null)
            {
                Debug.LogWarning($"Skipped {obj.name}: No MeshFilter found.");
                continue;
            }

            // 处理容器材质（透明包围盒隐喻）
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                // 可以设为一个非常淡的透明材质，或者用户后面自己指定。暂时保持现有赋予的材质
                // 后期可以通过 Shader 给这个模型增加线框展示。
            }

            // 给模型添加粒子系统，将其约束在模型网格形状内
            ParticleSystem ps = obj.GetComponent<ParticleSystem>();
            if (ps == null) ps = obj.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.loop = true;
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

            // 一次性迸发大量粒子，填满容器
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, (short)2000, (short)2000, 1, 1f) });

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

            // 分配 URP 发光未光照材质
            ParticleSystemRenderer psr = obj.GetComponent<ParticleSystemRenderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
            
            if (shader != null)
            {
                Material mat = new Material(shader);
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
                if (mat.HasProperty("_EmissionColor")) 
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", Color.white * 2.5f); // 自带发光
                }
                
                // 设置为 Transparent 混合模式
                mat.SetFloat("_Surface", 1); 
                mat.SetFloat("_Blend", 0);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.renderQueue = 3000;

                psr.material = mat;
                psr.renderMode = ParticleSystemRenderMode.Billboard;
            }

            Debug.Log($"[ParticleContainerTool] 成功将 {obj.name} 转换为粒子容器模型！可以在 Inspector 中调整粒子形态、颜色和材质。");
        }
    }
}
