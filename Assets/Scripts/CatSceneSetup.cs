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
        
        // 获取视觉中心（破除原点偏移）
        Renderer[] rs = sparkParent.GetComponentsInChildren<Renderer>();
        Vector3 trueCenter = sparkParent.position;
        if (rs.Length > 0)
        {
            Bounds b = rs[0].bounds;
            foreach (Renderer r in rs) b.Encapsulate(r.bounds);
            trueCenter = b.center;
        }

        // 创建壁炉火星特效 
        GameObject sparks = new GameObject("FireSparksParticles");
        sparks.transform.SetParent(sparkParent, false); 
        // 强制把特效生成在真实的网格几何中心附近
        sparks.transform.position = trueCenter - Vector3.up * 0.2f;
        sparks.transform.localScale = Vector3.one; // 强行拉回 1:1

        ParticleSystem ps = sparks.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.scalingMode = ParticleSystemScalingMode.Shape; 
        main.loop = true; // 恢复常态连绵涌动！
        main.playOnAwake = true;
        
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.6f, 0.1f, 0.8f), 
            new Color(1f, 0.2f, 0.0f, 0.8f) 
        );
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        // 【关键】将粒子调得非常细小，像真实的火苗而不是大光球！
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.08f);
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 2.0f);
        main.gravityModifier = -0.1f; 

        var emission = ps.emission;
        emission.rateOverTime = 0f; // 初始为 0，靠脚本里的“靠近”来慢慢推高

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone; // 改为锥形，像真实的火舌一样往上涌！
        shape.angle = 15f; 
        shape.radius = 0.1f; 

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.y = new ParticleSystem.MinMaxCurve(1.5f, 3.5f); // 向上涌动
        vel.x = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f); // 微微摇摆
        
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
        // 取消了“场景/文件夹”防呆验证，防止在 VR 真机 Build 里被误判拦截导致脚本挂不上！

        // 1. 黑猫 (遇人发出凶狠叫声)
        if (blackCatModel != null)
        {
            EnsureColliderAndRigidBody(blackCatModel, new Vector3(0.6f, 0.6f, 0.6f));
            CatTouchReceiver rec = blackCatModel.gameObject.AddComponent<CatTouchReceiver>();
            rec.catRole = CatTouchReceiver.CatRole.Aggressive;
            if (blackCatAggrAudio != null) rec.audioSource = CreateAudioSource(blackCatModel, blackCatAggrAudio);
        }

        // 2. 灵魂疑犯猫 (暴力死循环脚本注入，保证它至死不渝地动下去)
        if (murderedCatModel != null)
        {
            murderedCatModel.gameObject.AddComponent<SofaCatForeverLooper>();
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
        if (target == null) return;
        
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

    private Renderer statusIndicator; // 状态指示灯

    // 获取真实的网格中心，无视模型原点的偏离
    public Vector3 GetTrueCenter()
    {
        Renderer[] rs = GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return transform.position;
        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        return b.center;
    }

    void Start()
    {
        GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
        indicator.name = "StatusLight_" + gameObject.name;
        // 把指示灯挂在网格真正的重心偏上
        indicator.transform.position = GetTrueCenter() + Vector3.up * 0.8f;
        indicator.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);
        Destroy(indicator.GetComponent<Collider>());
        indicator.transform.SetParent(transform, true);
        
        statusIndicator = indicator.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = Color.blue; 
        mat.EnableKeyword("_EMISSION");
        statusIndicator.material = mat;
    }

    void Update()
    {
        bool playerNearby = false;
        Vector3 center = GetTrueCenter();

        // 使用 Camera.main 作为更稳妥的 VR 头显位置（如果 Camera.allCameras 获取的有问题）
        Camera playerCam = Camera.main;
        if (playerCam != null)
        {
            Vector2 catPlane = new Vector2(center.x, center.z);
            Vector2 camPlane = new Vector2(playerCam.transform.position.x, playerCam.transform.position.z);
            if (Vector2.Distance(catPlane, camPlane) < 4.0f) playerNearby = true;
        }

        bool forceInteract = Input.GetKeyDown(KeyCode.E);

        if (Time.time - lastTouchTime >= Cooldown && (playerNearby || forceInteract))
        {
            TriggerCat();
        }

        // --- 状态灯反馈：有人靠近变绿，互动中变红，待命蓝 ---
        if (statusIndicator != null)
        {
            if (Time.time - lastTouchTime < Cooldown) 
            {
                statusIndicator.material.SetColor("_EmissionColor", Color.red * 2f);
                statusIndicator.material.color = Color.red; 
            }
            else if (playerNearby) 
            {
                // 如果灯变绿了，说明【雷达测定你已经走到它旁边了】！
                // 这可以直接诊断距离判定代码是否在干活！
                statusIndicator.material.SetColor("_EmissionColor", Color.green * 2f);
                statusIndicator.material.color = Color.green;
            }
            else 
            {
                statusIndicator.material.SetColor("_EmissionColor", Color.blue * 1f);
                statusIndicator.material.color = Color.blue;
            }
        }
    }

    [ContextMenu(">>> CLICK ME: FORCE TRIGGER INTERACTION <<<")]
    private void TriggerCat()
    {
        lastTouchTime = Time.time;
        if (statusIndicator != null) statusIndicator.material.color = Color.red;
        
        Debug.Log($"[CatInteraction] Player interacted with {catRole} cat!");

        // 【终极可见测试】砸出巨大红球
        GameObject debugIndicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        debugIndicator.transform.position = transform.position + Vector3.up * 1.5f; 
        debugIndicator.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f); 
        Destroy(debugIndicator.GetComponent<Collider>());
        Material redMat = new Material(Shader.Find("Standard"));
        redMat.color = Color.red;
        debugIndicator.GetComponent<Renderer>().material = redMat;
        Destroy(debugIndicator, 2.0f); 

        // 播声音、播动画
        if (audioSource != null && audioSource.clip != null) audioSource.PlayOneShot(audioSource.clip);

        if (catRole == CatRole.Purr)
        {
            Animator anim = transform.root.GetComponentInChildren<Animator>();
            if (anim != null && anim.runtimeAnimatorController != null)
            {
                anim.Play(0, -1, 0f);
            }
        }
    }
}

