using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit; 
using UnityEngine.XR.Interaction.Toolkit.Interactables; 
using UnityEngine.SceneManagement; 

[RequireComponent(typeof(XRSimpleInteractable))] 
[RequireComponent(typeof(Rigidbody))]
public class ShardInteraction : MonoBehaviour
{
    [Header("Visual Feedback")]
    [Tooltip("Scale multiplier on hover")]
    public float hoverScale = 1.05f;
    [Tooltip("Emission intensity boost on hover")]
    public float hoverEmissionBoost = 2.0f;
    [Tooltip("Color tint on hover (additive)")]
    public Color hoverTint = new Color(0.2f, 0.2f, 0.2f, 0f);

    [Header("Portal Color Settings (碎片颜色定制)")]
    [Tooltip("为这块水晶碎片染上专属的马卡龙色！默认白色代表保持原本的冰蓝色。")]
    public Color portalColor = Color.white;
    [Tooltip("水晶基础发光强度")]
    public float baseEmissionStrength = 1.5f;

    private Vector3 originalScale;
    private Material[] materials;
    private Color[] originalEmissionColors;
    private Color[] originalBaseColors;
    private XRSimpleInteractable interactable;
    private bool isHovered = false;
    private float[] originalRimIntensities;
    private Light hoverLight;

    // Cooldown prevents accidental trigger right after returning from sub-scene
    private float interactionCooldown = 1.0f; // seconds
    private float sceneLoadTime;

