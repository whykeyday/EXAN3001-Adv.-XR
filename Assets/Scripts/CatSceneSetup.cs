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
    [Header("1. Black Cat (Aggr Sound on Touch)")]
    public Transform blackCatModel;
    public AudioClip blackCatAggrAudio;

    [Header("2. Murdered Soul Suspect Cat (Auto 5s Anim)")]
    public Transform murderedCatModel;

    [Header("3. Toon Cat Free (Purr + Anim on Touch)")]
    public Transform toonCatModel;
    public AudioClip toonCatPurrAudio;

    [Header("Real Asset References (Optional)")]
    [Tooltip("拖入你真正的篝火模型，这样火星就会附着在它上面")]
    public Transform fireplaceModel; 
    [Tooltip("如果你已经摆好了沙发，拖进来可以关掉占位白块")]
    public Transform sofaModel;
    public AudioSource fireplaceAudio;

    void Start()
    {
        CreateRoomPlaceholder();
        CreateFireplacePlaceholder();
        SetupFurnitureAndCats();
        
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
            
            // 确保没有分配模型时，互动依然挂载生效
            CampfireInteraction fireScript = sparkParent.gameObject.AddComponent<CampfireInteraction>();
            fireScript.fireplaceAudio = fireplaceAudio;
        }
        
        // 创建壁炉火星特效 (脱离父物体，绝对防止 FBX 的变态缩放把特效缩成 0.0001 毫米导致看不见)
        GameObject sparks = new GameObject("FireSparksParticles");
        sparks.transform.position = sparkParent.position + Vector3.up * 0.5f;
        sparks.transform.localScale = Vector3.one;

        ParticleSystem ps = sparks.AddComponent<ParticleSystem>();
        var main = ps.main;
        // 强制特效使用绝对世界大小，不随父物体畸变
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
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
        vel.y = new ParticleSystem.MinMaxCurve(2.0f, 5.0f); // 向上漂浮
        vel.x = new ParticleSystem.MinMaxCurve(-1.0f, 1.0f); // 左右摇摆
        
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
    void SetupFurnitureAndCats()
    {
        // ================= 沙发处理 =================
        if (sofaModel == null)
        {
            GameObject sofa = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sofa.name = "Sofa_Placeholder";
            sofa.transform.position = new Vector3(0, 0.4f, 0);
            sofa.transform.localScale = new Vector3(2f, 0.6f, 1f);
        }

        // ================= 三只猫专属业务逻辑分发 =================
        // 1. 黑猫 (遇人发出凶狠叫声)
        if (blackCatModel != null)
        {
            EnsureColliderAndRigidBody(blackCatModel, new Vector3(0.6f, 0.6f, 0.6f));
            CatTouchReceiver rec = blackCatModel.gameObject.AddComponent<CatTouchReceiver>();
            rec.catRole = CatTouchReceiver.CatRole.Aggressive;
            if (blackCatAggrAudio != null) rec.audioSource = CreateAudioSource(blackCatModel, blackCatAggrAudio);
        }

        // 2. 灵魂疑犯猫 (天然纯动画猫，已经设置了 Loop Time，所以不要加任何额外脚本干扰它)
        if (murderedCatModel != null)
        {
            // 你已经在 Unity 勾选了 Loop Time，它天然就是无限死循环播放的！
            // 所以我们彻底拔掉这只猫身上的全部干扰代码，让它原生态活下去。
        }

        // 3. 卡通猫 (摸一下以后呼噜噜，并触发动画)
        if (toonCatModel != null)
        {
            EnsureColliderAndRigidBody(toonCatModel, new Vector3(0.5f, 0.5f, 0.5f));
            CatTouchReceiver rec = toonCatModel.gameObject.AddComponent<CatTouchReceiver>();
            rec.catRole = CatTouchReceiver.CatRole.Purr;
            if (toonCatPurrAudio != null) rec.audioSource = CreateAudioSource(toonCatModel, toonCatPurrAudio);
        }
    }

    AudioSource CreateAudioSource(Transform parent, AudioClip clip)
    {
        AudioSource src = parent.gameObject.AddComponent<AudioSource>();
        src.clip = clip;
        src.spatialBlend = 1f; // 3D 立体声
        src.playOnAwake = false;
        return src;
    }

    void EnsureColliderAndRigidBody(Transform target, Vector3 colSize)
    {
        Collider col = target.GetComponentInChildren<Collider>();
        if (col == null)
        {
            BoxCollider bc = target.gameObject.AddComponent<BoxCollider>();
            bc.isTrigger = true;
            bc.size = colSize;
        }
        else { col.isTrigger = true; }

        Rigidbody rb = target.GetComponent<Rigidbody>();
        if (rb == null) { rb = target.gameObject.AddComponent<Rigidbody>(); rb.isKinematic = true; }
    }
}

