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

    [Header("Real Asset References (Optional)")]
    [Tooltip("拖入你真正的篝火模型，这样火星就会附着在它上面")]
    public Transform fireplaceModel; 
    [Tooltip("如果你已经摆好了沙发，拖进来可以关掉占位白块")]
    public Transform sofaModel;
    [Tooltip("真实的猫咪模型")]
    public Transform catModel;

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
        Transform sparkParent;

        if (fireplaceModel != null) 
        {
            sparkParent = fireplaceModel;
            
            // 自动为真实的篝火模型添加抓取/触碰碰撞盒
            Collider col = sparkParent.gameObject.GetComponentInChildren<Collider>();
            if (col == null)
            {
                BoxCollider bc = sparkParent.gameObject.AddComponent<BoxCollider>();
                bc.isTrigger = true;
                bc.size = new Vector3(1.5f, 1f, 1.5f); // 范围给大一点，方便手摸到
            }
            else { col.isTrigger = true; } // 手能穿过去触发

            Rigidbody rb = sparkParent.gameObject.GetComponent<Rigidbody>();
            if (rb == null) { rb = sparkParent.gameObject.AddComponent<Rigidbody>(); rb.isKinematic = true; }

            // 挂载我们新写的专属篝火互动脚本！
            CampfireInteraction fireScript = sparkParent.gameObject.AddComponent<CampfireInteraction>();
            fireScript.fireplaceAudio = fireplaceAudio;
        }
        else 
        {
            GameObject fireplace = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fireplace.name = "Fireplace_Placeholder";
            fireplace.transform.position = new Vector3(0, 0.5f, 2.4f);
            fireplace.transform.localScale = new Vector3(1.5f, 1f, 0.5f);
            sparkParent = fireplace.transform;
        }
        
        // 创建壁炉火星特效
        GameObject sparks = new GameObject("FireSparksParticles");
        sparks.transform.SetParent(sparkParent);
        sparks.transform.localPosition = Vector3.up * 0.5f;

        ParticleSystem ps = sparks.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 3.0f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.0f, 2.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.06f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.3f, 0f, 1f), // 非常红
            new Color(1f, 0.6f, 0.1f, 1f)  // 橘色
        );
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 40f;
        
        // 初始关闭：等玩家碰到了再爆发！
        if (fireplaceModel != null)
        {
            emission.rateOverTime = 0f; 
        }

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
        if (sofaModel == null)
        {
            // 沙发占位
            GameObject sofa = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sofa.name = "Sofa_Placeholder";
            sofa.transform.position = new Vector3(0, 0.4f, 0);
            sofa.transform.localScale = new Vector3(2f, 0.6f, 1f);
        }

        Transform catParent;
        
        if (catModel != null)
        {
            catParent = catModel;
        }
        else
        {
            // 猫咪主容器体
            GameObject catContainer = new GameObject("Cat_Container_Placeholder");
            catContainer.transform.position = new Vector3(0, 0.85f, 0);
            catParent = catContainer.transform;

            // 绑定材质：给猫咪创建一个白橘混合的发光粒子系统。真实模型到位后，可以通过 ParticleContainerTool 生成基于具体身型的粒子表面。
            ParticleSystem catPs = catContainer.AddComponent<ParticleSystem>();
            var main = catPs.main;
            main.startSize = 0.05f;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.6f, 0f, 1f), Color.white); // 白橘相间
            var shape = catPs.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.4f; // 猫咪大致大小

            catPs.GetComponent<ParticleSystemRenderer>().material = ParticleUtils.GetGlowingSphereMaterial();
        }

        // 头部碰撞与互动逻辑
        GameObject catHead = new GameObject("Cat_Head_Trigger");
        catHead.transform.SetParent(catParent);
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
        catBody.transform.SetParent(catParent);
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

            // ================= 新增：自动获取并触发 FBX 动画 =================
            Animator anim = transform.root.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                // Play(0) 会强制从头播放 Animator 里的默认第一个动画片段
                anim.Play(0, 0, 0f);
                Debug.Log("[CatInteraction] Triggered Animator play!");
            }
            else
            {
                Animation legacyAnim = transform.root.GetComponentInChildren<Animation>();
                if (legacyAnim != null)
                {
                    legacyAnim.Play();
                    Debug.Log("[CatInteraction] Triggered Legacy Animation play!");
                }
            }
        }
    }
}

/// <summary>
/// 篝火专属触摸交互系统
/// 一旦玩家触碰，点燃自身包裹的粒子系统（颜色变红、开启波动）并喷发火星！
/// </summary>
public class CampfireInteraction : MonoBehaviour
{
    public AudioSource fireplaceAudio;
    private float lastTouchTime = -999f;
    private const float Cooldown = 2.0f;
    private ParticleSystem containerPs; // 篝火表面的全息粒子
    private ParticleSystem sparksPs; // 往天空飘升的火星粒子

    private bool isIgnited = false;

    void Start()
    {
        // 尝试获取全息容器系统（由 ParticleContainerTool 生成的贴脸粒子）
        containerPs = GetComponent<ParticleSystem>();
        
        // 查找向上的那团喷发火星
        Transform sparks = transform.Find("FireSparksParticles");
        if (sparks != null) sparksPs = sparks.GetComponent<ParticleSystem>();
    }

    void OnTriggerEnter(Collider other)
    {
        // 忽略自身
        if (other.transform.IsChildOf(transform.root)) return; 
        
        if (Time.time - lastTouchTime < Cooldown) return;

        // 判断是否是玩家手柄
        if (other.CompareTag("PlayerHand") || other.name.ToLower().Contains("hand") || other.name.ToLower().Contains("controller"))
        {
            lastTouchTime = Time.time;
            IgniteFireplace();
        }
    }

    void IgniteFireplace()
    {
        if (!isIgnited)
        {
            isIgnited = true;
            Debug.Log("[CampfireInteraction] Fireplace Ignited by Player!");

            // 1. 播放柴火燃烧立体声（可循环播放）
            if (fireplaceAudio != null && !fireplaceAudio.isPlaying) 
            {
                fireplaceAudio.loop = true;
                fireplaceAudio.Play();
            }

            // 2. 燃烧篝火本身表面的粒子容器：让粒子变成炽热的红黄，并疯狂波动！
            if (containerPs != null)
            {
                var main = containerPs.main;
                // 色彩瞬间变为赤红与亮黄的混合
                main.startColor = new ParticleSystem.MinMaxGradient(
                    new Color(1f, 0.1f, 0f, 1f), // 炽红
                    new Color(1f, 0.6f, 0f, 1f)  // 亮橘
                );

                // 开启 Noise 扰动模块，让附着的粒子像真正的火焰一样翻滚、升腾
                var noise = containerPs.noise;
                noise.enabled = true;
                noise.strength = 0.4f; // 扭曲幅度
                noise.frequency = 1.0f; // 扭曲频率
                noise.scrollSpeed = 1.2f; // 火焰向上升腾的滚动感

                // 赋予生命周期内向上飘升的热气流速度
                var vel = containerPs.velocityOverLifetime;
                vel.enabled = true;
                vel.y = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
                vel.space = ParticleSystemSimulationSpace.World;
            }

            // 3. 喷发出中心炽热的飞升火星碎片
            if (sparksPs != null)
            {
                var emission = sparksPs.emission;
                emission.rateOverTime = 60f; // 火焰爆发！
                sparksPs.Play();
            }
        }
    }
}
