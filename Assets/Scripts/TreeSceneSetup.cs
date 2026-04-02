using UnityEngine;

/// <summary>
/// TreeSceneSetup — Builds the procedural glowing tree in TreeScene at runtime.
///
/// HOW TO SET UP in Unity Editor (TreeScene):
///   1. Create an empty GameObject, name it "TreeManager"
///   2. Add Component → TreeSceneSetup
///   3. Optionally add an AudioSource with forest ambient audio and drag into "Ambient Audio"
/// </summary>
public class TreeSceneSetup : MonoBehaviour
{
    [Header("Position")]
    [Tooltip("World-space centre of the tree.")]
    public Vector3 treeCenter = new Vector3(0f, 1.0f, 1.5f);

    [Header("Scale")]
    [Tooltip("Overall size multiplier for the whole tree.")]
    public float treeScale = 3.5f;

    [Header("Audio")]
    [Tooltip("Looping forest / wind ambient AudioSource.")]
    public AudioSource ambientAudio;

    [Header("Float/Sway Animation")]
    public float swayAmplitude = 0.015f;
    public float swaySpeed     = 0.6f;

    private GameObject treeRoot;

    [Header("Atmosphere — 深棕森林色调")]
    public Color forestFogColor = new Color(0.06f, 0.04f, 0.02f);
    public Color forestSkyColor = new Color(0.03f, 0.02f, 0.01f);

    void Start()
    {
        SetupAtmosphere();
        BuildTree();
        if (ambientAudio != null && !ambientAudio.isPlaying) ambientAudio.Play();
    }

    void SetupAtmosphere()
    {
        // 深棕色森林氛围（类似海洋的深蓝，但是棕色调）
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = forestFogColor;
        RenderSettings.fogDensity = 0.025f;

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.05f, 0.03f, 0.02f); // 极微弱棕色环境光
        RenderSettings.ambientIntensity = 0.2f;
        RenderSettings.skybox = null;

        // 方向光调暖色微弱
        Light[] lights = FindObjectsOfType<Light>();
        foreach (var l in lights)
        {
            if (l.type == LightType.Directional)
            {
                l.intensity = 0.08f;
                l.color = new Color(0.8f, 0.6f, 0.3f); // 暖黄色月光
            }
        }

        Camera cam = Camera.main;
        if (cam == null) cam = FindObjectOfType<Camera>();
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = forestSkyColor;
        }
    }

    private TreeHealer cachedHealer;

    void Update()
    {
        if (cachedHealer == null) cachedHealer = FindObjectOfType<TreeHealer>();

        // Gentle whole-tree sway & Growth mechanics
        if (treeRoot != null)
        {
            float energy = cachedHealer != null ? cachedHealer.energyLevel : 0f;
            
            // Branch lateral extension: X & Z expand dramatically from 40% to 100%
            Vector3 targetScale = new Vector3(
                treeScale * Mathf.Lerp(0.4f, 1.0f, energy),
                treeScale * Mathf.Lerp(0.6f, 1.0f, energy), // Y grows from 60% to 100%
                treeScale * Mathf.Lerp(0.4f, 1.0f, energy)
            );

            float s = 1f + Mathf.Sin(Time.time * swaySpeed) * swayAmplitude;
            treeRoot.transform.localScale = targetScale * s;
            
            // Rise up from the ground slightly as it heals (starts -0.8m underground, goes to 0)
            treeRoot.transform.position = treeCenter + Vector3.up * Mathf.Lerp(-0.8f, 0f, energy);
        }
    }

    // ── Geometry (matches GlassShardsSceneSetup.CreateTreeWorld exactly) ─────────────────
    void BuildTree()
    {
        treeRoot = new GameObject("TreeRoot");
        treeRoot.transform.position = treeCenter;
        treeRoot.transform.localScale = Vector3.one * treeScale;

        // Fixed seed for consistent branching (same as GlassShardsSceneSetup)
        Random.InitState(12345);

        var container = new GameObject("TreeContainer");
        container.transform.SetParent(treeRoot.transform, false);
        container.transform.localRotation = Quaternion.Euler(0, 30, 0);

        Color darkGreen = new Color(0.25f, 0.35f, 0.05f, 1f); // Dark yellow-green (withered)
        float glow    = 1.0f; // Start with lower glow for dead tree

        // Trunk (5 segments, short)
        for (int i = 0; i < 5; i++)
        {
            Vector3 pos = new Vector3(
                Random.Range(-0.02f, 0.02f),
                -0.35f + i * 0.07f,
                Random.Range(-0.02f, 0.02f));
            float scale = 0.12f * (1f - i / 7f);
            Sphere(container.transform, pos, Vector3.one * scale, darkGreen, glow, treeRoot);
        }

        // Branches (12, spherical spread, X-biased)
        for (int i = 0; i < 12; i++)
        {
            Vector3 dir = Random.onUnitSphere;
            if (dir.y < 0) dir.y *= -1;
            dir.y += 0.5f;
            dir.x *= 1.5f;
            dir.Normalize();

            Vector3 start = new Vector3(0, Random.Range(-0.1f, 0.1f), 0);
            Vector3 end   = start + dir * Random.Range(0.25f, 0.45f);
            Branch(container.transform, start, end, darkGreen, 0.04f, glow, treeRoot);
        }
    }

    // ── Branch helper (matches GlassShardsSceneSetup.CreateBranch) ───────────────────────
    void Branch(Transform parent, Vector3 start, Vector3 end, Color col, float baseScale, float emission, GameObject rootMarker)
    {
        Vector3 dir = (end - start).normalized;
        float   len = Vector3.Distance(start, end);
        int     segs = Mathf.CeilToInt(len / 0.06f);
        for (int i = 0; i < segs; i++)
        {
            Vector3 pos   = start + dir * (i * len / segs);
            float   scale = baseScale * (1f - i * 0.8f / segs);
            Sphere(parent, pos, Vector3.one * scale, col, emission, rootMarker);
        }
    }

    // ── Sphere primitive helper ───────────────────────────────────────────────────────────
    void Sphere(Transform parent, Vector3 localPos, Vector3 scale, Color col, float emission, GameObject rootMarker)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale    = scale;
        
        Collider c = go.GetComponent<Collider>();
        if (c != null) c.isTrigger = true;
        
        // Add Rigidbody to ensure trigger collisions fire even if hand lacks Rigidbody
        Rigidbody rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        // CRITICAL: Ensure the branches don't get moved by physics engines or explode apart
        rb.constraints = RigidbodyConstraints.FreezeAll;
        
        TreeBranchTrigger trigger = go.AddComponent<TreeBranchTrigger>();
        TreeHealer healer = FindObjectOfType<TreeHealer>();
        if (healer != null) {
            trigger.healer = healer;
        }

        Material mat = ParticleUtils.GetGlowingSphereMaterial();
        if (mat.HasProperty("_BaseColor"))  mat.SetColor("_BaseColor",  col);
        else                               mat.color = col;
        if (emission > 0.01f)
        {
            mat.SetColor("_EmissionColor", col * emission);
        }
        go.GetComponent<Renderer>().material = mat;

        if (healer != null)
        {
            healer.AddTreeRenderer(go.GetComponent<Renderer>());
        }
    }
}
