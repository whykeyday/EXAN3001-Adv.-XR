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

    [Header("=== Campfire Tuning ========= ")]
    [Tooltip("火星速度 (默认 0.2f 慢飘)")]
    public float fireSpeed = 0.4f;
    [Tooltip("火星寿命 (默认 1.0f 限制飘飞高度)")]
    public float fireLifetime = 1.0f;
    [Tooltip("火星重力 (默认 -0.05f 极其轻微向上)")]
    public float fireGravity = -0.02f;
    [Tooltip("火星大小 (极小火苗 0.05f)")]
    public float fireSize = 0.05f;
    [Tooltip("火星出生位置的绝对偏移 (如果火是从土里冒出来的，把 Y 调高)")]
    public Vector3 fireOffset = new Vector3(0f, -0.1f, 0f);
    [Tooltip("火星数量密度 (默认 50，想要火变多这改到 150)")]
    public float fireRate = 50f;
    [Tooltip("火星散布底部半径 (想要底部火再粗点改到 0.2f)")]
    public float fireRadius = 0.05f;

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
        // 强制把特效生成在真实的网格几何中心附近，并加上用户自定的微调偏移
        sparks.transform.position = trueCenter + fireOffset;
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
        
        main.startSize = new ParticleSystem.MinMaxCurve(fireSize, fireSize * 1.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(fireSpeed * 0.5f, fireSpeed);
        main.startLifetime = new ParticleSystem.MinMaxCurve(fireLifetime * 0.5f, fireLifetime);
        main.gravityModifier = fireGravity; 

        var emission = ps.emission;
        emission.rateOverTime = 0f; 

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone; 
        shape.angle = 15f; // 张角调大一点显得火旺
        shape.radius = fireRadius; 
        
        // 确保它绝对向上且不摇摆
        shape.rotation = new Vector3(-90f, 0f, 0f); 
        var vel = ps.velocityOverLifetime;
        vel.enabled = false; 
        
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
            EnsureColliderAndRigidBody(blackCatModel);
            CatTouchReceiver rec = blackCatModel.gameObject.AddComponent<CatTouchReceiver>();
            rec.catRole = CatTouchReceiver.CatRole.Aggressive;
            if (blackCatAggrAudio != null) rec.audioSource = CreateAudioSource(blackCatModel, blackCatAggrAudio);
            AttachForwarders(blackCatModel, rec);
        }

        // 2. 灵魂疑犯猫 (死循环脚本)
        if (murderedCatModel != null)
        {
            EnsureColliderAndRigidBody(murderedCatModel);
            murderedCatModel.gameObject.AddComponent<SofaCatForeverLooper>();
            
            CatTouchReceiver rec = murderedCatModel.gameObject.AddComponent<CatTouchReceiver>();
            rec.catRole = CatTouchReceiver.CatRole.Purr;
            AttachForwarders(murderedCatModel, rec);
        }

        // 3. 卡通猫
        if (toonCatModel != null)
        {
            EnsureColliderAndRigidBody(toonCatModel);
            CatTouchReceiver rec = toonCatModel.gameObject.AddComponent<CatTouchReceiver>();
            rec.catRole = CatTouchReceiver.CatRole.Purr;
            if (toonCatPurrAudio != null) rec.audioSource = CreateAudioSource(toonCatModel, toonCatPurrAudio);
            AttachForwarders(toonCatModel, rec);
        }
    }

    // 核心桥接技术：把子层网格身上的碰撞信号，快递给老爹身上的接收器！
    void AttachForwarders(Transform root, CatTouchReceiver receiver)
    {
        Collider[] cols = root.GetComponentsInChildren<Collider>();
        foreach (Collider c in cols)
        {
            if (c.gameObject != root.gameObject)
            {
                CatTouchForwarder fwd = c.gameObject.AddComponent<CatTouchForwarder>();
                fwd.target = receiver;
            }
        }
    }

    AudioSource CreateAudioSource(Transform parent, AudioClip clip)
    {
        // 【终极声音修复】：模型原点 Pivot 偏离百米不仅会导致红球偏离，也会导致 3D 喇叭偏离！
        // 如果喇叭在百米开外，由于衰减距离只有 20 米，就算音量最大玩家也听不见！
        // 因此我们要专门建一个独立的小喇叭节点，并把它强制塞进真实的网格中央！
        GameObject audioEmitter = new GameObject("AudioEmitter_PerfectCenter");
        audioEmitter.transform.SetParent(parent, false);

        CatTouchReceiver rec = parent.GetComponent<CatTouchReceiver>();
        if (rec != null) audioEmitter.transform.position = rec.GetTrueCenter();
        else audioEmitter.transform.position = parent.position;

        AudioSource src = audioEmitter.AddComponent<AudioSource>();
        src.clip = clip;
        // 保证音量穿透力：别设为完全 1.0 的纯 3D，保留 0.75 使得全屏都能隐约听到
        src.spatialBlend = 0.75f; 
        src.volume = 1f;
        src.minDistance = 2f;  // 贴脸 2 米内都是最大音量
        src.maxDistance = 20f; // 20 米外才完全消失
        src.rolloffMode = AudioRolloffMode.Linear; // 线性衰减更符合直观
        src.playOnAwake = false;
        return src;
    }

    void EnsureColliderAndRigidBody(Transform target)
    {
        if (target == null) return;
        
        // 【逆转思路：全面尊重玩家手工配置】
        // 原生 FBX 动画骨骼会导致通过脚本读取的 Bounds 严重飞偏移，所以绝不能去硬算！
        // 既然你之前【自己手动加过 Collider】，说明你精心调装过大小。
        // 我们不该清场删掉它！我们只需要把它从一堵“挡路的死墙”，变成“能穿透的手电筒（触发器）”！
        Collider[] allCols = target.GetComponentsInChildren<Collider>();
        if (allCols.Length > 0)
        {
            foreach(var c in allCols) 
            {
                c.isTrigger = true; // 把所有手动添加的挡路碰撞体全部软化成触发器
            }
        }
        else
        {
            // 如果玩家真的完全没加，再给个默认空气球
            SphereCollider sc = target.gameObject.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            float maxScale = Mathf.Max(target.lossyScale.x, Mathf.Max(target.lossyScale.y, target.lossyScale.z));
            sc.radius = 0.5f / (maxScale == 0 ? 1f : maxScale);
        }

        // 彻底删除会导致 XR手柄（Kinematic）因物理矩阵而相互穿透不报信的 Rigidbody！
        Rigidbody rb = target.GetComponent<Rigidbody>();
        if (rb != null) Destroy(rb);
    }

    public static bool IsValidPlayer(Collider other, Transform self)
    {
        // 注意！绝对不能用 self.root，很多玩家喜欢把整个场景全放进一家名叫 environment 的根文件夹！
        // 如果用 root 判定，意味着整个屋里的东西全成了你的孩子，判定直接报废！
        // 我们只排斥明确属于“它自身这块零配件”碰撞体的自己人！
        if (other.transform.IsChildOf(self)) return false;

        string n = other.name.ToLower();
        // 绝对黑名单：排除所有可能一直贴在一起的环境建筑
        if (n.Contains("ground") || n.Contains("floor") || n.Contains("sofa") || 
            n.Contains("room") || n.Contains("plane") || n.Contains("placeholder") ||
            n.Contains("terrain") || n.Contains("wall") || n.Contains("fire") || n.Contains("cat"))
        {
            return false;
        }

        // 白名单：无限制深度往上扒祖宗，寻找 VR 控制器的常见特征字眼
        Transform curr = other.transform;
        while (curr != null)
        {
            string cn = curr.name.ToLower();
            if (cn.Contains("hand") || cn.Contains("controller") || cn.Contains("interact") || 
                cn.Contains("xr") || cn.Contains("left") || cn.Contains("right") || 
                cn.Contains("player") || cn.Contains("camera"))
            {
                return true; // 确认是玩家手柄
            }
            curr = curr.parent;
        }

        // 宁可杀错绝不放过：如果在祖宗结构里找不到任何明显代表“玩家/VR设备”的关键词标记，
        // 我们坚决不认为这是一个合法的手！这能 100% 杜绝阿猫阿狗的场景白模（如名为 Cube 的地板）无限触发互动！
        return false; 
    }
}

