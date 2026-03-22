using UnityEngine;
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
    public float aliveMeshScaleMultiplier = 0.8f;

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
    public float witheredParticleRate = 3000f;
    [Tooltip("绿树的最大生成速率（决定多密集）")]
    public float aliveParticleRate = 5000f;
    [Tooltip("单一阶段允许同时存活的最大粒子数量极限")]
    public int maxParticleLimit = 15000;

    [Header("====== 治愈与自然衰退进度 ======")]
    [Range(0.01f, 1f)] public float healingRate = 0.05f;
    [Range(0.01f, 1f)] public float decayRate = 0.02f;
    [Range(0, 1)] public float energyLevel = 0f;

    [Header("====== 音效与贴图 ======")]
    public AudioSource birdAudio;
    public AudioSource chimeAudio;
    [Tooltip("蝴蝶动画序列帧，2x2 切图")]
    public Texture2D butterflyTexture;

    // --- 内部粒子系统引用 ---
    private ParticleSystem witheredPS;
    private ParticleSystem alivePS;
    private ParticleSystem petalsPS;
    private ParticleSystem scarfPS;
    private ParticleSystem butterflyPS;
    private ParticleSystem soilPS;

    // --- 逻辑控制 ---
    private Collider treeCollider;
    [HideInInspector] public bool triggerOverlapDetected = false;
    private bool fullyHealedTriggered = false;
    private ParticleSystem.Particle[] pBuffer; // 用于读取和染色粒子的高效缓存
    private float scanTimer = 0f;
    private List<GameObject> cachedHands = new List<GameObject>();
    private float treeMinY = 0f;

    // 粒子生成速率基准
    private readonly int MAX_WITHERED_RATE = 15000;
    private readonly int MAX_ALIVE_RATE = 15000;

    void Start()
    {
        treeCollider = GetComponent<Collider>();
        pBuffer = new ParticleSystem.Particle[Mathf.Max(MAX_ALIVE_RATE, MAX_WITHERED_RATE) + 2000];
        energyLevel = 0f;

        // ★ 动态计算树干截断高度与树冠高度，避免不同模型Inspector填错导致的问题
        if (aliveMesh != null)
        {
            treeMinY = aliveMesh.bounds.min.y * aliveMeshScaleMultiplier + aliveMeshPositionOffset.y;
            float maxY = aliveMesh.bounds.max.y * aliveMeshScaleMultiplier + aliveMeshPositionOffset.y;
            trunkHeightThreshold = treeMinY + (maxY - treeMinY) * 0.45f; // 下方 45% 作为树干 (褐色)
            canopyMaxHeight = maxY;
        }

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
        aMain.startSize = new ParticleSystem.MinMaxCurve(0.0002f, 0.0005f);
        aMain.startColor = Color.white;
        aMain.maxParticles = maxParticleLimit;
        aMain.playOnAwake = true;
        
        var aShape = alivePS.shape;
        aShape.shapeType = ParticleSystemShapeType.Mesh;
        aShape.meshShapeType = ParticleSystemMeshShapeType.Triangle;
        aShape.mesh = aliveMesh;
        aShape.position = aliveMeshPositionOffset;
        aShape.rotation = aliveMeshRotationOffset;
        aShape.scale = Vector3.one * aliveMeshScaleMultiplier;

        var aCol = alivePS.colorOverLifetime;
        aCol.enabled = true;
        aCol.color = wGrad; // 复用相同的透明度渐变

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
        // 位置设定在树冠顶端
        pObj.transform.localPosition = Vector3.up * (canopyMaxHeight * 0.85f);
        pObj.transform.localScale = Vector3.one / s; // 抵消父物体巨大的缩放
        
        petalsPS = pObj.AddComponent<ParticleSystem>();
        var pMain = petalsPS.main;
        pMain.simulationSpace = ParticleSystemSimulationSpace.World;
        pMain.startLifetime = new ParticleSystem.MinMaxCurve(5f, 10f); // 飘落很久
        pMain.startSpeed = 0f;
        pMain.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
        pMain.startColor = pinkPetalColor; // ★ 使用面板上公开的粉色变量
        pMain.gravityModifier = 0.015f; // 极缓的重力落下
        pMain.maxParticles = 2000;

        var pShape = petalsPS.shape;
        pShape.shapeType = ParticleSystemShapeType.Sphere;
        pShape.radius = canopyMaxHeight * 0.4f;

        var pVel = petalsPS.velocityOverLifetime;
        pVel.enabled = true;
        pVel.x = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);
        pVel.z = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);
        pVel.orbitalY = 0.15f; // 轻微旋转落下
        
        var pNoise = petalsPS.noise;
        pNoise.enabled = true;
        pNoise.strength = 0.2f;
        pNoise.frequency = 0.5f;

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
        scarfObj.transform.localPosition = Vector3.up * (canopyMaxHeight * 0.5f);
        scarfObj.transform.localScale = Vector3.one / s;

        scarfPS = scarfObj.AddComponent<ParticleSystem>();
        var sMain = scarfPS.main;
        sMain.loop = true;
        sMain.startLifetime = 2.5f;
        sMain.startSpeed = 0f;
        sMain.startSize = 0.5f;
        sMain.startColor = new Color(1f, 0.9f, 0.2f, 1f);
        sMain.simulationSpace = ParticleSystemSimulationSpace.World;

        var sShape = scarfPS.shape;
        sShape.shapeType = ParticleSystemShapeType.Circle;
        sShape.radius = canopyMaxHeight * 0.4f;
        sShape.arcMode = ParticleSystemShapeMultiModeValue.Loop;
        
        var sVel = scarfPS.velocityOverLifetime;
        sVel.enabled = true;
        sVel.orbitalY = 4.0f; 
        sVel.y = new ParticleSystem.MinMaxCurve(1.0f, 2.5f); 

        var sNoise = scarfPS.noise;
        sNoise.enabled = true;
        sNoise.strength = 1.2f;
        sNoise.frequency = 0.3f;
        sNoise.scrollSpeed = 0.5f;

        var sColList = scarfPS.colorOverLifetime;
        sColList.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(0f, 1f) }
        );
        sColList.color = grad;

        var sRender = scarfPS.GetComponent<ParticleSystemRenderer>();
        sRender.renderMode = ParticleSystemRenderMode.Billboard;
        sRender.material = glowMat;
        
        var sEmis = scarfPS.emission;
        sEmis.rateOverTime = 0; // 边治愈边出现

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
        bf.transform.localPosition = Vector3.up * (canopyMaxHeight * 0.7f);
        bf.transform.localScale = Vector3.one / s; 
        butterflyPS = bf.AddComponent<ParticleSystem>();
        var bMain = butterflyPS.main;
        bMain.loop = true;
        bMain.startLifetime = new ParticleSystem.MinMaxCurve(4f, 8f);
        bMain.startSpeed = new ParticleSystem.MinMaxCurve(1.0f, 3.0f);
        bMain.startSize = 0.5f; 
        bMain.simulationSpace = ParticleSystemSimulationSpace.World;
        var bShape = butterflyPS.shape;
        bShape.shapeType = ParticleSystemShapeType.Sphere;
        bShape.radius = 10.0f;
        var bVel = butterflyPS.velocityOverLifetime;
        bVel.enabled = true;
        bVel.orbitalY = new ParticleSystem.MinMaxCurve(-0.8f, 0.8f); 
        var texAnim = butterflyPS.textureSheetAnimation;
        texAnim.enabled = true;
        texAnim.numTilesX = 2; 
        texAnim.numTilesY = 2;
        texAnim.animation = ParticleSystemAnimationType.WholeSheet;
        var bRender = butterflyPS.GetComponent<ParticleSystemRenderer>();
        bRender.renderMode = ParticleSystemRenderMode.Billboard;
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
        var bEmis = butterflyPS.emission;
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

        if (isHealing) energyLevel += healingRate * Time.deltaTime; 
        else energyLevel -= decayRate * Time.deltaTime;

        energyLevel = Mathf.Clamp01(energyLevel);

        UpdateParticleSystems(isHealing);
    }

    void UpdateParticleSystems(bool isHealing)
    {
        if (witheredPS == null || alivePS == null || petalsPS == null) return;
        
        float reversedEnergy = 1.0f - energyLevel;
        
        var wEmis = witheredPS.emission;
        wEmis.rateOverTime = witheredParticleRate * reversedEnergy;

        var aEmis = alivePS.emission;
        aEmis.rateOverTime = aliveParticleRate * energyLevel;

        // 2. 黄色丝巾环绕逻辑
        var sEmis = scarfPS.emission;
        if (energyLevel > 0f && energyLevel < 0.99f && isHealing)
            sEmis.rateOverTime = 150f;
        else
            sEmis.rateOverTime = 0f;

        // 3. 满状态触发特效：粉色落花 & 蝴蝶 & 音效
        if (energyLevel >= 1.0f && !fullyHealedTriggered)
        {
            fullyHealedTriggered = true;
            if (birdAudio != null) birdAudio.Play();
            if (chimeAudio != null) chimeAudio.Play();
        }
        else if (energyLevel < 0.95f && fullyHealedTriggered)
        {
            fullyHealedTriggered = false; // 允许第二次交互重新触发！
        }

        // 保持飘落特效状态（粉簇落下）
        var pEmis = petalsPS.emission;
        if (energyLevel >= 0.95f)
        {
            pEmis.rateOverTime = 50f;
        }
        else
        {
            pEmis.rateOverTime = 0f;
        }

        var bEmis = butterflyPS.emission;
        bEmis.rateOverTime = (energyLevel >= 0.95f) ? 3f : 0f;
    }

    void LateUpdate()
    {
        // 从下往上的治愈扫描线 (根据 energyLevel 决定当前活化到了多高)
        float currentHeightLimit = Mathf.Lerp(treeMinY, canopyMaxHeight, energyLevel);

        // 1. 枯树粒子：向上扫光消散
        if (witheredPS != null && witheredPS.isPlaying && witheredPS.particleCount > 0)
        {
            int count = witheredPS.GetParticles(pBuffer);
            for (int i = 0; i < count; i++)
            {
                if (pBuffer[i].position.y < currentHeightLimit)
                {
                    pBuffer[i].remainingLifetime = -1f; // 已治愈区域，枯树立刻消失
                }
            }
            witheredPS.SetParticles(pBuffer, count);
        }

        // 2. 活树粒子：动态高度展开与三段式渐变变色
        if (alivePS != null && alivePS.isPlaying && alivePS.particleCount > 0)
        {
            int count = alivePS.GetParticles(pBuffer);
            float trunkLine = treeMinY + (canopyMaxHeight - treeMinY) * 0.40f;
            float leafLine = treeMinY + (canopyMaxHeight - treeMinY) * 0.85f;

            for (int i = 0; i < count; i++)
            {
                float y = pBuffer[i].position.y;
                if (y > currentHeightLimit)
                {
                    pBuffer[i].remainingLifetime = -1f; // 还没治愈到的区域抑制活树粒子
                }
                else
                {
                    // 高度染色功能！
                    if (y <= trunkLine)
                        pBuffer[i].startColor = aliveTrunkColor; // 底部树桩：褐色
                    else if (y <= leafLine)
                        pBuffer[i].startColor = aliveLeafColor;  // 中上部枝干：绿色
                    else
                        pBuffer[i].startColor = pinkPetalColor;  // 顶部枝梢：粉色
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
