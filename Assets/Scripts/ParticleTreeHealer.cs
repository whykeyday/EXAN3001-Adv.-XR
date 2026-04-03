using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 【全新重写】纯粒子驱动的树木治愈系统。
/// 完全弃用实体 MeshRenderer，依靠代码控制活树/枯树两套独立粒子系统，
/// 并且支持根据高度动态将同一棵树的粒子染色为树干（棕色）和树叶（绿色）。
/// </summary>
public class ParticleTreeHealer : MonoBehaviour
{
    [Header("====== 核心网格引用 ======")]
    [Tooltip("枯树模型（用于生成枯树粒子）")]
    public Mesh witheredMesh;
    [Tooltip("活树模型（用于生成绿色树冠和活树干粒子）")]
    public Mesh aliveMesh;
    public Vector3 aliveMeshPositionOffset = Vector3.zero;
    public Vector3 aliveMeshRotationOffset = new Vector3(-90, 0, 0);
    public float aliveMeshScaleMultiplier = 0.35f; // 【再一次缩小】让绿树尽量能套进枯树主干里

    [Header("====== 视觉染色与特效 ======")]
    [Tooltip("枯树状态的粒子颜色 (棕色/暗琥珀色)")]
    public Color witheredColor = new Color(0.4f, 0.2f, 0.05f);
    
    [Tooltip("活树树干的粒子颜色 (亮棕色)")]
    public Color aliveTrunkColor = new Color(0.45f, 0.25f, 0.1f);
    
    [Tooltip("活树树叶的粒子颜色 (翠绿色)")]
    public Color aliveLeafColor = new Color(0.15f, 0.8f, 0.25f);

    [Tooltip("树顶落花粒子的颜色 (粉色)")]
    public Color pinkPetalColor = new Color(1f, 0.6f, 0.8f, 0.9f);
    
    [Tooltip("高度阈值：判断 aliveMesh 的哪些顶点应该染成棕色树干，哪些染成绿色树叶。在局部坐标系下衡量。")]
    public float trunkHeightThreshold = 15f; 
    
    [Tooltip("树冠近似高度（用于确定丝巾、粉色花瓣、蝴蝶的生成位置）")]
    public float canopyMaxHeight = 30f;

    [Header("====== 粒子密度与数量设置 (任意调整尝试) ======")]
    [Tooltip("枯树的最大生成速率（决定多密集）")]
    public float witheredParticleRate = 5000f; // ★ 提升密度默认值
    [Tooltip("绿树的最大生成速率（决定多密集）")]
    public float aliveParticleRate = 12000f; // ★ 提升密度默认值
    [Tooltip("单一阶段允许同时存活的最大粒子数量极限")]
    public int maxParticleLimit = 15000;

    [Header("====== 治愈与自然衰退进度 ======")]
    [Tooltip("延迟衰退的时间：离开手后等多久才开始掉色变小（默认60秒）")]
    public float healLingerDuration = 60f;
    private float healLingerTimer = 0f;

    [Range(0.01f, 1f)] public float healingRate = 0.05f;
    [Range(0.01f, 1f)] public float decayRate = 0.02f;
    [Range(0, 1)] public float energyLevel = 0f;

    [Header("====== 音效与贴图 ======")]
    [Tooltip("鸟叫声音频文件（完全治愈时播放）")]
    public AudioClip birdAudioClip;
    [Tooltip("触碰树的魔法治愈音效")]
    public AudioClip magicHealClip;
    [Tooltip("鸟叫距离衰减范围")]
    public float birdFadeDistance = 5f;
    [Tooltip("魔法音效距离衰减范围")]
    public float magicFadeDistance = 3f;

    private AudioSource birdAudio;
    private AudioSource magicHealAudio;

    [Tooltip("蝴蝶动画序列帧，2x2 切图")]
    public Texture2D butterflyTexture;

    // --- 内部粒子系统引用 ---
    private ParticleSystem witheredPS;
    private ParticleSystem alivePS;
    private ParticleSystem petalsPS;
    private ParticleSystem scarfPS;
    private ParticleSystem butterfliesPS;
    private ParticleSystem soilPS;

