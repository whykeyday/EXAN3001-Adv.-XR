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

    void Start()
    {
        BuildTree();
        if (ambientAudio != null && !ambientAudio.isPlaying) ambientAudio.Play();
    }

    void Update()
    {
        // Gentle whole-tree sway
        if (treeRoot != null)
        {
            float s = 1f + Mathf.Sin(Time.time * swaySpeed) * swayAmplitude;
            treeRoot.transform.localScale = Vector3.one * s;
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

        Color woodCol = new Color(0.2f, 0.12f, 0.05f, 1f);
        float glow    = 6.0f;

        // Trunk (5 segments, short)
        for (int i = 0; i < 5; i++)
        {
            Vector3 pos = new Vector3(
                Random.Range(-0.02f, 0.02f),
                -0.35f + i * 0.07f,
                Random.Range(-0.02f, 0.02f));
            float scale = 0.12f * (1f - i / 7f);
            Sphere(container.transform, pos, Vector3.one * scale, woodCol, glow);
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
            Branch(container.transform, start, end, woodCol, 0.04f, glow);
        }
    }

    // ── Branch helper (matches GlassShardsSceneSetup.CreateBranch) ───────────────────────
    void Branch(Transform parent, Vector3 start, Vector3 end, Color col, float baseScale, float emission)
    {
        Vector3 dir = (end - start).normalized;
        float   len = Vector3.Distance(start, end);
        int     segs = Mathf.CeilToInt(len / 0.06f);
        for (int i = 0; i < segs; i++)
        {
            Vector3 pos   = start + dir * (i * len / segs);
            float   scale = baseScale * (1f - i * 0.8f / segs);
            Sphere(parent, pos, Vector3.one * scale, col, emission);
        }
    }

    // ── Sphere primitive helper ───────────────────────────────────────────────────────────
    void Sphere(Transform parent, Vector3 localPos, Vector3 scale, Color col, float emission)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale    = scale;
        Destroy(go.GetComponent<Collider>());

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
        go.GetComponent<Renderer>().material = mat;
    }
}