public class CatTouchForwarder : MonoBehaviour
{
    public CatTouchReceiver target;
    void OnTriggerEnter(Collider other) { if (target != null) target.OnTriggerEnter(other); }
    void OnTriggerStay(Collider other)  { if (target != null) target.OnTriggerStay(other); }
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
    private float baseAnimSpeed = 1.0f; // 记忆猫猫本来的播放速度

    private Renderer statusIndicator; // 状态指示灯

    // 获取真实的网格中心，无视模型原点的偏离
    public Vector3 GetTrueCenter()
    {
        // 如果有玩家手动加的碰撞体（无论盒子还是胶囊），它的中心绝对是最完美的！
        Collider col = GetComponentInChildren<Collider>();
        if (col != null) return col.bounds.center;

        // 兜底：如果完全没有碰撞体，再用极其容易被骨骼污染的 Mesh 暴力算
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
        bool forceInteract = Input.GetKeyDown(KeyCode.E);
        if (forceInteract && Time.time - lastTouchTime >= Cooldown)
        {
            TriggerCat();
        }

        // --- 状态灯反馈：互动中变红，平时隐形 ---
        if (statusIndicator != null)
        {
            if (Time.time - lastTouchTime < Cooldown) 
            {
                statusIndicator.enabled = true;
                statusIndicator.material.SetColor("_EmissionColor", Color.red * 2f);
                statusIndicator.material.color = Color.red; 
            }
            else 
            {
                statusIndicator.enabled = false; // 平时不触发灯光就不亮
            }
        }
    }