    // --- 逻辑控制 ---
    private Collider treeCollider;
    [HideInInspector] public bool triggerOverlapDetected = false;
    private bool fullyHealedTriggered = false;
    private bool wasHealing = false;
    private Coroutine birdCoroutine;
    private ParticleSystem.Particle[] pBuffer; // 用于读取和染色粒子的高效缓存
    private float scanTimer = 0f;
    private List<GameObject> cachedHands = new List<GameObject>();
    private float wMinY = 0f;
    private float wMaxY = 10f;
    private float aMinY = 0f;
    private float aMaxY = 10f;

    // 粒子生成速率基准
    private readonly int MAX_WITHERED_RATE = 15000;
    private readonly int MAX_ALIVE_RATE = 15000;

    void Start()
    {
        treeCollider = GetComponent<Collider>();
        pBuffer = new ParticleSystem.Particle[Mathf.Max(MAX_ALIVE_RATE, MAX_WITHERED_RATE) + 2000];
        energyLevel = 0f;

        // 自动创建 AudioSource + 距离衰减
        if (birdAudioClip != null)
        {
            birdAudio = gameObject.AddComponent<AudioSource>();
            birdAudio.clip = birdAudioClip;
            birdAudio.spatialBlend = 1f;
            birdAudio.playOnAwake = false;
            AudioDistanceFader.Setup(birdAudio, birdFadeDistance, 0.8f);
        }
        if (magicHealClip != null)
        {
            magicHealAudio = gameObject.AddComponent<AudioSource>();
            magicHealAudio.clip = magicHealClip;
            magicHealAudio.spatialBlend = 1f;
            magicHealAudio.playOnAwake = false;
            AudioDistanceFader.Setup(magicHealAudio, magicFadeDistance, 0.5f);
        }

        // ★ 强行覆盖 Inspector 中可能残留的旧参数，确保本次更新立即生效！！
        aliveMeshScaleMultiplier = 0.35f;
        witheredParticleRate = Mathf.Max(witheredParticleRate, 5000f);
        aliveParticleRate = Mathf.Max(aliveParticleRate, 12000f);

        // 单独计算完全不同的两套骨架空间的 Y 极值，防止因为比例不同导致扫描线与着色断层！
        if (witheredMesh != null && aliveMesh != null)
        {
            // 通过构建完整的临时渲染器来精确获取物理界限差距，完美解决缩放与 -90 度旋转造成的包围盒扭曲！！
            GameObject tempW = new GameObject("TempW");
            var wFilter = tempW.AddComponent<MeshFilter>();
            wFilter.sharedMesh = witheredMesh;
            var wRender = tempW.AddComponent<MeshRenderer>();
            Bounds wBounds = wRender.bounds;

            GameObject tempA = new GameObject("TempA");
            var aFilter = tempA.AddComponent<MeshFilter>();
            aFilter.sharedMesh = aliveMesh;
            var aRender = tempA.AddComponent<MeshRenderer>();
            tempA.transform.localEulerAngles = aliveMeshRotationOffset;
            tempA.transform.localScale = Vector3.one * aliveMeshScaleMultiplier;
            Bounds aBoundsRaw = aRender.bounds;

            // 存入真实尺度下的最低点差距，强行抵消因为 Scale 0.35 导致的抬升腾空！
            float hoverOffset = wBounds.min.y - aBoundsRaw.min.y;
            // 补偿 5% 的高度防止完全陷入图中
            hoverOffset += (wBounds.max.y - wBounds.min.y) * 0.05f;
            aliveMeshPositionOffset = new Vector3(aliveMeshPositionOffset.x, hoverOffset, aliveMeshPositionOffset.z);

            // 更新真实边界，确保它完美扎根
            tempA.transform.position = new Vector3(0, hoverOffset, 0);
            Bounds aBoundsFinal = aRender.bounds;

            wMinY = wBounds.min.y;
            wMaxY = wBounds.max.y;
            aMinY = aBoundsFinal.min.y;
            aMaxY = aBoundsFinal.max.y;

            Destroy(tempW);
            Destroy(tempA);
        }
        else
        {
            if (witheredMesh != null)
            {
                wMinY = witheredMesh.bounds.min.y;
                wMaxY = witheredMesh.bounds.max.y;
            }
            if (aliveMesh != null)
            {
                aMinY = aliveMesh.bounds.min.y * aliveMeshScaleMultiplier + aliveMeshPositionOffset.y;
                aMaxY = aliveMesh.bounds.max.y * aliveMeshScaleMultiplier + aliveMeshPositionOffset.y;
            }
        }
        
        // 顶部特效（花瓣、蝴蝶）取两个模型中最高的那一个，避免卡在树干里
        canopyMaxHeight = Mathf.Max(wMaxY, aMaxY);
        trunkHeightThreshold = wMinY + (wMaxY - wMinY) * 0.45f;

        // 强行修正 UX 体验数值，避免离开手后瞬间衰退回枯树
        healingRate = 0.4f; // 2.5秒完全治愈
        decayRate = 0.05f;  // 20秒衰退

        // 1. 强力清理旧状态：隐藏所有 MeshRenderer（我们只需要纯粒子！）
        var meshRenderers = GetComponentsInChildren<MeshRenderer>();
        foreach (var r in meshRenderers) r.enabled = false;

        // 安全地清理旧粒子系统：
        // 删除根物体上的旧 ParticleSystem 组件（但不删除 GameObject 本身！）
        ParticleSystem rootPS = GetComponent<ParticleSystem>();
        if (rootPS != null) Destroy(rootPS);

        // 只删除**子物体**上的旧粒子系统
        foreach (Transform child in transform)
        {
            ParticleSystem childPS = child.GetComponent<ParticleSystem>();
            if (childPS != null) Destroy(child.gameObject);
        }

        // 干掉旧 VisualMeshBacking
        Transform oldVisual = transform.Find("VisualMeshBacking");
        if (oldVisual != null) Destroy(oldVisual.gameObject);

        // 2. 重新创建完美的粒子结构
        BuildParticleSystems();
    }

