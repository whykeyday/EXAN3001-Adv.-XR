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
            transparentMat.SetColor("_BaseColor", new Color(1f, 1f, 1f, 0.05f)); 
            AssetDatabase.CreateAsset(transparentMat, "Assets/Materials/HoloTransparentUnlit.mat");
        }

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

                // 统一替换所有副材质
                Undo.RecordObject(renderer, "Change Material to Glass");
                Material[] newMats = new Material[renderer.sharedMaterials.Length];
                for (int i = 0; i < newMats.Length; i++) newMats[i] = transparentMat;
                renderer.sharedMaterials = newMats;

                // 智能计算物体的表面积来决定粒子密度
                Bounds bounds = renderer.bounds;
                float surfaceArea = 2f * (bounds.size.x * bounds.size.y + bounds.size.x * bounds.size.z + bounds.size.y * bounds.size.z);
                if (surfaceArea < 0.01f) surfaceArea = 0.01f;

                // 智能缩放：抵消 Transform 的极大缩放比例（例如FBX放大100倍），保证粒子恒定在 0.04 - 0.06 米大小
                float scaleX = obj.transform.lossyScale.x;
                if (scaleX == 0f) scaleX = 1f;
                float desiredMin = 0.04f / scaleX;
                float desiredMax = 0.06f / scaleX;

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
                main.scalingMode = ParticleSystemScalingMode.Hierarchy; 
                
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
