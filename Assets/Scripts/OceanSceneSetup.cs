using UnityEngine;

/// <summary>
/// OceanSceneSetup — Builds the ocean bubble world at runtime for OceanScene.
///
/// HOW TO SET UP in Unity Editor (OceanScene):
///   1. Create an empty GameObject, name it "OceanManager"
///   2. Add Component → OceanSceneSetup  (BreathInputManager is added automatically)
///   3. Create an AudioSource on OceanManager, assign your ocean ambient clip, enable Loop
///   4. Drag that AudioSource into the "Ocean Audio" slot in the Inspector
///   5. Optionally drag a coral-particle audio clip into "Coral Audio" for breath-driven volume
/// </summary>
[RequireComponent(typeof(BreathInputManager))]
public class OceanSceneSetup : MonoBehaviour
{
    [Header("Breath")]
    public BreathInputManager breathInput;   // Auto-found on same GO

    [Header("Fog (breath-driven)")]
    public bool enableFog = true;
    [Range(0f, 0.1f)] public float minFog = 0.003f;
    [Range(0f, 0.1f)] public float maxFog  = 0.04f;

    [Header("Audio (breath-driven volume)")]
    [Tooltip("AudioSource with ocean/underwater ambient clip (Loop = true).")]
    public AudioSource oceanAudio;
    [Range(0f, 1f)] public float minVolume = 0.15f;
    [Range(0f, 1f)] public float maxVolume = 1.0f;

    [Header("Bubble Cluster")]
    [Tooltip("World-space centre of the bubble cluster.")]
    public Vector3 clusterCenter = new Vector3(0f, 1.4f, 2f);
    public float bubbleScale = 1.6f;    // Larger bubbles than the shard inset version

    [Header("Float Animation (FloatBob component)")]
    public float bobAmplitude = 0.04f;
    public float bobPeriod    = 6f;

    void Awake()
    {
        if (breathInput == null) breathInput = GetComponent<BreathInputManager>();
    }

    void Start()
    {
        if (enableFog)
        {
            RenderSettings.fog      = true;
            RenderSettings.fogMode  = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0f, 0.15f, 0.4f);
            RenderSettings.fogDensity = minFog;
        }

        BuildBubbles();

        if (oceanAudio != null && !oceanAudio.isPlaying) oceanAudio.Play();
    }

    void Update()
    {
        if (breathInput == null) return;
        float b = breathInput.BreathValue;

        if (enableFog)
            RenderSettings.fogDensity = Mathf.Lerp(minFog, maxFog, b);

        if (oceanAudio != null)
            oceanAudio.volume = Mathf.Lerp(minVolume, maxVolume, b);
    }

    // ── Procedural bubble cluster (matches GlassShardsSceneSetup.CreateOceanWorld) ──────
    void BuildBubbles()
    {
        var root = new GameObject("BubbleCluster");
        root.transform.position = clusterCenter;

        Random.InitState((int)(Time.realtimeSinceStartup * 1000));

        // Layer 1 — large faint
        for (int i = 0; i < 8; i++)
            AddBubble(root.transform, Random.insideUnitSphere * 0.35f,
                      Random.Range(0.08f, 0.15f), new Color(0f, 0.5f, 1f, 0.3f), 1.5f);

        // Layer 2 — medium bright
        for (int i = 0; i < 20; i++)
            AddBubble(root.transform, Random.insideUnitSphere * 0.4f,
                      Random.Range(0.04f, 0.07f),
                      Random.value > 0.4f ? new Color(0f, 0.7f, 1f, 0.8f) : new Color(0f, 0.2f, 1f, 0.9f), 5.5f);

        // Layer 3 — tiny glitter
        for (int i = 0; i < 40; i++)
            AddBubble(root.transform, Random.insideUnitSphere * 0.45f,
                      Random.Range(0.015f, 0.03f),
                      Random.value > 0.3f ? new Color(0.3f, 0.8f, 1f, 0.95f) : new Color(0f, 0.4f, 1f, 0.95f), 8.5f);
    }

    void AddBubble(Transform parent, Vector3 localPos, float size, Color col, float emission)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale    = Vector3.one * size * bubbleScale;
        Destroy(go.GetComponent<Collider>());

        // Material
        Shader sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var mat = new Material(sh);
        if (mat.HasProperty("_BaseColor"))  mat.SetColor("_BaseColor",  col);
        else                               mat.color = col;
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0f);
        if (mat.HasProperty("_Metallic"))   mat.SetFloat("_Metallic",   0f);
        if (emission > 0.01f)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", col * emission);
        }
        if (col.a < 1f)
        {
            mat.SetFloat("_Surface", 1);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = 3000;
        }
        go.GetComponent<Renderer>().material = mat;

        // FloatBob (existing script reuse)
        var bob = go.AddComponent<FloatBob>();
        bob.amplitude    = bobAmplitude;
        bob.period       = bobPeriod;
        bob.randomPhase  = true;
    }
}