    void BuildParticleSystems()
    {
        Material glowMat = ParticleUtils.GetGlowingSphereMaterial();
        float s = Mathf.Max(transform.lossyScale.x, 1f);

        // ==========================================
        // 1. 枯树粒子系统 (Withered_PS)
        // ==========================================
        GameObject wObj = new GameObject("Withered_PS");
        wObj.transform.SetParent(transform, false);
        witheredPS = wObj.AddComponent<ParticleSystem>();
        var wMain = witheredPS.main;
        wMain.simulationSpace = ParticleSystemSimulationSpace.Local;
        wMain.scalingMode = ParticleSystemScalingMode.Hierarchy; // ★ 让粒子大小跟随父物体缩放
        wMain.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 3.0f); // 自然消散
        wMain.startSpeed = 0f;
        wMain.startSize = new ParticleSystem.MinMaxCurve(0.0002f, 0.0005f); // 尺寸大幅度减小
        wMain.startColor = witheredColor;
        wMain.maxParticles = maxParticleLimit;
        wMain.playOnAwake = true;
        
        var wShape = witheredPS.shape;
        wShape.shapeType = ParticleSystemShapeType.Mesh;
        wShape.meshShapeType = ParticleSystemMeshShapeType.Triangle; 
        wShape.mesh = witheredMesh;
        