    void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true; // Ensure it doesn't fall
    }

    void Start()
    {
        sceneLoadTime = Time.time; // Record when scene/object started
        originalScale = transform.localScale;
        
        // Setup Hover Light (Halo)
        GameObject lightObj = new GameObject("HaloLight");
        lightObj.transform.parent = transform;
        lightObj.transform.localPosition = Vector3.zero;
        hoverLight = lightObj.AddComponent<Light>();
        hoverLight.type = LightType.Point;
        hoverLight.color = new Color(1.0f, 0.6f, 0.2f); // Warm Gold
        hoverLight.range = 3.0f;
        hoverLight.intensity = 8.0f; // Bright
        hoverLight.enabled = false;

        Renderer r = GetComponent<Renderer>();
        if (r != null)
        {
            materials = r.materials;
            originalEmissionColors = new Color[materials.Length];
            originalBaseColors = new Color[materials.Length];
            originalRimIntensities = new float[materials.Length];
            
            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i].HasProperty("_EmissionColor"))
                    originalEmissionColors[i] = materials[i].GetColor("_EmissionColor");
                else
                    originalEmissionColors[i] = Color.black; 
                
                if (materials[i].HasProperty("_BaseColor"))
                    originalBaseColors[i] = materials[i].GetColor("_BaseColor");
                else
                    originalBaseColors[i] = Color.white;

                if (materials[i].HasProperty("_RimIntensity"))
                    originalRimIntensities[i] = materials[i].GetFloat("_RimIntensity");
                
                materials[i].EnableKeyword("_EMISSION");

                // ★ 全新功能：强制在这个阶段给水晶染上玩家自定的马卡龙色（覆盖原本的暗淡蓝冰）
                if (portalColor != Color.white)
                {
                    if (materials[i].HasProperty("_BaseColor"))
                    {
                        Color tintedBase = originalBaseColors[i] * portalColor;
                        materials[i].SetColor("_BaseColor", tintedBase);
                        originalBaseColors[i] = tintedBase; // 更新基准色，确保 Hover 基于新颜色
                    }

                    if (materials[i].HasProperty("_EmissionColor"))
                    {
                        // 提取原本的亮度或强制给予一个基础亮度，并用选定的颜色渲染发光
                        Color tintedEmission = portalColor * baseEmissionStrength;
                        materials[i].SetColor("_EmissionColor", tintedEmission);
                        originalEmissionColors[i] = tintedEmission; 
                    }
                }
            }
        }

        // Subscribe to XR events
        if (interactable != null)
        {
            interactable.hoverEntered.AddListener(OnHoverEnter);
            interactable.hoverExited.AddListener(OnHoverExit);
            interactable.selectEntered.AddListener(OnSelect);
        }
    }

    void OnDestroy()
    {
        if (interactable != null)
        {
            interactable.hoverEntered.RemoveListener(OnHoverEnter);
            interactable.hoverExited.RemoveListener(OnHoverExit);
            interactable.selectEntered.RemoveListener(OnSelect);
        }
    }

    // --- Interaction Events (Accessible by HandRayPointer) ---

    // Correct signatures for XR Interaction Toolkit 3.x
    public void OnHoverEnter(HoverEnterEventArgs args) => PublicHoverEnter();
    public void OnHoverExit(HoverExitEventArgs args) => PublicHoverExit();
    public void OnSelect(SelectEnterEventArgs args) => PublicSelect();

    public void PublicHoverEnter()
    {
        if (isHovered) return;
        isHovered = true;

        // Visual Feedback: Scale Up
        transform.localScale = originalScale * hoverScale;

        // Visual Feedback: Halo Light
        if (hoverLight != null) hoverLight.enabled = true;

        // Visual Feedback: Material
        if (materials != null)
        {
            for (int i = 0; i < materials.Length; i++)
            {
              
                if (materials[i].HasProperty("_EmissionColor"))
                {
                    Color currentEmission = originalEmissionColors[i];
                    Color boost = currentEmission * hoverEmissionBoost;
                    if (currentEmission.grayscale < 0.05f) 
                        boost = new Color(0.2f, 0.2f, 0.2f); 
                    materials[i].SetColor("_EmissionColor", boost);
                }
                

                if (materials[i].HasProperty("_BaseColor"))
                {
                    materials[i].SetColor("_BaseColor", originalBaseColors[i] + hoverTint);
                }

                
                if (materials[i].HasProperty("_RimIntensity"))
                {
                    materials[i].SetFloat("_RimIntensity", originalRimIntensities[i] * hoverEmissionBoost);
                }
            }
        }
    }

    public void PublicHoverExit()
    {
        if (!isHovered) return;
        isHovered = false;

        // Reset Scale
        transform.localScale = originalScale;

        // Reset Halo Light
        if (hoverLight != null) hoverLight.enabled = false;

        // Reset Materials
        if (materials != null)
        {
            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i].HasProperty("_EmissionColor"))
                    materials[i].SetColor("_EmissionColor", originalEmissionColors[i]);
                
                if (materials[i].HasProperty("_BaseColor"))
                    materials[i].SetColor("_BaseColor", originalBaseColors[i]);

                if (materials[i].HasProperty("_RimIntensity"))
                    materials[i].SetFloat("_RimIntensity", originalRimIntensities[i]);
            }
        }
    }

    [Header("Transition Settings")]
    public string targetSceneName; // e.g. "OceanScene", "TreeScene", "CatScene"
    public MeshParticleDissolve dissolveEffect;

    // ... (rest of class)

    public void PublicSelect()
    {
        // Ignore select events during startup cooldown (prevents accidental re-trigger after scene return)
        if (Time.time - sceneLoadTime < interactionCooldown)
        {
            Debug.Log("ShardInteraction: Ignored (cooldown active)");
            return;
        }

        Debug.Log($"Shard Selected: {gameObject.name} -> Target: {targetSceneName}");
        
        // Disable interaction to prevent double-clicks
        if (interactable != null) interactable.enabled = false; 

        if (dissolveEffect != null)
        {
            dissolveEffect.TriggerDissolve(() =>
            {
                // Use fade transition if available, otherwise direct load
                if (SceneTransitionManager.Instance != null)
                    SceneTransitionManager.Instance.FadeAndLoad(targetSceneName);
                else if (!string.IsNullOrEmpty(targetSceneName))
                    SceneManager.LoadScene(targetSceneName);
            });
        }
        else
        {
            // No dissolve effect — just fade and load
            if (SceneTransitionManager.Instance != null)
                SceneTransitionManager.Instance.FadeAndLoad(targetSceneName);
            else if (!string.IsNullOrEmpty(targetSceneName))
                SceneManager.LoadScene(targetSceneName);
        }
    }
}
