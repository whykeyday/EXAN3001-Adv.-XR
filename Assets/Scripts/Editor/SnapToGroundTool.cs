using UnityEngine;
using UnityEditor;

public class SnapToGroundTool : EditorWindow
{
    private float targetY = 0f; // 默认把底部对齐到 Y=0，也是大多场景的地面基准

    [MenuItem("Tools/自动对齐底部高度到地板")]
    public static void ShowWindow()
    {
        GetWindow<SnapToGroundTool>("对齐物体高度");
    }

    void OnGUI()
    {
        GUILayout.Label("强行把物体的最下边(脚底)按到一个基准线上！", EditorStyles.boldLabel);
        GUILayout.Space(10);
        targetY = EditorGUILayout.FloatField("目标最低点(Y轴高度):", targetY);
        GUILayout.Space(10);

        if (GUILayout.Button("一键对齐选中的物体（Snap）"))
        {
            SnapSelected();
        }
    }

    private void SnapSelected()
    {
        GameObject[] selectedObjects = Selection.gameObjects;
        if (selectedObjects.Length == 0)
        {
            Debug.LogWarning("[对齐工具] 请先在左边列表中选中你要调整的模型！");
            return;
        }

        int count = 0;
        foreach (GameObject obj in selectedObjects)
        {
            Undo.RecordObject(obj.transform, "Snap Object To Ground");

            // 获取物体及其子物体身上所有的渲染器框体
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) continue;

            // 把所有网格的边界包络到一起，求出真正的"物体底边"
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            // 计算它现在的脚底离目标高度差了多远
            float currentBottomY = bounds.min.y;
            float offsetY = targetY - currentBottomY;
            
            // 补偿差距，把它精准地拽到目标线上
            obj.transform.position += new Vector3(0, offsetY, 0);
            count++;
        }

        Debug.Log($"[对齐工具] 大成功！调整了 {count} 个物体，它们的真正底部（接触点）现在全部拉齐到 Y={targetY} 啦！");
    }
}
