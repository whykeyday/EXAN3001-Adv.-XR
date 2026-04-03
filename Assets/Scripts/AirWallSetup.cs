using UnityEngine;

/// <summary>
/// 自动读取所挂载 Plane 的尺寸，并在其四周建立隐形的空气墙（BoxCollider）。
/// </summary>
[RequireComponent(typeof(MeshFilter))]
public class AirWallSetup : MonoBehaviour
{
    public float wallHeight = 10f;
    public float wallThickness = 1f;
    public bool makePlaneInvisible = true;

    void Start()
    {
        CreateWalls();
        CleanupScene();
        MakeFloorsTransparent();
        
        if (makePlaneInvisible)
        {
            // 不再完全隐藏 Renderer，而是替换成“暗色透明”材质，解决地板太白的问题
            Renderer r = GetComponent<Renderer>();
            
            // 如果没找到，尝试在子物体找一下相关的平面
            if (r == null || !gameObject.name.ToLower().Contains("plane"))
            {
                foreach (var childR in GetComponentsInChildren<MeshRenderer>())
                {
                    if (childR.gameObject.name.ToLower().Contains("plane") || childR.gameObject.name.ToLower().Contains("floor"))
                    {
                        r = childR;
                        break;
                    }
                }
            }

            if (r != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (mat == null) mat = new Material(Shader.Find("Standard"));

                // 设为极深蓝色/黑色的半透明，既能衬托聚光灯，又不晃眼
                Color darkColor = new Color(0.04f, 0.04f, 0.06f, 0.4f);
                mat.SetFloat("_Surface", 1f); 
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.renderQueue = 3000;
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.SetColor("_BaseColor", darkColor);
                mat.color = darkColor;

                r.material = mat;
                r.enabled = true; // 确保它是开启的！
            }
        }
    }

    void CreateWalls()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) return;

        Bounds bounds = mf.sharedMesh.bounds;
        Vector3 extents = Vector3.Scale(bounds.extents, transform.lossyScale);
        Vector3 center = transform.TransformPoint(bounds.center);

        // 创建东、南、西、北 4 面墙
        CreateWall("AirWall_North", center + Vector3.forward * extents.z, new Vector3(extents.x * 2, wallHeight, wallThickness));
        CreateWall("AirWall_South", center - Vector3.forward * extents.z, new Vector3(extents.x * 2, wallHeight, wallThickness));
        CreateWall("AirWall_East",  center + Vector3.right * extents.x, new Vector3(wallThickness, wallHeight, extents.z * 2));
        CreateWall("AirWall_West",  center - Vector3.right * extents.x, new Vector3(wallThickness, wallHeight, extents.z * 2));
    }

    void CreateWall(string name, Vector3 pos, Vector3 size)
    {
        GameObject wall = new GameObject(name);
        wall.transform.SetParent(transform);
        // 上移以盖住地平面上的空间
        wall.transform.position = pos + Vector3.up * (wallHeight / 2f); 
        
        BoxCollider bc = wall.AddComponent<BoxCollider>();
        bc.size = size;
    }

    void CleanupScene()
    {
        // 自动清理的基本原则：绝对不碰玩家、高度依赖的 XR 组件
        foreach (var obj in FindObjectsOfType<GameObject>())
        {
            string n = obj.name.ToLower();
            if (n.Contains("template") || n.Contains("module") || n.Contains("prototype"))
            {
                // 安全白名单：包含这些词的一律不删
                if (n.Contains("xr") || n.Contains("player") || n.Contains("origin") || n.Contains("hand") || n.Contains("interact"))
                    continue;

                // 只处理 15 米外的远景物品
                if (obj.transform.position.magnitude > 15f)
                {
                    // 确认没有物理碰撞器（防止踩空）才隐藏
                    if (obj.GetComponent<Collider>() == null)
                        obj.SetActive(false);
                }
            }
        }
    }

    void MakeFloorsTransparent()
    {
        // 将所有地板变透明淡入 (深色半透)
        foreach (var mr in FindObjectsOfType<MeshRenderer>())
        {
            if (mr.gameObject.name.ToLower().Contains("plane") || mr.gameObject.name.ToLower().Contains("floor"))
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (mat == null) mat = new Material(Shader.Find("Standard"));
                
                Color floorColor = new Color(0.01f, 0.02f, 0.05f, 0.35f); 
                mat.SetFloat("_Surface", 1.0f);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.renderQueue = 3000;
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.SetColor("_BaseColor", floorColor);
                mat.color = floorColor;
                mr.material = mat;
            }
        }
    }
}
