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
            Debug.LogWarning("Please select at least one GameObject with a MeshFilter or SkinnedMeshRenderer.");
            return;
        }

        // 提前预加载或创建统一的透明玻璃材质
        Material transparentMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/HoloTransparentUnlit.mat");
        if (transparentMat == null)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Materials"))
                AssetDatabase.CreateFolder("Assets", "Materials");
                
            transparentMat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Transparent"));
            transparentMat.SetFloat("_Surface", 1); 
            transparentMat.SetFloat("_Blend", 0); 
            transparentMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            transparentMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            transparentMat.SetInt("_ZWrite", 0);
            transparentMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            transparentMat.EnableKeyword("_ALPHABLEND_ON");
            transparentMat.renderQueue = 3000;
            AssetDatabase.CreateAsset(transparentMat, "Assets/Materials/HoloTransparentUnlit.mat");
        }
        
        // 强制把已经存在于硬盘里的那个旧 5% 材质的 Alpha 属性刷成完全透明！
        transparentMat.SetColor("_BaseColor", new Color(0f, 0f, 0f, 0f));

        System.Collections.Generic.HashSet<GameObject> processed = new System.Collections.Generic.HashSet<GameObject>();

        foreach (GameObject rootObj in selectedObjects)
        {
            Renderer[] renderers = rootObj.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) continue;

            foreach (Renderer renderer in renderers)
            {
                Mesh targetMesh = null;
                SkinnedMeshRenderer targetSmr = null;

                if (renderer is MeshRenderer)
                {
                    MeshFilter mf = renderer.GetComponent<MeshFilter>();
                    if (mf == null || mf.sharedMesh == null) continue;
                    targetMesh = mf.sharedMesh;
                }
                else if (renderer is SkinnedMeshRenderer)
                {
                    targetSmr = (SkinnedMeshRenderer)renderer;
                    if (targetSmr.sharedMesh == null) continue;
                }
                else
                {
                    continue; 
                }

                GameObject obj = renderer.gameObject;
                if (processed.Contains(obj)) continue;
                processed.Add(obj);

                // 统一替换所有副材质，并且强制让 Unity 的相机忽略渲染它的肉身
                Undo.RecordObject(renderer, "Change Material to Glass");
                Material[] newMats = new Material[renderer.sharedMaterials.Length];
                for (int i = 0; i < newMats.Length; i++) newMats[i] = transparentMat;
                renderer.sharedMaterials = newMats;
                
                // 终极绝杀：直接让 Unity 引擎在画图时彻底跳过这个包围盒（还能节省极大地性能损耗），只渲染它生成的粒子！
                renderer.forceRenderingOff = true;

                // 智能计算物体的表面积来决定粒子密度
                Bounds bounds = renderer.bounds;
                float surfaceArea = 2f * (bounds.size.x * bounds.size.y + bounds.size.x * bounds.size.z + bounds.size.y * bounds.size.z);
                // --- 终极防御：判断网格是否允许粒子读取数据，如果不允许，强行修改导入设置并开启 ---
                Mesh checkMesh = targetMesh != null ? targetMesh : (targetSmr != null ? targetSmr.sharedMesh : null);
                if (checkMesh != null && !checkMesh.isReadable)
                {
                    string assetPath = AssetDatabase.GetAssetPath(checkMesh);
                    if (!string.IsNullOrEmpty(assetPath))
                    {
                        ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
                        if (importer != null)
                        {
                            importer.isReadable = true;
                            importer.SaveAndReimport();
                            Debug.Log($"[ParticleTool] 自动为 {obj.name} 开启了模型 Read/Write 权限（粒子引擎必需）。");
                        }
                    }
                }

                // 物理世界中，我们希望每个粒子恒定在 0.04~0.06 米左右。
                float desiredMin = 0.04f;
                float desiredMax = 0.06f;

                // 智能发射率（树大则粒子多，猫小则粒子少）
                float calculatedRate = Mathf.Clamp(surfaceArea * 800f, 100f, 10000f);
                int calculatedMax = Mathf.CeilToInt(calculatedRate * 3.5f);

                ParticleSystem ps = obj.GetComponent<ParticleSystem>();
                if (ps == null) ps = obj.AddComponent<ParticleSystem>();
                Undo.RecordObject(ps, "Add Particle System");

                var main = ps.main;
                main.loop = true;
                main.prewarm = true; 
                main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 3.5f);
                main.startSpeed = 0f; 
                main.startSize = new ParticleSystem.MinMaxCurve(desiredMin, desiredMax); // 彻底解决巨型粒子
                main.maxParticles = calculatedMax; 
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
                main.scalingMode = ParticleSystemScalingMode.Shape; // 完美世界级常量比例：不管父级多大，粒子永远是设定的固定厘米级大小
                
                main.startColor = new ParticleSystem.MinMaxGradient(
                    new Color(1f, 1f, 1f, 0.5f), 
                    new Color(1f, 1f, 1f, 1f)
                );

                var emission = ps.emission;
                emission.rateOverTime = calculatedRate;
                emission.SetBursts(new ParticleSystem.Burst[0]); 

                // 约束在网格表面发射（修复聚拢问题）
                var shape = ps.shape;
                if (targetSmr != null)
                {
                    shape.shapeType = ParticleSystemShapeType.SkinnedMeshRenderer;
                    shape.skinnedMeshRenderer = targetSmr;
                }
                else
                {
                    shape.shapeType = ParticleSystemShapeType.MeshRenderer;
                    shape.meshRenderer = (MeshRenderer)renderer;
                }
                shape.meshShapeType = ParticleSystemMeshShapeType.Triangle;
                shape.useMeshColors = false; // 严防有些FBX自带了全黑或透明顶点颜色导致粒子幽灵化隐形

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

                var sizeOverLife = ps.sizeOverLifetime;
                sizeOverLife.enabled = true;
                AnimationCurve sizeCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.2f, 1), new Keyframe(0.8f, 1), new Keyframe(1, 0));
                sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

                ParticleSystemRenderer psr = obj.GetComponent<ParticleSystemRenderer>();
                psr.material = ParticleUtils.GetGlowingSphereMaterial();
                psr.renderMode = ParticleSystemRenderMode.Billboard;

            } // end inner foreach
        } // end outer foreach
    }
}
