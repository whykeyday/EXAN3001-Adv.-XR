using UnityEngine;

/// <summary>
/// 玩家脚下聚光灯 + 发光圆盘：跟随 XR 摄像机的 XZ 位置。
/// 即使地板透明，也能看到脚下的光圈。
/// </summary>
public class PlayerSpotlight : MonoBehaviour
{
    [Header("Light Settings")]
    public float height = 4f;
    public Color lightColor = new Color(1f, 0.98f, 0.95f, 1f);
    public float intensity = 15f;
    public float spotAngle = 50f;
    public float range = 10f;

    [Header("Ground Glow Disc")]
    [Tooltip("地面光圈大小")]
    public float discRadius = 1.2f;
    [Tooltip("光圈颜色")]
    public Color discColor = new Color(1f, 0.95f, 0.85f, 0.15f);
    [Tooltip("光圈距地面高度")]
    public float discGroundOffset = 0.05f;

    [Header("Follow")]
    [Tooltip("留空则自动找 Main Camera")]
    public Transform followTarget;

    private Light spotLight;
    private GameObject glowDisc;

    void Start()
    {
        // 创建聚光灯
        GameObject lightObj = new GameObject("PlayerFootSpotlight");
        spotLight = lightObj.AddComponent<Light>();
        spotLight.type = LightType.Spot;
        spotLight.color = lightColor;
        spotLight.intensity = intensity;
        spotLight.spotAngle = spotAngle;
        spotLight.innerSpotAngle = spotAngle * 0.4f;
        spotLight.range = range;
        spotLight.shadows = LightShadows.None;
        lightObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        // 创建地面发光圆盘（即使地板透明也能看到）
        glowDisc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        glowDisc.name = "GroundGlowDisc";
        Destroy(glowDisc.GetComponent<Collider>()); // 不要碰撞

        glowDisc.transform.localScale = new Vector3(discRadius * 2f, 0.01f, discRadius * 2f);

        // 发光材质
        Material discMat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit") 
            ?? Shader.Find("Particles/Standard Unlit")
            ?? Shader.Find("Sprites/Default"));
        
        if (discMat != null)
        {
            discMat.SetFloat("_Surface", 1.0f);
            discMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            discMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            discMat.SetInt("_ZWrite", 0);
            
            if (discMat.HasProperty("_BaseColor")) discMat.SetColor("_BaseColor", discColor);
            else discMat.color = discColor;
            
            if (discMat.HasProperty("_EmissionColor"))
            {
                discMat.EnableKeyword("_EMISSION");
                discMat.SetColor("_EmissionColor", lightColor * 0.5f);
            }
        }
        glowDisc.GetComponent<Renderer>().material = discMat;
    }

    void LateUpdate()
    {
        if (followTarget == null)
        {
            Camera cam = Camera.main;
            if (cam == null) cam = FindObjectOfType<Camera>();
            if (cam != null) followTarget = cam.transform;
        }

        if (followTarget != null)
        {
            Vector3 xzPos = new Vector3(followTarget.position.x, 0f, followTarget.position.z);

            if (spotLight != null)
            {
                spotLight.transform.position = xzPos + Vector3.up * height;
            }

            if (glowDisc != null)
            {
                glowDisc.transform.position = xzPos + Vector3.up * discGroundOffset;
            }
        }
    }
}