    // 这两个方法必须是 public，因为我们的 CatTouchForwarder 信号快递员在外层（甚至子物体上）要调用它们！
    public void OnTriggerEnter(Collider other)
    {
        if (!CatSceneSetup.IsValidPlayer(other, transform)) return;
        if (Time.time - lastTouchTime < Cooldown) return;
        TriggerCat();
    }

    public void OnTriggerStay(Collider other)
    {
        if (!CatSceneSetup.IsValidPlayer(other, transform)) return;
        if (Time.time - lastTouchTime < Cooldown) return;
        TriggerCat();
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

        // 【终极防漏判定测试：爆炸的紫色小球！】
        // 只要这个代码进来了，不管猫有没有绑定动画，必然会从中心弹出一个存在 2 秒的紫色小球！
        GameObject burst = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        burst.transform.position = GetTrueCenter() + Vector3.up * 0.7f;
        burst.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
        burst.GetComponent<Renderer>().material.color = Color.magenta;
        Destroy(burst.GetComponent<Collider>());
        Destroy(burst, 2.0f); // 2秒后自动消失

        if (catRole == CatRole.Purr || catRole == CatRole.Aggressive)
        {
            // --- 核心视觉反馈：抛开坑人的骨骼动画器，强制执行完美的身体闪红！---
            if (flashCoroutine != null) StopCoroutine(flashCoroutine);
            flashCoroutine = StartCoroutine(CatFlashRoutine());

            Animator anim = GetComponentInChildren<Animator>();
            if (anim == null && transform.parent != null) anim = transform.parent.GetComponentInChildren<Animator>();

            if (anim != null)
            {
                if (anim.runtimeAnimatorController == null)
                {
                    Debug.LogWarning("[CatInteraction] 猫无 Animator Controller，只展示物理弹跳与红球反馈！");
                }
                else
                {
                    // 记忆原始速度，给动画提速到 2.0 倍持续两秒！
                    baseAnimSpeed = anim.speed;
                    anim.speed = 2.0f;
                    Invoke("ResetAnimSpeed", 2.0f);
                    anim.Play(0, -1, 0f); // 重播当前动画
                }
            }
        }
    }

    private Coroutine flashCoroutine;

    private System.Collections.IEnumerator CatFlashRoutine()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        System.Collections.Generic.Dictionary<Renderer, Color> origColors = new System.Collections.Generic.Dictionary<Renderer, Color>();
        
        // 记录原始颜色
        foreach (var r in renderers) 
        {
            if (r.material.HasProperty("_Color")) 
                origColors[r] = r.material.color;
        }

        float elapsed = 0f;
        float duration = 0.5f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // 材质颜色从红色平滑褪回原始颜色
            foreach (var r in renderers) 
            {
                if (origColors.ContainsKey(r))
                {
                    r.material.color = Color.Lerp(Color.red, origColors[r], t);
                }
            }
            yield return null;
        }

        // 确保完美复原
        foreach (var r in renderers) 
        {
            if (origColors.ContainsKey(r)) r.material.color = origColors[r];
        }
    }

    void ResetAnimSpeed()
    {
        Animator anim = GetComponentInChildren<Animator>();
        if (anim == null && transform.parent != null) anim = transform.parent.GetComponentInChildren<Animator>();
        if (anim != null) anim.speed = baseAnimSpeed;
    }
}

