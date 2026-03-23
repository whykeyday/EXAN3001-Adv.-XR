using UnityEngine;

/// <summary>
/// Coral container logic. 
/// 1. Pulses based on BreathInputManager.
/// 2. Emits yellow particles on touch.
/// 可以挂载在使用 ParticleContainer 生成的珊瑚模型主体上。
/// </summary>
public class CoralInteractor : MonoBehaviour
{
    private BreathInputManager breath;
    private ParticleSystem yellowTouchParticles;

    [Header("Breath Animation")]
    public float baseScale = 1.0f;
    public float breathScaleMultiplier = 1.25f; // scale up to 25% larger on heavy breath
    public float baseFloatAmplitude = 0.02f;
    public float maxFloatAmplitude = 0.08f;
    public float baseFloatSpeed = 1f;
    public float maxFloatSpeed = 3f;

    private Vector3 initialScale;
    private Vector3 initialLocalPos;
    private float floatTimer = 0f;

    void Start()
    {
        breath = FindObjectOfType<BreathInputManager>();
        initialScale = transform.localScale;
        initialLocalPos = transform.localPosition;
        
        // 确保有 Trigger 可以被手触碰到
        if (GetComponent<Collider>() == null)
        {
            BoxCollider col = gameObject.AddComponent<BoxCollider>();
            col.isTrigger = true;
        }
        else
        {
            Collider[] cols = GetComponents<Collider>();
            foreach(var c in cols) c.isTrigger = true;
        }

        CreateTouchParticles();
    }

    void Update()
    {
        // 按照最新需求，大珊瑚不再随着呼吸跳动和变大
        // 仅保留下面的交互特效逻辑
    }

    void CreateTouchParticles()
    {
        GameObject pObj = new GameObject("YellowTouchParticles");
        pObj.transform.SetParent(transform);
        pObj.transform.localPosition = Vector3.zero;

        yellowTouchParticles = pObj.AddComponent<ParticleSystem>();
        var main = yellowTouchParticles.main;
        main.loop = false;
        main.startLifetime = 1.5f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.08f);
        main.startColor = new Color(1f, 0.9f, 0.2f, 1f); // Yellow

        var emission = yellowTouchParticles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 20f) });

        var shape = yellowTouchParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.5f;

        yellowTouchParticles.GetComponent<ParticleSystemRenderer>().material = ParticleUtils.GetGlowingSphereMaterial();
    }

    void OnTriggerEnter(Collider other)
    {
        // 判断被玩家手部碰触 (Fallback checking names if Tags are not standard)
        if (other.CompareTag("PlayerHand") || other.name.ToLower().Contains("hand") || other.name.ToLower().Contains("controller"))
        {
            if (yellowTouchParticles != null)
                yellowTouchParticles.Play();
        }
    }
}