public class AutoCatAnimator : MonoBehaviour
{
    public float interval = 5.0f;
    private Animator anim;
    private Animation legacyAnim;

    void Start()
    {
        anim = GetComponentInChildren<Animator>();
        legacyAnim = GetComponentInChildren<Animation>();
        InvokeRepeating(nameof(TriggerAnimation), interval, interval);
    }

    void TriggerAnimation()
    {
        // 彻底解决直接导入的 FBX 没有 Animator Controller 的问题
        if (anim != null)
        {
            if (anim.runtimeAnimatorController != null)
            {
                int stateHash = anim.GetCurrentAnimatorStateInfo(0).fullPathHash;
                anim.Play(stateHash, -1, 0f);
                Debug.Log($"[AutoCatAnimator] {gameObject.name} played Animator state.");
            }
            else
            {
                // 如果没有挂载 Controller, 尝试暴力拿所有自带的 AnimationClip，用 Playables API 播放它是黑科技，
                // 但为了简单，如果用户没配 Controller，我们尝试去找 legacyAnim
                Debug.LogWarning($"[AutoCatAnimator] ⚠️ {gameObject.name} 缺少 Animator Controller! 请在 Unity 里点右键 Create->Animator Controller，然后配上它的动画并拖给这个组件！");
            }
        }
        
        if (legacyAnim != null)
        {
            legacyAnim.Stop();
            legacyAnim.Play();
            Debug.Log($"[AutoCatAnimator] {gameObject.name} played Legacy Animation clip.");
        }
    }
}

/// <summary>
/// 猫咪触摸感应器：分别响应不同设定的猫咪（暴躁凶神、呼噜动画）
/// </summary>
public class CatTouchReceiver : MonoBehaviour
{
    public enum CatRole { Aggressive, Purr }
    public CatRole catRole;
    
    public AudioSource audioSource;
    private float lastTouchTime = -999f;
    private const float Cooldown = 3.0f; // 防止连叫

    void OnTriggerEnter(Collider other)
    {
        // 忽略自身
        if (other.transform.IsChildOf(transform.root)) return; 
        
        if (Time.time - lastTouchTime < Cooldown) return;

        // 简易判断：避免报错，直接用名字检测
        string n = other.name.ToLower();
        if (other.CompareTag("Player") || other.CompareTag("MainCamera") || 
            n.Contains("hand") || n.Contains("controller") || n.Contains("player") || n.Contains("xr") || n.Contains("vr"))
        {
            TriggerCat();
        }
    }

    void Update()
    {
        // ================= 终极雷达检测（遍历所有摄像机防止找错对象） =================
        bool playerNearby = false;
        
        // 我们遍历当前世界上正在渲染的“所有”摄像机（XR双眼相机、主相机）
        foreach (Camera c in Camera.allCameras)
        {
            Vector2 catPlane = new Vector2(transform.position.x, transform.position.z);
            Vector2 camPlane = new Vector2(c.transform.position.x, c.transform.position.z);
            
            // XR头显因为安全边界和 Offset 的问题，它的坐标可能不是真实脑袋，我们把雷达开到 3.0 米死区！
            if (Vector2.Distance(catPlane, camPlane) < 3.0f)
            {
                playerNearby = true;
                break;
            }
        }

        // 神级后门：直接按键盘上的 E 键，强制触发互动！
        bool forceInteract = Input.GetKeyDown(KeyCode.E);

        if (Time.time - lastTouchTime >= Cooldown && (playerNearby || forceInteract))
        {
            Debug.Log($"[CatInteraction] Player interacted with {gameObject.name}! (Plane Dist or E Key)");
            TriggerCat();
        }
    }

