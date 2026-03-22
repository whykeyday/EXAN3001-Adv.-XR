using UnityEngine;

/// <summary>
/// 猫咪场景设置 (重构版)
/// 建立基于新美术资产结构的占位模型及系统逻辑。包括房间粒子容器、壁炉特效以及按部位分别互动的猫咪容器。
/// 
/// HOW TO SET UP in Unity Editor (CatScene):
///   1. 创建一个名为 "CatManager" 的空物体并挂载 CatSceneSetup。
///   2. 将录制的三种音频源 (壁炉柴火、呼噜声、喵叫声) 拖入对应的 Audio Placeholder 槽中。
///   3. 直接运行即可生成场景全貌，供后续将 Placeholder 网格替换为专门的模型资产。
/// </summary>
public class CatSceneSetup : MonoBehaviour
{
    [Header("Audio Placeholders")]
    public AudioSource fireplaceAudio;
    public AudioSource catPurrAudio;
    public AudioSource catMeowAudio;

    void Start()
    {
        CreateRoomPlaceholder();
        CreateFireplacePlaceholder();
        CreateSofaAndCatPlaceholder();
        
        // 进入场景伴随壁炉炸裂声
        if (fireplaceAudio != null && !fireplaceAudio.isPlaying) 
        {
            fireplaceAudio.loop = true;
            fireplaceAudio.Play();
        }
        else if (fireplaceAudio == null)
        {
            Debug.Log("[Placeholder] Fireplace audio (crackling wood) missing. Please attach AudioSource.");
        }
    }

    /// <summary>
    /// 生成壁橱房间占位符
    /// </summary>
    void CreateRoomPlaceholder()
    {
        GameObject room = GameObject.CreatePrimitive(PrimitiveType.Cube);
        room.name = "ClosetRoom_Placeholder";
        room.transform.position = new Vector3(0, 1.5f, 0);
        room.transform.localScale = new Vector3(5f, 3f, 5f);
        
        // 这里只是个占位，真正的粒子房间需要你在该物体上挂载自己捏的倒角模型网格 
        // 然后使用 Tools -> Make Particle Container 把它的模型转化为星星粒子。
        Renderer r = room.GetComponent<Renderer>();
        r.enabled = false; // 隐藏原始立方体外壳，等待用粒子取代
        
        Debug.Log("[Placeholder] Closet Room Created. Target this with ParticleContainerTool.");
    }

    /// <summary>
    /// 生成壁炉及火星特效占位
    /// </summary>
    void CreateFireplacePlaceholder()
    {
        GameObject fireplace = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fireplace.name = "Fireplace_Placeholder";
        fireplace.transform.position = new Vector3(0, 0.5f, 2.4f);
        fireplace.transform.localScale = new Vector3(1.5f, 1f, 0.5f);
        
        // 创建壁炉火星特效
        GameObject sparks = new GameObject("FireSparksParticles");
        sparks.transform.SetParent(fireplace.transform);
        sparks.transform.localPosition = Vector3.up * 0.5f;

        ParticleSystem ps = sparks.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 3.0f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.0f, 2.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.06f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.4f, 0f, 1f), // 橘红
            new Color(1f, 0.8f, 0.2f, 1f)  // 黄色
        );
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 40f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(1.2f, 0.1f, 0.3f);

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.y = new ParticleSystem.MinMaxCurve(1.0f, 2.0f); // 向上漂浮
        vel.x = new ParticleSystem.MinMaxCurve(-0.5f, 0.5f); // 左右摇摆
        
        var colorOL = ps.colorOverLifetime;
        colorOL.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) }
        );
        colorOL.color = grad;

        ps.GetComponent<ParticleSystemRenderer>().material = ParticleUtils.GetGlowingSphereMaterial();
    }

    /// <summary>
    /// 生成沙发及粒子橘白猫占位，并分配分别挂载不同播放逻辑的碰触接收器
    /// </summary>
    void CreateSofaAndCatPlaceholder()
    {
        // 沙发占位
        GameObject sofa = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sofa.name = "Sofa_Placeholder";
        sofa.transform.position = new Vector3(0, 0.4f, 0);
        sofa.transform.localScale = new Vector3(2f, 0.6f, 1f);
        
        // 猫咪主容器体
        GameObject catContainer = new GameObject("Cat_Container_Placeholder");
        catContainer.transform.position = new Vector3(0, 0.85f, 0);

        // 绑定材质：给猫咪创建一个白橘混合的发光粒子系统。真实模型到位后，可以通过 ParticleContainerTool 生成基于具体身型的粒子表面。
        ParticleSystem catPs = catContainer.AddComponent<ParticleSystem>();
        var main = catPs.main;
        main.startSize = 0.05f;
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.6f, 0f, 1f), Color.white); // 白橘相间
        var shape = catPs.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.4f; // 猫咪大致大小

        catPs.GetComponent<ParticleSystemRenderer>().material = ParticleUtils.GetGlowingSphereMaterial();

        // 头部碰撞与互动逻辑
        GameObject catHead = new GameObject("Cat_Head_Trigger");
        catHead.transform.SetParent(catContainer.transform);
        catHead.transform.localPosition = new Vector3(0, 0.2f, -0.3f);
        
        SphereCollider headCol = catHead.AddComponent<SphereCollider>();
        headCol.isTrigger = true;
        headCol.radius = 0.25f;
        
        Rigidbody headRb = catHead.AddComponent<Rigidbody>();
        headRb.isKinematic = true;
        
        CatTouchReceiver headReceiver = catHead.AddComponent<CatTouchReceiver>();
        headReceiver.bodyPart = CatPart.Head;
        headReceiver.audioSource = catPurrAudio;

        // 身体碰撞与互动逻辑
        GameObject catBody = new GameObject("Cat_Body_Trigger");
        catBody.transform.SetParent(catContainer.transform);
        catBody.transform.localPosition = new Vector3(0, 0, 0);
        
        SphereCollider bodyCol = catBody.AddComponent<SphereCollider>();
        bodyCol.isTrigger = true;
        bodyCol.radius = 0.4f;
        
        Rigidbody bodyRb = catBody.AddComponent<Rigidbody>();
        bodyRb.isKinematic = true;
        
        CatTouchReceiver bodyReceiver = catBody.AddComponent<CatTouchReceiver>();
        bodyReceiver.bodyPart = CatPart.Body;
        bodyReceiver.audioSource = catMeowAudio;
    }
}

public enum CatPart { Head, Body }

/// <summary>
/// 猫咪触摸感应器：分别响应头部（呼噜声）和身体（喵叫声）
/// </summary>
public class CatTouchReceiver : MonoBehaviour
{
    public CatPart bodyPart;
    public AudioSource audioSource;
    
    private float lastTouchTime = -999f;
    private const float Cooldown = 1.0f; // 避免疯狂手抖重复触发

    void OnTriggerEnter(Collider other)
    {
        // 忽略父物体的内部穿插
        if (other.transform.IsChildOf(transform.root)) return; 
        
        if (Time.time - lastTouchTime < Cooldown) return;

        // 简易判断：只要接触物是玩家手柄/手
        if (other.CompareTag("PlayerHand") || other.name.ToLower().Contains("hand") || other.name.ToLower().Contains("controller"))
        {
            lastTouchTime = Time.time;
            
            Debug.Log($"[CatInteraction] Player touched {bodyPart}");

            if (audioSource != null && audioSource.clip != null)
            {
                audioSource.PlayOneShot(audioSource.clip);
            }
            else
            {
                Debug.LogWarning($"[Placeholder] No AudioClip attached for Cat {bodyPart}!");
            }
        }
    }
}
