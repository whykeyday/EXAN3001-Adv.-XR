using UnityEngine;

/// <summary>
/// 自动在所挂载的中心点上方生成一盏垂直向下的聚光灯（舞台光效）。
/// 重点照亮玩家交互的区域。
/// </summary>
public class StageLightSetup : MonoBehaviour
{
    public float height = 8f;
    public Color lightColor = new Color(1f, 0.95f, 0.9f);
    public float lightIntensity = 10f; // URP standard typically higher
    public float spotAngle = 60f;

    void Start()
    {
        GameObject lightObj = new GameObject("StageLight_Spot");
        lightObj.transform.SetParent(transform);
        lightObj.transform.localPosition = new Vector3(0, height, 0);
        lightObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // 直射向下

        Light l = lightObj.AddComponent<Light>();
        l.type = LightType.Spot;
        l.color = lightColor;
        l.intensity = lightIntensity;
        l.innerSpotAngle = spotAngle * 0.6f;
        l.spotAngle = spotAngle;
        l.range = height + 10f;
    }
}