    [ContextMenu(">>> CLICK ME: FORCE TRIGGER INTERACTION <<<")]
    private void TriggerCat()
    {
        lastTouchTime = Time.time;
        
        Debug.Log($"[CatInteraction] Player interacted with {catRole} cat!");

        // 【终极瞎子可见测试】如果你没挂载声音，或者没有控制器，你根本不知道交互触发了没有！
        // 我给你砸出一个巨大的半透明红球，停留 3 秒，只要大红球出现了，就说明系统代码全部顺畅触发成功！！
        GameObject debugIndicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        debugIndicator.transform.position = transform.position + Vector3.up * 1.5f; // 飘在猫头上
        debugIndicator.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f); // 绝对够大的警告球
        Destroy(debugIndicator.GetComponent<Collider>());
        
        Material redMat = new Material(Shader.Find("Standard"));
        redMat.color = Color.red;
        redMat.EnableKeyword("_EMISSION");
        redMat.SetColor("_EmissionColor", Color.red * 2f);
        debugIndicator.GetComponent<Renderer>().material = redMat;
        Destroy(debugIndicator, 3.0f); // 3秒后销毁

        // 1. 发出对应的声音（如果你加了的话）
        if (audioSource != null && audioSource.clip != null)
        {
            audioSource.PlayOneShot(audioSource.clip);
        }
        else
        {
            Debug.LogWarning($"[Placeholder] No AudioClip attached for Cat ({catRole})!");
        }

        // 2. 如果是呼噜卡通猫，互动后还要配合播放动画！
        if (catRole == CatRole.Purr)
        {
            Animator anim = transform.root.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                if (anim.runtimeAnimatorController != null)
                {
                    int stateHash = anim.GetCurrentAnimatorStateInfo(0).fullPathHash;
                    anim.Play(stateHash, 0, 0f);
                    Debug.Log("[CatInteraction] Toon Cat played purring animation!");
                }
            }
            else
            {
                Animation legacyAnim = transform.root.GetComponentInChildren<Animation>();
                if (legacyAnim != null) legacyAnim.Play();
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
        containerPs = GetComponent<ParticleSystem>(); 
        
        // 查找世界里的特效名字（因为我们把它脱离父级防止了变态缩放）
        GameObject sparks = GameObject.Find("FireSparksParticles");
        if (sparks != null) sparksPs = sparks.GetComponent<ParticleSystem>();
    }

    void Update()
    {
        if (isIgnited) return;

        bool playerNearby = false;
        foreach (Camera c in Camera.allCameras)
        {
            Vector2 firePlane = new Vector2(transform.position.x, transform.position.z);
            Vector2 camPlane = new Vector2(c.transform.position.x, c.transform.position.z);
            
            // XR 空间放宽到恐怖的 4 米半径雷达！只要你在房间里基本上必触发。
            if (Vector2.Distance(firePlane, camPlane) < 4.0f) 
            {
                playerNearby = true;
                break;
            }
        }

        bool forceInteract = Input.GetKeyDown(KeyCode.E);

        if (playerNearby || forceInteract)
        {
            Debug.Log("[CampfireInteraction] Player body or E Key triggered the campfire!");
            IgniteFireplace();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // 忽略自身
        if (other.transform.IsChildOf(transform.root)) return; 
        
        if (Time.time - lastTouchTime < Cooldown) return;

        // 判断是否是玩家手柄
        string n = other.name.ToLower();
        if (other.CompareTag("Player") || other.CompareTag("MainCamera") || 
            n.Contains("hand") || n.Contains("controller") || n.Contains("player") || n.Contains("xr") || n.Contains("vr"))
        {
            lastTouchTime = Time.time;
            IgniteFireplace();
        }
    }

    [ContextMenu(">>> CLICK ME: FORCE IGNITE FIREPLACE <<<")]
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
                noise.strength = 1.0f; // 更加剧烈的扭曲幅度（火从下往上涌）
                noise.frequency = 1.5f; // 更加剧烈的高频扭曲
                noise.scrollSpeed = 2.5f; // 极速火焰向上升腾的滚动感

                // 赋予生命周期内向上飘升的巨量热气流速度！！！
                var vel = containerPs.velocityOverLifetime;
                vel.enabled = true;
                vel.y = new ParticleSystem.MinMaxCurve(2.0f, 4.5f); // 极其暴躁地从下往上涌出
                vel.z = new ParticleSystem.MinMaxCurve(-0.5f, 0.5f); // 微微向外扩散
                vel.x = new ParticleSystem.MinMaxCurve(-0.5f, 0.5f); 
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
