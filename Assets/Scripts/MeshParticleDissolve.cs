using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(ParticleSystem))]
public class MeshParticleDissolve : MonoBehaviour
{
    // ============================================================
    //  INSPECTOR SETTINGS — Edit these in Unity without coding!
    // ============================================================

    [Header("── Timing ──")]
    [Tooltip("How long the mesh dissolves (seconds)")]
    public float duration = 1.5f;

    [Header("── Wind / Direction ──")]
    [Tooltip("Direction particles drift. X=right, Y=up, Z=forward. Change in Inspector!")]
    public Vector3 windDirection = new Vector3(2.5f, 0.8f, 0f);

    [Header("── Particle Look ──")]
    [Tooltip("Noise turbulence strength — higher = more curvy silk arcs")]
    public float noiseStrength = 1.2f;
    [Tooltip("Smallest particle size (meters)")]
    public float particleSizeMin = 0.002f;
    [Tooltip("Largest particle size (meters, large = bokeh blob)")]
    public float particleSizeMax = 0.12f;
    [Tooltip("HDR multiplier for bloom brightness. Values > 1 trigger Global Volume Bloom glow.")]
    public float bloomBrightness = 2.5f;

    [Header("── Particle Color ──")]
    [Tooltip("Start color of burst (bright white-gold = HDR)")]
    public Color colorStart  = new Color(1f, 1f, 0.5f, 1f);
    [Tooltip("End color as particles fade")]
    public Color colorEnd    = new Color(1f, 0.5f, 0.02f, 1f);

    [Header("── Trail / Silk Ribbon ──")]
    [Tooltip("How long each particle's ribbon trail lingers (seconds)")]
    public float trailLifetime = 0.25f;
    [Tooltip("Trail head color")]
    public Color trailColorHead = new Color(1f, 0.85f, 0.3f, 1f);
    [Tooltip("Trail tail color (usually transparent)")]
    public Color trailColorTail = new Color(1f, 0.5f, 0.0f, 0f);

    [Header("── Material Settings ──")]
    public Material dissolveMaterialTemplate; // Optional

    private ParticleSystem ps;
    private MeshFilter meshFilter;
    private static Texture2D _cachedGoldTex;