        var wCol = witheredPS.colorOverLifetime;
        wCol.enabled = true;
        Gradient wGrad = new Gradient();
        wGrad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.0f, 0.0f),
                new GradientAlphaKey(1.0f, 0.5f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        wCol.color = wGrad;

        var wRender = witheredPS.GetComponent<ParticleSystemRenderer>();
        wRender.renderMode = ParticleSystemRenderMode.Billboard;
        wRender.material = glowMat;
        
        var wEmis = witheredPS.emission;
        wEmis.rateOverTime = 1500; // 持续生成
        wEmis.SetBursts(new ParticleSystem.Burst[0]); 

        // ==========================================
        // 2. 活树粒子系统 (Alive_PS)
        // ==========================================
        GameObject aObj = new GameObject("Alive_PS");
        aObj.transform.SetParent(transform, false);
        alivePS = aObj.AddComponent<ParticleSystem>();
        var aMain = alivePS.main;
        aMain.simulationSpace = ParticleSystemSimulationSpace.Local;
        aMain.scalingMode = ParticleSystemScalingMode.Hierarchy;
        aMain.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 3.0f);
        aMain.startSpeed = 0f;
        aMain.startSize = new ParticleSystem.MinMaxCurve(0.0002f, 0.0005f); // 适中尺寸
        aMain.startColor = Color.white;
        aMain.maxParticles = maxParticleLimit;
        aMain.playOnAwake = true;
        
        var aShape = alivePS.shape;
        aShape.shapeType = ParticleSystemShapeType.Mesh;
        // ★ 必须用 Vertex，确保粒子死死咬住树的每个多边形顶点，不要松散发射
        aShape.meshShapeType = ParticleSystemMeshShapeType.Vertex;
        aShape.mesh = aliveMesh; // 统一只用绿树的模型！
        aShape.position = aliveMeshPositionOffset;
        aShape.rotation = aliveMeshRotationOffset;
        aShape.scale = Vector3.one * aliveMeshScaleMultiplier;

        var aCol = alivePS.colorOverLifetime;
        aCol.enabled = true;
        aCol.color = wGrad;

        var aRender = alivePS.GetComponent<ParticleSystemRenderer>();
        aRender.renderMode = ParticleSystemRenderMode.Billboard;
        aRender.material = glowMat;
        var aEmis = alivePS.emission;
        aEmis.rateOverTime = 0;

        // ==========================================
        // 3. 粉色落花粒子系统 (PinkPetals_PS)
        // ==========================================
        GameObject pObj = new GameObject("PinkPetals_PS");
        pObj.transform.SetParent(transform, false);
        pObj.transform.localPosition = Vector3.zero;
        pObj.transform.localScale = Vector3.one;
        
        petalsPS = pObj.AddComponent<ParticleSystem>();
        var pMain = petalsPS.main;
        pMain.simulationSpace = ParticleSystemSimulationSpace.World;
        pMain.scalingMode = ParticleSystemScalingMode.Hierarchy;
        pMain.startLifetime = new ParticleSystem.MinMaxCurve(2.0f, 4.0f); // ★ 悬浮更久一点
        pMain.startSpeed = 0f; 
        pMain.startSize = 0.0005f; // ★ 绝对微小，绝对不再变大！
        pMain.startColor = pinkPetalColor; 
        pMain.gravityModifier = 0f; // 彻底无重力
        pMain.maxParticles = 5000; 

        var pShape = petalsPS.shape;
        pShape.shapeType = ParticleSystemShapeType.Box; // ★ 改用长方体，完美宽广地覆盖整个树冠层！
        float treeH = aMaxY - aMinY;
        pShape.position = Vector3.up * (aMinY + treeH * 0.95f); // ★ 极其靠上，锁定在顶部 95%！
        pShape.scale = new Vector3(treeH * 1.5f, treeH * 0.1f, treeH * 1.5f); // ★ 极宽极薄的气垫区域，绝对散布全身！

        var pVel = petalsPS.velocityOverLifetime;
        pVel.enabled = true; 
        pVel.x = new ParticleSystem.MinMaxCurve(-0.01f / s, 0.01f / s); 
        pVel.y = new ParticleSystem.MinMaxCurve(-0.002f / s, 0.002f / s); // ★ 真正的微波级定格悬浮！
        pVel.z = new ParticleSystem.MinMaxCurve(-0.01f / s, 0.01f / s);

        var pSizeAnim = petalsPS.sizeOverLifetime;
        pSizeAnim.enabled = false; // ★ 彻底关闭放大效果！解决巨型花瓣的问题！
        
        var pNoise = petalsPS.noise;
        pNoise.enabled = true;
        pNoise.strength = 0.5f; // 随机扭曲飞行路线，增加蓬松空气感
        pNoise.frequency = 0.3f;

        var pRender = petalsPS.GetComponent<ParticleSystemRenderer>();
        pRender.renderMode = ParticleSystemRenderMode.Billboard;
        pRender.material = glowMat;

        var pEmis = petalsPS.emission;
        pEmis.rateOverTime = 0; // 等待愈合后触发

        // ==========================================
        // 4. 黄色环绕丝巾 (YellowScarf_PS)
        // ==========================================
        GameObject scarfObj = new GameObject("YellowScarf_PS");
        scarfObj.transform.SetParent(transform, false);
        scarfObj.transform.localPosition = Vector3.zero;
        scarfObj.transform.localScale = Vector3.one;

        scarfPS = scarfObj.AddComponent<ParticleSystem>();
        var sMain = scarfPS.main;
        sMain.loop = true;
        sMain.startLifetime = 15f; // ★ 时间放慢到 15 秒！
        sMain.startSpeed = 0f;
        sMain.startSize = 0.0001f; // 几乎隐藏本体，全看丝带拖尾
        sMain.startColor = new Color(1f, 0.9f, 0.2f, 1f);
        sMain.simulationSpace = ParticleSystemSimulationSpace.World;
        sMain.scalingMode = ParticleSystemScalingMode.Hierarchy;
        sMain.maxParticles = 2; // ★ 物理级硬锁：全宇宙同时最多只能存在 2 条丝带！绝对不可能乱！

        var sShape = scarfPS.shape;
        sShape.shapeType = ParticleSystemShapeType.Circle;
        sShape.position = Vector3.up * wMinY; // ★ 从地面根部开始往上绕！
        sShape.radius = canopyMaxHeight * 4.5f; // ★ 直径再远3倍！
        sShape.arcMode = ParticleSystemShapeMultiModeValue.BurstSpread; // 让两个丝带头在圆环对侧严格对称！
        
        var sVel = scarfPS.velocityOverLifetime;
        sVel.enabled = true;
        sVel.orbitalY = 0.3f; // ★ 极慢极慢的环绕旋转
        sVel.y = (canopyMaxHeight * 2.0f) / 15f; // ★ 15秒内飞高到原先 2 倍的树盖高度，角度更大，更明显往上蹿！

        var sNoise = scarfPS.noise;
        sNoise.enabled = true;
        sNoise.strength = 1.0f / s; 
        sNoise.frequency = 0.15f;  
        sNoise.scrollSpeed = 0.2f;

        var sColList = scarfPS.colorOverLifetime;
        sColList.enabled = false; // 关闭生命周期颜色，转而使用真正的拖尾末端淡出

        var sTrails = scarfPS.trails;
        sTrails.enabled = true;
        sTrails.ratio = 1.0f; 
        sTrails.lifetimeMultiplier = 0.4f; // 拖尾长度
        
        // ★ 针对拖影本身的头尾颜色淡出：头部完全不透明 -> 尾部彻底变淡透明
        Gradient trailGrad = new Gradient();
        trailGrad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        sTrails.colorOverTrail = trailGrad;

        var sRender = scarfPS.GetComponent<ParticleSystemRenderer>();
        sRender.renderMode = ParticleSystemRenderMode.None; // ★ 隐藏光球本身，只渲染丝带拖尾！
        sRender.trailMaterial = glowMat;
        
        var sEmis = scarfPS.emission;
        sEmis.rateOverTime = 0; 
        sEmis.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 2, 2, 0, 15f) }); // ★ 与 15 秒寿命保持一致，15秒发一次

        // ==========================================
        // 5. 蝴蝶与土壤 (Butterfly_PS, Soil_PS)
        // ==========================================
        CreateButterfliesAndSoil(s, glowMat);
    }

    void CreateButterfliesAndSoil(float s, Material glowMat)
    {
        // 蝴蝶
        GameObject bf = new GameObject("Butterfly_PS");
        bf.transform.SetParent(transform, false);
        bf.transform.localPosition = Vector3.zero;
        bf.transform.localScale = Vector3.one; 
        
        butterfliesPS = bf.AddComponent<ParticleSystem>();
        var bMain = butterfliesPS.main;
        bMain.loop = true;
        bMain.startLifetime = new ParticleSystem.MinMaxCurve(4f, 8f);
        bMain.startSpeed = 0f; // 无需初始速度
        bMain.startSize = 0.006f; // ★ 变小，远看不再突兀
        bMain.simulationSpace = ParticleSystemSimulationSpace.World;
        bMain.scalingMode = ParticleSystemScalingMode.Hierarchy;

        var bShape = butterfliesPS.shape;
        bShape.shapeType = ParticleSystemShapeType.Box; // ★ 跟随粉色花瓣相同的发射区域
        float treeH = aMaxY - aMinY;
        bShape.position = Vector3.up * (aMinY + treeH * 0.95f); // 同样定位在树冠 95%
        bShape.scale = new Vector3(treeH * 1.5f, treeH * 0.1f, treeH * 1.5f); // 相同的覆盖整个树顶的薄气垫
        
        var bVel = butterfliesPS.velocityOverLifetime;
        bVel.enabled = true;
        bVel.orbitalY = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f); // 稍微加快环绕速度让其有动感
        bVel.x = new ParticleSystem.MinMaxCurve(-0.2f / s, 0.2f / s); // ★ 加快横向穿梭
        bVel.y = new ParticleSystem.MinMaxCurve(-0.05f / s, 0.05f / s); // ★ 给一点微弱的上下浮动，不死板
        bVel.z = new ParticleSystem.MinMaxCurve(-0.2f / s, 0.2f / s);

        var bColList = butterfliesPS.colorOverLifetime;
        bColList.enabled = true;
        Gradient bGrad = new Gradient();
        // ★ 从透明度 0 淡入出现，在尾期彻底淡出 (decay 消失)
        bGrad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(1f, 0.8f), new GradientAlphaKey(0f, 1f) }
        );
        bColList.color = bGrad;

        var bTrails = butterfliesPS.trails;
        bTrails.enabled = true;
        bTrails.ratio = 1.0f; // 蝴蝶全部带有光斑拖尾
        bTrails.lifetimeMultiplier = 0.2f; // 拖尾轻微保留

        var texAnim = butterfliesPS.textureSheetAnimation;
        texAnim.enabled = true; // ★ 重新开启帧动画！原来是 3 个连着的图！
        texAnim.numTilesX = 3;  // ★ 1行3列的排布
        texAnim.numTilesY = 1;
        texAnim.animation = ParticleSystemAnimationType.WholeSheet; // 连贯播放整个序列
        texAnim.cycleCount = 10; // ★ 保证在漫长的生命里，翅膀能多扑扇几遍！

        var bRender = butterfliesPS.GetComponent<ParticleSystemRenderer>();
        bRender.renderMode = ParticleSystemRenderMode.Billboard;
        bRender.trailMaterial = glowMat; // 拖尾材质！
        var bMat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit"));
        bMat.EnableKeyword("_ALPHABLEND_ON");
        bMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        bMat.SetFloat("_Surface", 1.0f);
        bMat.SetFloat("_Blend", 0.0f);
        bMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        bMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        bMat.SetInt("_ZWrite", 0);
        if (butterflyTexture != null)
        {
            bMat.mainTexture = butterflyTexture;
            if (bMat.HasProperty("_BaseMap")) bMat.SetTexture("_BaseMap", butterflyTexture);
        }
        bRender.material = bMat;
        var bEmis = butterfliesPS.emission;
        bEmis.rateOverTime = 0;

        // 泥土
        GameObject soil = new GameObject("Soil_PS");
        soil.transform.SetParent(transform, false);
        soil.transform.localPosition = Vector3.zero;
        soil.transform.localScale = Vector3.one / s; 
        soilPS = soil.AddComponent<ParticleSystem>();
        var mMain = soilPS.main;
        mMain.loop = true;
        mMain.startLifetime = 4f;
        mMain.startSpeed = 0f;
        mMain.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
        mMain.startColor = new Color(0.25f, 0.15f, 0.05f, 0.8f);
        var mShape = soilPS.shape;
        mShape.shapeType = ParticleSystemShapeType.Circle;
        mShape.radius = 5.0f;
        var mVel = soilPS.velocityOverLifetime;
        mVel.enabled = true;
        mVel.y = new ParticleSystem.MinMaxCurve(-0.05f, 0.2f);
        var mRender = soilPS.GetComponent<ParticleSystemRenderer>();
        mRender.renderMode = ParticleSystemRenderMode.Billboard;
        mRender.material = glowMat;
        var mEmis = soilPS.emission;
        mEmis.rateOverTime = 30f; // 泥土始终存在
    }

    // ==========================================
    // 交互与状态更新
    // ==========================================

    void OnTriggerStay(Collider other)
    {
        // 向上层级搜索：解决手柄碰撞体挂在子物体上（比如名为 Sphere 且无标签）导致漏判的问题
        bool isValidHand = false;
        Transform curr = other.transform;
        
        while (curr != null)
        {
            if (curr.CompareTag("PlayerHand") || curr.CompareTag("GameController")) 
            { 
                isValidHand = true; 
                break; 
            }
            string cn = curr.name.ToLower();
            // 极度保守的名字匹配，防止误伤玩家的 Gaze Interactor, Teleport Interactor 或 PlayerController
            if (cn == "left controller" || cn == "right controller" || cn.Contains("hand") || cn.Contains("poke")) 
            { 
                isValidHand = true; 
                break; 
            }
            curr = curr.parent;
        }

        if (isValidHand)
        {
            triggerOverlapDetected = true;
        }
    }

    void Update()
    {
        // 彻底移除原先的 Camera 自动触发预案与全场景手部对象搜索逻辑
        // 因为用户场景中已经在碰撞体上精准绑定了 triggers。
        // 现在仅完全依靠真实的物体碰撞 (手部/控制器触发 OnTriggerStay)
        bool isHealing = triggerOverlapDetected;
        triggerOverlapDetected = false;

        if (isHealing)
        {
            energyLevel += healingRate * Time.deltaTime;
            healLingerTimer = healLingerDuration;
            // 每次开始交互播放一次魔法音效（一次交互响一次）
            if (!wasHealing && magicHealAudio != null && !magicHealAudio.isPlaying)
                magicHealAudio.PlayOneShot(magicHealAudio.clip);
        }
        else
        {
            if (healLingerTimer > 0f)
            {
                healLingerTimer -= Time.deltaTime; // 等待延迟时间结束
            }
            else
            {
                energyLevel -= decayRate * Time.deltaTime; // 延迟耗尽，开始极其缓慢地自然衰退（从上往下）
            }
        }

        energyLevel = Mathf.Clamp01(energyLevel);
        wasHealing = isHealing;

        UpdateParticleSystems(isHealing);
    }

    void UpdateParticleSystems(bool isHealing)
    {
        if (witheredPS == null || alivePS == null) return;
        
        float reversedEnergy = 1.0f - energyLevel;
        
        var wEmis = witheredPS.emission;
        wEmis.rateOverTime = witheredParticleRate * reversedEnergy;

        var aEmis = alivePS.emission;
        aEmis.rateOverTime = aliveParticleRate * energyLevel;

        // 2. 黄色由于改为了两条动态上升的丝带拖尾 Burst，仅需整体控制启停即可，不需要 rateOverTime
        var sEmis = scarfPS.emission;
        if (energyLevel >= 0.95f || (energyLevel > 0f && isHealing))
        {
            if (!sEmis.enabled) { sEmis.enabled = true; scarfPS.Play(); } // 重置播放触发两条初始丝带
        }
        else
        {
            sEmis.enabled = false;
        }

        // 3. 满状态触发特效：粉色落花 & 蝴蝶 & 鸟叫循环
        if (energyLevel >= 1.0f && !fullyHealedTriggered)
        {
            fullyHealedTriggered = true;
            // 开启鸟叫随机循环
            if (birdAudio != null && birdCoroutine == null)
                birdCoroutine = StartCoroutine(RandomBirdRoutine());
        }
        else if (energyLevel < 0.95f && fullyHealedTriggered)
        {
            fullyHealedTriggered = false;
            // 停止鸟叫循环
            if (birdCoroutine != null) { StopCoroutine(birdCoroutine); birdCoroutine = null; }
            if (birdAudio != null && birdAudio.isPlaying) birdAudio.Stop();
        }

        // 保持飘落特效状态（密集的短距悬浮花簇）
        var pEmis = petalsPS.emission;
        if (energyLevel >= 0.95f)
        {
            pEmis.rateOverTime = 800f; // ★ 爆发式增加，形成像花一样一簇簇极其密集的分布
        }
        else
        {
            pEmis.rateOverTime = 0f;
        }

        var bEmis = butterfliesPS.emission;
        bEmis.rateOverTime = (energyLevel >= 0.95f) ? 0.4f : 0f; // ★ 再少一半，数量极度稀缺
    }

    IEnumerator RandomBirdRoutine()
    {
        // 激活后立即叫一声
        if (birdAudio != null) birdAudio.PlayOneShot(birdAudio.clip);

        while (true)
        {
            yield return new WaitForSeconds(Random.Range(3f, 5f));
            if (birdAudio != null)
            {
                birdAudio.pitch = Random.Range(0.9f, 1.1f);
                birdAudio.PlayOneShot(birdAudio.clip);
            }
        }
    }

    void LateUpdate()
    {
        // 1. 枯树骨架的专属扫描高度
        float wLimitY = Mathf.Lerp(wMinY, wMaxY, energyLevel);
        // 2. 活树模型的专属扫描高度
        float aLimitY = Mathf.Lerp(aMinY, aMaxY, energyLevel);

        // 1. 枯树粒子：向上扫光消散
        if (witheredPS != null && witheredPS.isPlaying && witheredPS.particleCount > 0)
        {
            int count = witheredPS.GetParticles(pBuffer);
            for (int i = 0; i < count; i++)
            {
                if (pBuffer[i].position.y < wLimitY)
                    pBuffer[i].remainingLifetime = -1f; // 已治愈区域，枯树立刻消失
            }
            witheredPS.SetParticles(pBuffer, count);
        }

        // 2. 活树粒子：纯粹使用活着的大树模型，由高极值动态进行颜色渐变计算
        if (alivePS != null && alivePS.isPlaying && alivePS.particleCount > 0)
        {
            int count = alivePS.GetParticles(pBuffer);
            
            // 3段式完美色彩渐变：根部(棕) -> 树心(绿) -> 树冠全粉(Pink)
            // 让大量粉色粒子在树冠上静止附着，完美契合柳絮飘落氛围
            float trunkLine = aMinY + (aMaxY - aMinY) * 0.30f;
            float leafLine = aMinY + (aMaxY - aMinY) * 0.65f;
            float petalLine = aMinY + (aMaxY - aMinY) * 0.85f;

            for (int i = 0; i < count; i++)
            {
                float y = pBuffer[i].position.y;
                if (y > aLimitY)
                {
                    pBuffer[i].remainingLifetime = -1f; // 还没治愈到的区域抑制活树
                }
                else
                {
                    // 完美的动态插值三段色彩渐变
                    if (y <= trunkLine)
                        pBuffer[i].startColor = aliveTrunkColor;
                    else if (y <= leafLine)
                        pBuffer[i].startColor = Color.Lerp(aliveTrunkColor, aliveLeafColor, (y - trunkLine) / (leafLine - trunkLine));
                    else if (y <= petalLine)
                        pBuffer[i].startColor = Color.Lerp(aliveLeafColor, pinkPetalColor, (y - leafLine) / (petalLine - leafLine));
                    else
                        pBuffer[i].startColor = pinkPetalColor; // 树冠满粉
                }
            }
            alivePS.SetParticles(pBuffer, count);
        }
    }
}

