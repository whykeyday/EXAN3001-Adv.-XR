using UnityEngine;

/// <summary>
/// CatSceneSetup — Builds the full cat in CatScene at runtime.
/// Touching the cat's head triggers a sound.
///
/// HOW TO SET UP in Unity Editor (CatScene):
///   1. Create an empty GameObject, name it "CatManager"
///   2. Add Component → CatSceneSetup
///   3. Add two AudioSource components on CatManager:
///        • First:  ambient purr (Loop = true) → drag to "Ambient Audio"
///        • Second: meow / reaction sound      → drag to "Touch Audio"
///   4. Drag the meow AudioClip asset into "Touch Clip"
///   5. Optionally set Cat Center to wherever you want the cat to appear
/// </summary>
public class CatSceneSetup : MonoBehaviour
{
    [Header("Position")]
    public Vector3 catCenter = new Vector3(0f, 1.2f, 1.5f);

    [Header("Audio")]
    [Tooltip("Looping ambient purring sound.")]
    public AudioSource ambientAudio;
    [Tooltip("Short reaction AudioSource (not looping).")]
    public AudioSource touchAudio;
    [Tooltip("Clip played when the cat's head is touched (e.g. meow).")]
    public AudioClip touchClip;

    [Header("Idle Pulse")]
    public float pulseAmplitude = 0.018f;
    public float pulseSpeed     = 1.1f;

    private GameObject catRoot;

    void Start()
    {
        BuildCat();
        if (ambientAudio != null && !ambientAudio.isPlaying) ambientAudio.Play();
    }

    void Update()
    {
        if (catRoot != null)
        {
            float s = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmplitude;
            catRoot.transform.localScale = Vector3.one * s;
        }
    }

    // ── Geometry (matches GlassShardsSceneSetup.CreateCatWorld exactly) ─────────────────
    void BuildCat()
    {
        catRoot = new GameObject("CatRoot");
        catRoot.transform.position = catCenter;

        var body = new GameObject("CatBody");
        body.transform.SetParent(catRoot.transform, false);
        body.transform.localRotation = Quaternion.Euler(0, 55, 0);

        Color w = Color.white;
        float g = 1.2f;

        // Body
        Part(body.transform, new Vector3(0, -0.15f, 0.15f), Vector3.one * 0.45f, w, g);

        // Head — store reference to add touch trigger
        var head = Part(body.transform, new Vector3(0, 0.15f, -0.1f), Vector3.one * 0.3f, w, g);
        AddHeadTrigger(head);

        // Ears
        Part(body.transform, new Vector3(-0.1f, 0.28f, -0.1f), new Vector3(0.12f, 0.08f, 0.05f), w, g)
            .transform.localRotation = Quaternion.Euler(0, 0, 25);
        Part(body.transform, new Vector3( 0.1f, 0.28f, -0.1f), new Vector3(0.12f, 0.08f, 0.05f), w, g)
            .transform.localRotation = Quaternion.Euler(0, 0, -25);

        // Eyes
        var eyeCol = new Color(0.25f, 0.25f, 0.25f, 1f);
        Part(body.transform, new Vector3(-0.08f, 0.18f, -0.22f), Vector3.one * 0.06f, eyeCol, 0f);
        Part(body.transform, new Vector3( 0.08f, 0.18f, -0.22f), Vector3.one * 0.06f, eyeCol, 0f);

        // Whiskers
        for (int i = 1; i <= 2; i++)
        {
            Part(body.transform, new Vector3( 0.12f + i * 0.05f, 0.12f, -0.2f), Vector3.one * 0.02f, w, 1f);
            Part(body.transform, new Vector3(-0.12f - i * 0.05f, 0.12f, -0.2f), Vector3.one * 0.02f, w, 1f);
        }

        // Tail (wraps from back to front)
        for (int i = 0; i < 15; i++)
        {
            float t = i / 15f;
            float angle = t * Mathf.PI;
            float x = Mathf.Sin(angle) * 0.32f + (Mathf.Sin(angle) > 0 ? 0.05f : 0f);
            float z = Mathf.Cos(angle) * 0.32f + 0.15f;
            float y = -0.25f + Mathf.Sin(t * Mathf.PI) * 0.05f;
            Part(body.transform, new Vector3(x, y, z), Vector3.one * Mathf.Lerp(0.11f, 0.04f, t), w, g);
        }
    }

    // ── Touch trigger on the head ────────────────────────────────────────────────────────
    void AddHeadTrigger(GameObject head)
    {
        var col    = head.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius    = 0.65f; // generous VR reach

        var rb = head.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity  = false;

        var rx = head.AddComponent<CatTouchReceiver>();
        rx.ambientAudio = ambientAudio;
        rx.touchAudio   = touchAudio;
        rx.touchClip    = touchClip;
    }

    // ── Sphere primitive helper (same as GlassShardsSceneSetup.CreateParticle) ──────────
    GameObject Part(Transform parent, Vector3 pos, Vector3 scale, Color col, float emission)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale    = scale;
        Destroy(go.GetComponent<Collider>());

        Shader sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var mat = new Material(sh);
        if (mat.HasProperty("_BaseColor"))  mat.SetColor("_BaseColor",  col);
        else                               mat.color = col;
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0f);
        if (mat.HasProperty("_Metallic"))   mat.SetFloat("_Metallic",   0f);
        if (emission > 0.01f) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", col * emission); }
        go.GetComponent<Renderer>().material = mat;
        return go;
    }
}

// ─────────────────────────────────────────────────────────────────────────────────────────
/// <summary>
/// Placed on the cat's head. Fires audio when a VR hand or XR controller enters the trigger.
/// </summary>
public class CatTouchReceiver : MonoBehaviour
{
    [HideInInspector] public AudioSource ambientAudio;
    [HideInInspector] public AudioSource touchAudio;
    [HideInInspector] public AudioClip   touchClip;

    private const float Cooldown = 0.8f;
    private float lastTime = -999f;

    void OnTriggerEnter(Collider other)
    {
        if (other.transform.IsChildOf(transform.root)) return; // ignore own parts
        if (Time.time - lastTime < Cooldown) return;
        lastTime = Time.time;

        Debug.Log($"[CatTouchReceiver] Touched by {other.name}");

        if (touchClip != null)
        {
            var src = touchAudio != null ? touchAudio : ambientAudio;
            if (src != null) src.PlayOneShot(touchClip);
        }
        else if (ambientAudio != null && ambientAudio.clip != null)
        {
            ambientAudio.PlayOneShot(ambientAudio.clip);
        }
    }
}