/// <summary>
/// 专为沙发猫设计的“死不悔改循环脚本”
/// </summary>
public class SofaCatForeverLooper : MonoBehaviour
{
    private Animator anim;
    void Start() { anim = GetComponentInChildren<Animator>(); }
    void Update()
    {
        if (anim != null && anim.runtimeAnimatorController != null)
        {
            // 如果动画播到了结尾或者没在动，暴力重启！
            var state = anim.GetCurrentAnimatorStateInfo(0);
            if (state.normalizedTime >= 0.99f || !anim.enabled)
            {
                anim.Play(state.fullPathHash, 0, 0f);
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
    private ParticleSystem containerPs; 
    private ParticleSystem sparksPs; 
    
    // 平滑线性燃烧强度
    private float currentFireIntensity = 0f;

    private Renderer statusIndicator; 

    public Vector3 GetTrueCenter()
    {
        Renderer[] rs = GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return transform.position;
        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        return b.center;
    }

    void Start()
    {
        containerPs = GetComponent<ParticleSystem>(); 
        Transform sparks = transform.Find("FireSparksParticles");
        if (sparks != null) sparksPs = sparks.GetComponent<ParticleSystem>();

        GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
        indicator.name = "StatusLight_Campfire";
        indicator.transform.position = GetTrueCenter() + Vector3.up * 1.5f;
        indicator.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
        Destroy(indicator.GetComponent<Collider>());
        indicator.transform.SetParent(transform, true);
        
        statusIndicator = indicator.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = Color.blue;
        mat.EnableKeyword("_EMISSION");
        statusIndicator.material = mat;
        if (fireplaceAudio != null) 
        {
            fireplaceAudio.loop = true;
            fireplaceAudio.Play();
            fireplaceAudio.volume = 0f; // 初始静音，靠靠近变大
        }
    }

    void Update()
    {
        Vector3 center = GetTrueCenter();
        Camera playerCam = Camera.main;
        
        float dist = 10f; // 默认很远
        if (playerCam != null)
        {
            Vector2 firePlane = new Vector2(center.x, center.z);
            Vector2 camPlane = new Vector2(playerCam.transform.position.x, playerCam.transform.position.z);
            dist = Vector2.Distance(firePlane, camPlane);
        }

        // === 【核心重构：连绵涌动 与 渐隐熄灭】 ===
        // 在 2 米处火势达到 100% (最猛)；退到 6 米外火势减弱到 0%。
        float targetIntensity = Mathf.Clamp01(1.0f - (dist - 2.0f) / 4.0f);
        
        // 我们利用插值让火星增减变得像呼吸一样自然平滑
        currentFireIntensity = Mathf.Lerp(currentFireIntensity, targetIntensity, Time.deltaTime * 2.5f);

        // 1. 无缝控火星：越靠近，喷射越疯狂 (最高 50颗粒/秒)
        if (sparksPs != null)
        {
            var em = sparksPs.emission;
            em.rateOverTime = currentFireIntensity * 50f; 
        }

        // 2. 无缝控颜色和底座：火苗由暗淡白逐渐烧红
        if (containerPs != null)
        {
            var main = containerPs.main;
            Color baseColor = new Color(1f, 1f, 1f, 0.4f); // 幽灵白
            Color fireColor = new Color(1f, 0.4f, 0f, 1f); // 炽热红
            main.startColor = Color.Lerp(baseColor, fireColor, currentFireIntensity);

            var noise = containerPs.noise;
            noise.enabled = true;
            // 越近底部闪动越剧烈
            noise.strength = Mathf.Lerp(0f, 1.5f, currentFireIntensity); 
        }

        // 3. 声音由远及近
        if (fireplaceAudio != null)
        {
            fireplaceAudio.volume = currentFireIntensity;
        }

        // 4. 灯光反馈
        if (statusIndicator != null)
        {
            statusIndicator.material.SetColor("_EmissionColor", Color.Lerp(Color.blue, Color.red, currentFireIntensity) * 2f);
            statusIndicator.material.color = Color.Lerp(Color.blue, Color.red, currentFireIntensity);
        }
    }

    // 依然响应纯物理触手抚摸 (按满强度瞬爆)
    void OnTriggerStay(Collider other)
    {
        if (other.transform.IsChildOf(transform.root)) return; 
        currentFireIntensity = 1.0f; // 只要手放进去，火烧最旺！
    }

    [ContextMenu(">>> CLICK ME: FORCE IGNITE FIREPLACE <<<")]
    void IgniteFireplace()
    {
        currentFireIntensity = 1.0f;
    }
}
