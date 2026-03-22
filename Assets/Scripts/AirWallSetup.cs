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
        
        if (makePlaneInvisible)
        {
            Renderer r = GetComponent<Renderer>();
            if (r != null) r.enabled = false;
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
}