/// <summary>
/// 专为沙发猫设计的“死不悔改循环脚本”
/// </summary>
public class SofaCatForeverLooper : MonoBehaviour
{
    private Animator anim;
    void Start() 
    { 
        anim = GetComponentInChildren<Animator>(); 
        if (anim != null) anim.speed = 0.2f; // 你要求的放慢 5 倍 (1/5 的速度)
    }
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
    
    // 我们从父类获取最大爆发数量
    private float maxFireRate = 50f;

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
        
        CatSceneSetup setup = FindObjectOfType<CatSceneSetup>();
        if (setup != null) maxFireRate = setup.fireRate;

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

        // 起步时强行把主火焰粒子发射率设为 0，彻底浇灭之前的“白色幽灵火”！
        if (containerPs != null)
        {
            var em = containerPs.emission;
            em.rateOverTime = 0f;
        }

        if (fireplaceAudio != null) 
        {
            fireplaceAudio.spatialBlend = 0.8f;
            fireplaceAudio.minDistance = 1.5f;
            fireplaceAudio.maxDistance = 15f;
            fireplaceAudio.rolloffMode = AudioRolloffMode.Linear;
            fireplaceAudio.loop = true;
            fireplaceAudio.Play();
            fireplaceAudio.volume = 0f; // 初始静音，靠靠近变大
        }
    }

    void Update()
    {
        // 如果距离上次触摸在 2 秒内，火势就是 100%；否则火势渐渐熄灭为 0%。
        float targetIntensity = (Time.time - lastTouchTime < 2.0f) ? 1.0f : 0f;
        
        currentFireIntensity = Mathf.Lerp(currentFireIntensity, targetIntensity, Time.deltaTime * 3.5f);

        // 1. 无缝控火星
        if (sparksPs != null)
        {
            var em = sparksPs.emission;
            em.rateOverTime = currentFireIntensity * maxFireRate; 
        }

        // 2. 彻底控主火苗：不摸就不喷！一摸猛喷！
        if (containerPs != null)
        {
            var em = containerPs.emission;
            em.rateOverTime = currentFireIntensity * 35f; // 从 0 直接喷射到 35 颗/秒

            var main = containerPs.main;
            Color baseColor = new Color(1.0f, 0.6f, 0.2f, 0.5f); // 微弱预热橘色
            Color fireColor = new Color(1f, 0.2f, 0f, 1f); // 炽热大火红
            main.startColor = Color.Lerp(baseColor, fireColor, currentFireIntensity);

            var noise = containerPs.noise;
            noise.enabled = true;
            noise.strength = Mathf.Lerp(0f, 1.5f, currentFireIntensity); 
        }

        // 3. 声音由远及近
        if (fireplaceAudio != null)
        {
            fireplaceAudio.volume = currentFireIntensity * 1.5f; // 确保音量足够大
        }

        // 4. 灯光反馈
        if (statusIndicator != null)
        {
            if (targetIntensity > 0)
            {
                statusIndicator.enabled = true;
                statusIndicator.material.SetColor("_EmissionColor", Color.red * 2f);
                statusIndicator.material.color = Color.red;
            }
            else
            {
                statusIndicator.enabled = false;
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!CatSceneSetup.IsValidPlayer(other, transform)) return;

        // 如果火是熄灭的，给它一个爆炸火花效果！
        if (Time.time - lastTouchTime > 2.0f && sparksPs != null) sparksPs.Emit(40);

        currentFireIntensity = 1.0f;
        lastTouchTime = Time.time;
    }

    void OnTriggerStay(Collider other)
    {
        if (!CatSceneSetup.IsValidPlayer(other, transform)) return;
        currentFireIntensity = 1.0f;
        lastTouchTime = Time.time;
    }

    [ContextMenu(">>> CLICK ME: FORCE IGNITE FIREPLACE <<<")]
    void IgniteFireplace()
    {
        currentFireIntensity = 1.0f;
    }
}