    static Texture2D GetGoldCircleTexture(int size = 64)
    {
        if (_cachedGoldTex != null) return _cachedGoldTex;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        float half = size * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - half) / half;
                float dy = (y - half) / half;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01(1f - Mathf.SmoothStep(0.3f, 1.0f, dist) * 2f);
                Color pixel = Color.Lerp(Color.white, new Color(1f, 0.7f, 0.05f, 0f), Mathf.SmoothStep(0f, 1f, dist));
                pixel.a = alpha;
                tex.SetPixel(x, y, pixel);
            }
        }
        tex.Apply();
        _cachedGoldTex = tex;
        return tex;
    }

    void SetupParticleMaterial()
    {
        var psr = ps.GetComponent<ParticleSystemRenderer>();
        if (psr == null) return;

        Texture2D goldTex = GetGoldCircleTexture();
        Shader sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (sh == null) sh = Shader.Find("Particles/Additive");
        if (sh == null) sh = Shader.Find("Sprites/Default");

        Material mat = new Material(sh);
        mat.SetTexture("_BaseMap", goldTex);
        mat.SetTexture("_MainTex", goldTex);

        // SrcAlpha controls circle edge → round shape
        // One (dst) = additive glow → HDR center exceeds 1.0 → Bloom fires
        mat.SetInt("_SrcBlend",  (int)BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend",  (int)BlendMode.One);
        mat.SetInt("_ZWrite",    0);
        mat.SetInt("_ZTest",     4);
        mat.SetInt("_Surface",   1);
        mat.SetInt("_BlendMode", 2); // 2 = Additive in URP Particles/Unlit
        mat.SetFloat("_ColorMode", 0);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.EnableKeyword("_BLENDMODE_ADD");
        mat.renderQueue = 3500;
        psr.material = mat;

        // Trail (silk ribbon)
        var trails = ps.trails;
        trails.enabled = true;
        trails.mode = ParticleSystemTrailMode.PerParticle;
        trails.ratio = 1.0f;
        trails.lifetime = new ParticleSystem.MinMaxCurve(trailLifetime);
        trails.minVertexDistance = 0.005f;
        trails.worldSpace = true;
        trails.dieWithParticles = false;
        trails.sizeAffectsWidth = true;
        trails.widthOverTrail = new ParticleSystem.MinMaxCurve(1.0f,
            new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 0)));
        trails.colorOverTrail = new ParticleSystem.MinMaxGradient(trailColorHead, trailColorTail);
        trails.textureMode = ParticleSystemTrailTextureMode.Stretch;
        psr.trailMaterial = mat;
    }

    void Awake()
    {
        ps = GetComponent<ParticleSystem>();
        meshFilter = GetComponent<MeshFilter>();
    }

    public void TriggerDissolve(System.Action onComplete)
    {
        StartCoroutine(DissolveRoutine(onComplete));
    }

    private IEnumerator DissolveRoutine(System.Action onComplete)
    {
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Mesh;
        shape.mesh = meshFilter.sharedMesh;
        shape.scale = transform.localScale;

        var renderers = GetComponentsInChildren<Renderer>();
        var fadeMaterials = new List<Material>();
        foreach (var r in renderers)
        {
            if (r is ParticleSystemRenderer) continue;
            if (r.sharedMaterial == null) continue;
            Material m = new Material(r.sharedMaterial);
            m.SetFloat("_Surface", 1);
            m.SetFloat("_Blend", 0);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0);
            m.renderQueue = 3000;
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            if (m.HasProperty("_BaseColor")) { Color c = m.GetColor("_BaseColor"); c.a = 1f; m.SetColor("_BaseColor", c); }
            r.material = m;
            fadeMaterials.Add(m);
        }

        var main = ps.main;
        main.loop = false;
        main.duration = duration;
        main.startLifetime = new ParticleSystem.MinMaxCurve(duration * 1.5f, duration * 3.0f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.0f, 0.15f);
        main.startSize = new ParticleSystem.MinMaxCurve(particleSizeMin, particleSizeMax);
        main.startColor = new ParticleSystem.MinMaxGradient(
            colorStart * bloomBrightness,
            colorEnd
        );
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 6000;

        var emission = ps.emission;
        emission.rateOverTime = 1200f;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 800, 1, 0.03f) });

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(windDirection.x * 0.3f, windDirection.x * 1.0f);
        velocity.y = new ParticleSystem.MinMaxCurve(windDirection.y * 0.0f, windDirection.y * 1.0f);
        velocity.z = new ParticleSystem.MinMaxCurve(-windDirection.x * 0.15f, windDirection.x * 0.15f);

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = noiseStrength;
        noise.frequency = 0.3f;
        noise.scrollSpeed = 0.6f;
        noise.octaveCount = 3;
        noise.octaveMultiplier = 0.5f;
        noise.octaveScale = 2.0f;
        noise.damping = false;
        noise.quality = ParticleSystemNoiseQuality.High;

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve(
            new Keyframe(0.0f,  0.0f),
            new Keyframe(0.03f, 1.3f),
            new Keyframe(0.07f, 1.0f),
            new Keyframe(0.55f, 1.0f),
            new Keyframe(0.75f, 1.2f),
            new Keyframe(1.0f,  0.0f)
        );
        size.size = new ParticleSystem.MinMaxCurve(1.0f, sizeCurve);

        var rotation = ps.rotationOverLifetime;
        rotation.enabled = true;
        rotation.z = new ParticleSystem.MinMaxCurve(-180f, 180f);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(Color.white,                    0.00f),
                new GradientColorKey(new Color(1f, 0.95f, 0.5f), 0.15f),
                new GradientColorKey(new Color(1f, 0.80f, 0.2f), 0.50f),
                new GradientColorKey(new Color(0.9f, 0.5f, 0.0f), 0.90f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(1.0f, 0.00f),
                new GradientAlphaKey(1.0f, 0.75f),
                new GradientAlphaKey(0.0f, 1.00f)
            }
        );
        col.color = grad;

        SetupParticleMaterial();
        ps.Play();

        float timer = 0f;
        while (timer < duration)
        {
            float t = timer / duration;
            float alpha = 1f - (t * t * (3f - 2f * t));
            foreach (var mat in fadeMaterials)
            {
                if (mat.HasProperty("_BaseColor")) { Color c = mat.GetColor("_BaseColor"); c.a = alpha; mat.SetColor("_BaseColor", c); }
            }
            timer += Time.deltaTime;
            yield return null;
        }
        foreach (var r in renderers)
        {
            if (r is ParticleSystemRenderer) continue;
            r.enabled = false;
        }

        yield return new WaitForSeconds(duration * 1.0f);
        onComplete?.Invoke();
    }
}