/// <summary>
/// 自动挂载在带有 Rigidbody 且是 Trigger 的子物体（如树枝小球）上。
/// 解决 Unity 物理引擎中：子物体带有独立 Rigidbody 时，碰撞事件不会冒泡给父物体脚本的问题。
/// </summary>
public class TreeTriggerForwarder : MonoBehaviour
{
    public ParticleTreeHealer parentHealer;

    void OnTriggerStay(Collider other)
    {
        if (parentHealer == null) return;
        
        // 向上层级搜索：解决手柄碰撞体挂在子物体上导致漏判的问题
        bool isValidHand = false;
        Transform curr = other.transform;
        
        while (curr != null)
        {
            if (curr.CompareTag("PlayerHand") || curr.CompareTag("GameController")) 
            { 
                isValidHand = true; 
                break; 
            }
            string cn = curr.name.ToLower();
            // 极度保守的名字匹配，防止误伤玩家的 Gaze Interactor, Teleport Interactor 或 PlayerController
            if (cn == "left controller" || cn == "right controller" || cn.Contains("hand") || cn.Contains("poke")) 
            { 
                isValidHand = true; 
                break; 
            }
            curr = curr.parent;
        }

        if (isValidHand)
        {
            parentHealer.triggerOverlapDetected = true;
        }
    }
}
