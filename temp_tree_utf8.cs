using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// πÇÉσà¿µû░ΘçìσåÖπÇæτ║»τ▓Æσ¡ÉΘ⌐▒σè¿τÜäµáæµ£¿µ▓╗µäêτ│╗τ╗ƒπÇé
/// σ«îσà¿σ╝âτö¿σ«₧Σ╜ô MeshRenderer∩╝îΣ╛¥Θ¥áΣ╗úτáüµÄºσê╢µ┤╗µáæ/µ₧»µáæΣ╕ñσÑùτï¼τ½ïτ▓Æσ¡Éτ│╗τ╗ƒ∩╝î
/// σ╣╢Σ╕öµö»µîüµá╣µì«Θ½ÿσ║ªσè¿µÇüσ░åσÉîΣ╕Çµú╡µáæτÜäτ▓Æσ¡ÉµƒôΦë▓Σ╕║µáæσ╣▓∩╝êµúòΦë▓∩╝ëσÆîµáæσÅ╢∩╝êτ╗┐Φë▓∩╝ëπÇé
/// </summary>
public class ParticleTreeHealer : MonoBehaviour
{
    [Header("====== µá╕σ┐âτ╜æµá╝σ╝òτö¿ ======")]
    [Tooltip("µ₧»µáæµ¿íσ₧ï∩╝êτö¿Σ║ÄτöƒµêÉµ₧»µáæτ▓Æσ¡É∩╝ë")]
    public Mesh witheredMesh;
    [Tooltip("µ┤╗µáæµ¿íσ₧ï∩╝êτö¿Σ║ÄτöƒµêÉτ╗┐Φë▓µáæσåáσÆîµ┤╗µáæσ╣▓τ▓Æσ¡É∩╝ë")]
    public Mesh aliveMesh;
    public Vector3 aliveMeshPositionOffset = Vector3.zero;
    public Vector3 aliveMeshRotationOffset = new Vector3(-90, 0, 0);
    public float aliveMeshScaleMultiplier = 0.35f; // πÇÉσåìΣ╕Çµ¼íτ╝⌐σ░ÅπÇæΦ«⌐τ╗┐µáæσ░╜ΘçÅΦâ╜σÑùΦ┐¢µ₧»µáæΣ╕╗σ╣▓Θçî

    [Header("====== ΦºåΦºëµƒôΦë▓Σ╕Äτë╣µòê ======")]
    [Tooltip("µ₧»µáæτè╢µÇüτÜäτ▓Æσ¡ÉΘó£Φë▓ (µúòΦë▓/µÜùτÉÑτÅÇΦë▓)")]
    public Color witheredColor = new Color(0.4f, 0.2f, 0.05f);
    
    [Tooltip("µ┤╗µáæµáæσ╣▓τÜäτ▓Æσ¡ÉΘó£Φë▓ (Σ║«µúòΦë▓)")]
    public Color aliveTrunkColor = new Color(0.45f, 0.25f, 0.1f);
    
    [Tooltip("µ┤╗µáæµáæσÅ╢τÜäτ▓Æσ¡ÉΘó£Φë▓ (τ┐áτ╗┐Φë▓)")]
    public Color aliveLeafColor = new Color(0.15f, 0.8f, 0.25f);

    [Tooltip("µáæΘí╢ΦÉ╜Φè▒τ▓Æσ¡ÉτÜäΘó£Φë▓ (τ▓ëΦë▓)")]
    public Color pinkPetalColor = new Color(1f, 0.6f, 0.8f, 0.9f);
    
    [Tooltip("Θ½ÿσ║ªΘÿêσÇ╝∩╝Üσêñµû¡ aliveMesh τÜäσô¬Σ║¢Θí╢τé╣σ║öΦ»ÑµƒôµêÉµúòΦë▓µáæσ╣▓∩╝îσô¬Σ║¢µƒôµêÉτ╗┐Φë▓µáæσÅ╢πÇéσ£¿σ▒ÇΘâ¿σ¥Éµáçτ│╗Σ╕ïΦííΘçÅπÇé")]
    public float trunkHeightThreshold = 15f; 
    
    [Tooltip("µáæσåáΦ┐æΣ╝╝Θ½ÿσ║ª∩╝êτö¿Σ║Äτí«σ«ÜΣ╕¥σ╖╛πÇüτ▓ëΦë▓Φè▒τôúπÇüΦ¥┤Φ¥╢τÜäτöƒµêÉΣ╜ìτ╜«∩╝ë")]
    public float canopyMaxHeight = 30f;

    [Header("====== τ▓Æσ¡Éσ»åσ║ªΣ╕Äµò░ΘçÅΦ«╛τ╜« (Σ╗╗µäÅΦ░âµò┤σ░¥Φ»ò) ======")]
    [Tooltip("µ₧»µáæτÜäµ£ÇσñºτöƒµêÉΘÇƒτÄç∩╝êσå│σ«ÜσñÜσ»åΘ¢å∩╝ë")]
    public float witheredParticleRate = 5000f; // Γÿà µÅÉσìçσ»åσ║ªΘ╗ÿΦ«ñσÇ╝
    [Tooltip("τ╗┐µáæτÜäµ£ÇσñºτöƒµêÉΘÇƒτÄç∩╝êσå│σ«ÜσñÜσ»åΘ¢å∩╝ë")]
    public float aliveParticleRate = 12000f; // Γÿà µÅÉσìçσ»åσ║ªΘ╗ÿΦ«ñσÇ╝
    [Tooltip("σìòΣ╕ÇΘÿ╢µ«╡σàüΦ«╕σÉîµù╢σ¡ÿµ┤╗τÜäµ£Çσñºτ▓Æσ¡Éµò░ΘçÅµ₧üΘÖÉ")]
    public int maxParticleLimit = 15000;

    [Header("====== µ▓╗µäêΣ╕ÄΦç¬τä╢Φí░ΘÇÇΦ┐¢σ║ª ======")]
    [Tooltip("σ╗╢Φ┐ƒΦí░ΘÇÇτÜäµù╢Θù┤∩╝Üτª╗σ╝ÇµëïσÉÄτ¡ëσñÜΣ╣àµëìσ╝ÇσºïµÄëΦë▓σÅÿσ░Å∩╝êΘ╗ÿΦ«ñ60τºÆ∩╝ë")]
    public float healLingerDuration = 60f;
    private float healLingerTimer = 0f;

    [Range(0.01f, 1f)] public float healingRate = 0.05f;
    [Range(0.01f, 1f)] public float decayRate = 0.02f;
    [Range(0, 1)] public float energyLevel = 0f;

    [Header("====== Θƒ│µòêΣ╕ÄΦ┤┤σ¢╛ ======")]
    [Tooltip("Θ╕ƒσÅ½σú░Θƒ│ΘóæµûçΣ╗╢∩╝êσ«îσà¿µ▓╗µäêµù╢µÆ¡µö╛∩╝ë")]
    public AudioClip birdAudioClip;
    [Tooltip("Θ╕ƒσÅ½σú░Θƒ│ΘçÅ")]
    [Range(0f, 1f)] public float birdVolume = 0.5f;
    [Tooltip("Θ╕ƒσÅ½µ£Çσ░ÅΘù┤ΘÜö(τºÆ)")]
    public float minBirdInterval = 6f;
    [Tooltip("Θ╕ƒσÅ½µ£ÇσñºΘù┤ΘÜö(τºÆ)")]
    public float maxBirdInterval = 12f;
    [Tooltip("Φºªτó░µáæτÜäΘ¡öµ│òµ▓╗µäêΘƒ│µòê")]
    public AudioClip magicHealClip;

    [Header("Bird Audio Distance (µëïσè¿Φ░âµò┤)")]
    public float birdFarDistance = 15f;
    public float birdNearDistance = 1.0f;
    public float birdFalloff = 1.5f;

    [Header("Magic Heal Settings (µëïσè¿Φ░âµò┤)")]
    [Tooltip("σùíΘ╕úΘƒ│ΘçÅ")]
    [Range(0f, 2f)] public float magicVolume = 1.0f;
    [Tooltip("µëïΘâ¿Φ╖¥τª╗µáæσ╣▓Σ╕¡σ┐âσñÜσ░æτ▒│σåàµëìσôìΦ╡╖Θ¡öµ│òσùíΘ╕ú (µáæσñºΦ»╖Φ░âσñº)")]
    public float magicRecognitionDistance = 2.0f; 
    public float magicFarDistance = 5.0f;
    public float magicNearDistance = 0.5f;
    public float magicFalloff = 1.2f;
    public bool showDebugDistance = false;

    private AudioSource birdAudio;
    private AudioSource magicHealAudio;
    private float magicCurrentVolume = 0f; // τ¢┤µÄÑµÄºσê╢Θ¡öµ│òΘƒ│ΘçÅ∩╝îΣ╕ìΣ╛¥Φ╡û AudioDistanceFader

    [Tooltip("Φ¥┤Φ¥╢σè¿τö╗σ║Åσêùσ╕º∩╝î2x2 σêçσ¢╛")]
    public Texture2D butterflyTexture;

    // --- σåàΘâ¿τ▓Æσ¡Éτ│╗τ╗ƒσ╝òτö¿ ---
    private ParticleSystem witheredPS;
    private ParticleSystem alivePS;
    private ParticleSystem petalsPS;
    private ParticleSystem butterfliesPS;
    private ParticleSystem soilPS;
    private ParticleSystem yellowScarfPS;
    
    [Header("Distance Tracking")]
    [Tooltip("µáæσ╣▓Σ╕¡σ┐â∩╝êτö¿Σ║ÄΦ«íτ«ùµëïΘâ¿Φ╖¥τª╗∩╝ë∩╝îσªéµ₧£Σ╕ìµïûσàÑσêÖΣ╜┐τö¿µ£¼τë⌐Σ╜ôΣ╕¡σ┐â")]
    public Transform treeCenter;

    // --- ΘÇ╗Φ╛æµÄºσê╢ ---
    private Collider treeCollider;
    [HideInInspector] public bool triggerOverlapDetected = false;
    private bool fullyHealedTriggered = false;
    private bool wasHealing = false;
    private Coroutine birdCoroutine;
    private ParticleSystem.Particle[] pBuffer; // τö¿Σ║ÄΦ»╗σÅûσÆîµƒôΦë▓τ▓Æσ¡ÉτÜäΘ½ÿµòêτ╝ôσ¡ÿ
    private float scanTimer = 0f; // Kept as placeholder for future scans
    private List<GameObject> cachedHands = new List<GameObject>();
    private float wMinY = 0f;
    private float wMaxY = 10f;
    private float aMinY = 0f;
    private float aMaxY = 10f;

    // τ▓Æσ¡ÉτöƒµêÉΘÇƒτÄçσƒ║σçå
    private readonly int MAX_WITHERED_RATE = 15000;
    private readonly int MAX_ALIVE_RATE = 15000;

    void Start()
    {
        treeCollider = GetComponent<Collider>();
        pBuffer = new ParticleSystem.Particle[Mathf.Max(MAX_ALIVE_RATE, MAX_WITHERED_RATE) + 2000];
        energyLevel = 0f;

        // Φç¬σè¿σê¢σ╗║Θ╕ƒσÅ½ AudioSource∩╝êΣ╕ìΣ╜┐τö¿ AudioDistanceFader∩╝îΘ¥á Unity σÄƒτöƒ 3D rolloff∩╝ë
        if (birdAudioClip != null)
        {
            GameObject birdObj = new GameObject("BirdAudio");
            birdObj.transform.SetParent(transform, false);
            birdAudio = birdObj.AddComponent<AudioSource>();
            birdAudio.clip = birdAudioClip;
            birdAudio.spatialBlend = 1f;
            birdAudio.volume = birdVolume; // Γÿà µëïσè¿Φ░âΘƒ│ΘçÅ
            birdAudio.playOnAwake = false;
            birdAudio.ignoreListenerPause = true; // Γÿà Θÿ▓µ¡óΣ╝áΘÇüΣ╕¡µû¡
            birdAudio.minDistance = 5.0f; // Γÿà Φ░âσñºµ£Çσ░ÅΦ╖¥τª╗∩╝îτí«Σ┐¥µ¢┤Φ┐£Σ╣ƒΦâ╜σÉ¼σê░
            birdAudio.maxDistance = Mathf.Max(birdFarDistance, 30f); 
            birdAudio.rolloffMode = AudioRolloffMode.Linear;
            Debug.Log($"[TreeAudio] Bird audio created. Clip: {birdAudioClip.name}");
        }
        if (magicHealClip != null)
        {
            // Γÿà σê¢σ╗║σ£¿τï¼τ½ïσ¡Éτë⌐Σ╜ôΣ╕è∩╝îΘÿ▓µ¡óσÆî birdAudio σà▒Σ║½ gameObject σ»╝Φç┤ AudioDistanceFader σå▓τ¬ü
            GameObject magicObj = new GameObject("MagicHealAudio");
            magicObj.transform.SetParent(transform, false);
            magicHealAudio = magicObj.AddComponent<AudioSource>();
            magicHealAudio.clip = magicHealClip;
            // Γÿà Σ┐«µö╣Σ╕║ 2D Φ┤┤ΦÇ│Θƒ│µòê∩╝îΘÿ▓µ¡óµáæσ╣▓Σ╕¡σ┐âσñ¬Φ┐£σ»╝Φç┤σÄƒτöƒτÜä 3D Φí░σçÅΦ«⌐Σ╜áσÉ¼Σ╕ìΦºü
            magicHealAudio.spatialBlend = 0f; 
            magicHealAudio.loop = true;
            magicHealAudio.playOnAwake = false;
            magicHealAudio.ignoreListenerPause = true; 
            magicHealAudio.volume = 0f; 
            // Σ╕ìσåìσÉÄσÅ░µéäµéäµÆ¡µö╛∩╝îΣ╛¥Θ¥áΦºªµæ╕τ₧¼Θù┤ΦºªσÅæ Play()
            Debug.Log($"[TreeAudio] Magic audio created. Clip: {magicHealClip.name}, magicVolume: {magicVolume}");
        }

        // Γÿà σ╝║ΦíîΦªåτ¢û Inspector Σ╕¡σÅ»Φâ╜µ«ïτòÖτÜäµùºσÅéµò░∩╝îτí«Σ┐¥µ£¼µ¼íµ¢┤µû░τ½ïσì│τöƒµòê∩╝ü∩╝ü
        aliveMeshScaleMultiplier = 0.35f;
        witheredParticleRate = Mathf.Max(witheredParticleRate, 5000f);
        aliveParticleRate = Mathf.Max(aliveParticleRate, 12000f);

        // σìòτï¼Φ«íτ«ùσ«îσà¿Σ╕ìσÉîτÜäΣ╕ñσÑùΘ¬¿µ₧╢τ⌐║Θù┤τÜä Y µ₧üσÇ╝∩╝îΘÿ▓µ¡óσ¢áΣ╕║µ»öΣ╛ïΣ╕ìσÉîσ»╝Φç┤µë½µÅÅτ║┐Σ╕Äτ¥ÇΦë▓µû¡σ▒é∩╝ü
        if (witheredMesh != null && aliveMesh != null)
        {
            // ΘÇÜΦ┐çµ₧äσ╗║σ«îµò┤τÜäΣ╕┤µù╢µ╕▓µƒôσÖ¿µ¥Ñτ▓╛τí«ΦÄ╖σÅûτë⌐τÉåτòîΘÖÉσ╖«Φ╖¥∩╝îσ«îτ╛ÄΦºúσå│τ╝⌐µö╛Σ╕Ä -90 σ║ªµùïΦ╜¼ΘÇáµêÉτÜäσîàσ¢┤τ¢Æµë¡µ¢▓∩╝ü∩╝ü
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

            // σ¡ÿσàÑτ£ƒσ«₧σ░║σ║ªΣ╕ïτÜäµ£ÇΣ╜Äτé╣σ╖«Φ╖¥∩╝îσ╝║Φíîµè╡µ╢êσ¢áΣ╕║ Scale 0.35 σ»╝Φç┤τÜäµè¼σìçΦà╛τ⌐║∩╝ü
            float hoverOffset = wBounds.min.y - aBoundsRaw.min.y;
            // ΦíÑσü┐ 5% τÜäΘ½ÿσ║ªΘÿ▓µ¡óσ«îσà¿ΘÖ╖σàÑσ¢╛Σ╕¡
            hoverOffset += (wBounds.max.y - wBounds.min.y) * 0.05f;
            aliveMeshPositionOffset = new Vector3(aliveMeshPositionOffset.x, hoverOffset, aliveMeshPositionOffset.z);

            // µ¢┤µû░τ£ƒσ«₧Φ╛╣τòî∩╝îτí«Σ┐¥σ«âσ«îτ╛ÄµëÄµá╣
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
        
        // Θí╢Θâ¿τë╣µòê∩╝êΦè▒τôúπÇüΦ¥┤Φ¥╢∩╝ëσÅûΣ╕ñΣ╕¬µ¿íσ₧ïΣ╕¡µ£ÇΘ½ÿτÜäΘéúΣ╕ÇΣ╕¬∩╝îΘü┐σàìσìíσ£¿µáæσ╣▓Θçî
        canopyMaxHeight = Mathf.Max(wMaxY, aMaxY);
        trunkHeightThreshold = wMinY + (wMaxY - wMinY) * 0.45f;

        // σ╝║ΦíîΣ┐«µ¡ú UX Σ╜ôΘ¬îµò░σÇ╝∩╝îσ«îσà¿Θü╡σ╛¬Σ║ñΣ║ÆΘÇ╗Φ╛æ
        healingRate = 0.4f; // 2.5τºÆσ«îσà¿µ▓╗µäê
        healLingerDuration = 5.0f; // τª╗σ╝ÇµëïσÉÄ 5 τºÆΣ╛┐σ╝ÇσºïΦí░ΘÇÇ
        decayRate = 0.5f;  // 2τºÆΦí░ΘÇÇΣ╕║µ₧»µáæ∩╝êΦ╡░Φ┐£σÉÄσ╛êσ┐½µüóσñìµ₧»µ£¿∩╝ë

        // 1. σ╝║σè¢µ╕àτÉåµùºτè╢µÇü∩╝ÜΘÜÉΦùÅµëÇµ£ë MeshRenderer∩╝êµêæΣ╗¼σÅ¬Θ£ÇΦªüτ║»τ▓Æσ¡É∩╝ü∩╝ë
        var meshRenderers = GetComponentsInChildren<MeshRenderer>();
        foreach (var r in meshRenderers) r.enabled = false;

        // σ«ëσà¿σ£░µ╕àτÉåµùºτ▓Æσ¡Éτ│╗τ╗ƒ∩╝Ü
        // σêáΘÖñµá╣τë⌐Σ╜ôΣ╕èτÜäµùº ParticleSystem τ╗äΣ╗╢∩╝êΣ╜åΣ╕ìσêáΘÖñ GameObject µ£¼Φ║½∩╝ü∩╝ë
        ParticleSystem rootPS = GetComponent<ParticleSystem>();
        if (rootPS != null) Destroy(rootPS);

        // σÅ¬σêáΘÖñ**σ¡Éτë⌐Σ╜ô**Σ╕èτÜäµùºτ▓Æσ¡Éτ│╗τ╗ƒ
        foreach (Transform child in transform)
        {
            ParticleSystem childPS = child.GetComponent<ParticleSystem>();
            if (childPS != null) Destroy(child.gameObject);
        }

        // σ╣▓µÄëµùº VisualMeshBacking
        Transform oldVisual = transform.Find("VisualMeshBacking");
        if (oldVisual != null) Destroy(oldVisual.gameObject);

        // 2. Θçìµû░σê¢σ╗║σ«îτ╛ÄτÜäτ▓Æσ¡Éτ╗ôµ₧ä
        BuildParticleSystems();
    }

    void BuildParticleSystems()
    {
        Material glowMat = ParticleUtils.GetGlowingSphereMaterial();
        float s = Mathf.Max(transform.lossyScale.x, 1f);

        // ==========================================
        // 1. µ₧»µáæτ▓Æσ¡Éτ│╗τ╗ƒ (Withered_PS)
        // ==========================================
        GameObject wObj = new GameObject("Withered_PS");
        wObj.transform.SetParent(transform, false);
        witheredPS = wObj.AddComponent<ParticleSystem>();
        var wMain = witheredPS.main;
        wMain.simulationSpace = ParticleSystemSimulationSpace.Local;
        wMain.scalingMode = ParticleSystemScalingMode.Hierarchy; // Γÿà Φ«⌐τ▓Æσ¡Éσñºσ░ÅΦ╖ƒΘÜÅτê╢τë⌐Σ╜ôτ╝⌐µö╛
        wMain.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 3.0f); // Φç¬τä╢µ╢êµòú
        wMain.startSpeed = 0f;
        wMain.startSize = new ParticleSystem.MinMaxCurve(0.0002f, 0.0005f); // σ░║σ»╕σñºσ╣àσ║ªσçÅσ░Å
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
        wEmis.rateOverTime = 1500; // µîüτ╗¡τöƒµêÉ
        wEmis.SetBursts(new ParticleSystem.Burst[0]); 

        // ==========================================
        // 2. µ┤╗µáæτ▓Æσ¡Éτ│╗τ╗ƒ (Alive_PS)
        // ==========================================
        GameObject aObj = new GameObject("Alive_PS");
        aObj.transform.SetParent(transform, false);
        alivePS = aObj.AddComponent<ParticleSystem>();
        var aMain = alivePS.main;
        aMain.simulationSpace = ParticleSystemSimulationSpace.Local;
        aMain.scalingMode = ParticleSystemScalingMode.Hierarchy;
        aMain.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 3.0f);
        aMain.startSpeed = 0f;
        aMain.startSize = new ParticleSystem.MinMaxCurve(0.0002f, 0.0005f); // ΘÇéΣ╕¡σ░║σ»╕
        aMain.startColor = Color.white;
        aMain.maxParticles = maxParticleLimit;
        aMain.playOnAwake = true;
        
        var aShape = alivePS.shape;
        aShape.shapeType = ParticleSystemShapeType.Mesh;
        // Γÿà σ┐àΘí╗τö¿ Vertex∩╝îτí«Σ┐¥τ▓Æσ¡Éµ¡╗µ¡╗σÆ¼Σ╜ÅµáæτÜäµ»ÅΣ╕¬σñÜΦ╛╣σ╜óΘí╢τé╣∩╝îΣ╕ìΦªüµ¥╛µòúσÅæσ░ä
        aShape.meshShapeType = ParticleSystemMeshShapeType.Vertex;
        aShape.mesh = aliveMesh; // τ╗ƒΣ╕ÇσÅ¬τö¿τ╗┐µáæτÜäµ¿íσ₧ï∩╝ü
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
        // 3. τ▓ëΦë▓ΦÉ╜Φè▒τ▓Æσ¡Éτ│╗τ╗ƒ (PinkPetals_PS)
        // ==========================================
        GameObject pObj = new GameObject("PinkPetals_PS");
        pObj.transform.SetParent(transform, false);
        pObj.transform.localPosition = Vector3.zero;
        pObj.transform.localScale = Vector3.one;
        
        petalsPS = pObj.AddComponent<ParticleSystem>();
        var pMain = petalsPS.main;
        pMain.simulationSpace = ParticleSystemSimulationSpace.World;
        pMain.scalingMode = ParticleSystemScalingMode.Hierarchy;
        pMain.startLifetime = new ParticleSystem.MinMaxCurve(2.0f, 4.0f); // Γÿà µé¼µ╡«µ¢┤Σ╣àΣ╕Çτé╣
        pMain.startSpeed = 0f; 
        pMain.startSize = 0.0005f; // Γÿà τ╗¥σ»╣σ╛«σ░Å∩╝îτ╗¥σ»╣Σ╕ìσåìσÅÿσñº∩╝ü
        pMain.startColor = pinkPetalColor; 
        pMain.gravityModifier = 0f; // σ╜╗σ║òµùáΘçìσè¢
        pMain.maxParticles = 5000; 

        var pShape = petalsPS.shape;
        pShape.shapeType = ParticleSystemShapeType.Box; // Γÿà µö╣τö¿Θò┐µû╣Σ╜ô∩╝îσ«îτ╛Äσ«╜σ╣┐σ£░Φªåτ¢ûµò┤Σ╕¬µáæσåáσ▒é∩╝ü
        float treeH = aMaxY - aMinY;
        pShape.position = Vector3.up * (aMinY + treeH * 0.95f); // Γÿà µ₧üσà╢Θ¥áΣ╕è∩╝îΘöüσ«Üσ£¿Θí╢Θâ¿ 95%∩╝ü
        pShape.scale = new Vector3(treeH * 1.5f, treeH * 0.1f, treeH * 1.5f); // Γÿà µ₧üσ«╜µ₧üΦûäτÜäµ░öσ₧½σî║σƒƒ∩╝îτ╗¥σ»╣µòúσ╕âσà¿Φ║½∩╝ü

        var pVel = petalsPS.velocityOverLifetime;
        pVel.enabled = true; 
        pVel.x = new ParticleSystem.MinMaxCurve(-0.01f / s, 0.01f / s); 
        pVel.y = new ParticleSystem.MinMaxCurve(-0.002f / s, 0.002f / s); // Γÿà τ£ƒµ¡úτÜäσ╛«µ│óτ║ºσ«Üµá╝µé¼µ╡«∩╝ü
        pVel.z = new ParticleSystem.MinMaxCurve(-0.01f / s, 0.01f / s);

        var pSizeAnim = petalsPS.sizeOverLifetime;
        pSizeAnim.enabled = false; // Γÿà σ╜╗σ║òσà│Θù¡µö╛σñºµòêµ₧£∩╝üΦºúσå│σ╖¿σ₧ïΦè▒τôúτÜäΘù«Θóÿ∩╝ü
        
        var pNoise = petalsPS.noise;
        pNoise.enabled = true;
        pNoise.strength = 0.5f; // ΘÜÅµ£║µë¡µ¢▓Θú₧ΦíîΦ╖»τ║┐∩╝îσó₧σèáΦô¼µ¥╛τ⌐║µ░öµäƒ
        pNoise.frequency = 0.3f;

        var pRender = petalsPS.GetComponent<ParticleSystemRenderer>();
        pRender.renderMode = ParticleSystemRenderMode.Billboard;
        pRender.material = glowMat;

        var pEmis = petalsPS.emission;
        pEmis.rateOverTime = 0; 

        // ==========================================
        // 4. Θ╗äΦë▓τÄ»τ╗òΣ╕¥σ╖╛ (YellowScarf_PS)
        // ==========================================
        GameObject yellowObj = new GameObject("YellowScarf_PS");
        yellowObj.transform.SetParent(transform, false);
        yellowObj.transform.localPosition = Vector3.zero;
        yellowObj.transform.localScale = Vector3.one;

        yellowScarfPS = yellowObj.AddComponent<ParticleSystem>();
        var ysMain = yellowScarfPS.main;
        ysMain.loop = true;
        ysMain.startLifetime = 15f; 
        ysMain.startSpeed = 0f;
        ysMain.startSize = 0.0001f; 
        ysMain.startColor = new Color(1f, 0.9f, 0.2f, 1f);
        ysMain.simulationSpace = ParticleSystemSimulationSpace.World;
        ysMain.scalingMode = ParticleSystemScalingMode.Hierarchy;
        ysMain.maxParticles = 2; 

        var ysShape = yellowScarfPS.shape;
        ysShape.shapeType = ParticleSystemShapeType.Circle;
        ysShape.position = Vector3.up * wMinY; 
        ysShape.radius = canopyMaxHeight * 4.5f; 
        ysShape.arcMode = ParticleSystemShapeMultiModeValue.BurstSpread; 
        
        var ysVel = yellowScarfPS.velocityOverLifetime;
        ysVel.enabled = true;
        ysVel.orbitalY = 0.3f; 
        ysVel.y = (canopyMaxHeight * 2.0f) / 15f; 

        var ysNoise = yellowScarfPS.noise;
        ysNoise.enabled = true;
        ysNoise.strength = 1.0f / s; 
        ysNoise.frequency = 0.15f;  
        ysNoise.scrollSpeed = 0.2f;

        var ysColList = yellowScarfPS.colorOverLifetime;
        ysColList.enabled = false; 

        var ysTrails = yellowScarfPS.trails;
        ysTrails.enabled = true;
        ysTrails.ratio = 1.0f; 
        ysTrails.lifetimeMultiplier = 0.4f; 
        
        Gradient trailGrad = new Gradient();
        trailGrad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        ysTrails.colorOverTrail = trailGrad;

        var ysRender = yellowScarfPS.GetComponent<ParticleSystemRenderer>();
        ysRender.renderMode = ParticleSystemRenderMode.None; 
        ysRender.trailMaterial = glowMat;
        
        var ysEmis = yellowScarfPS.emission;
        ysEmis.rateOverTime = 0; 
        ysEmis.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 2, 2, 0, 15f) }); 

        // ==========================================
        // 5. Φ¥┤Φ¥╢Σ╕Äσ£ƒσúñ (Butterfly_PS, Soil_PS)
        // ==========================================
        CreateButterfliesAndSoil(s, glowMat);
    }

    void CreateButterfliesAndSoil(float s, Material glowMat)
    {
        // Φ¥┤Φ¥╢
        GameObject bf = new GameObject("Butterfly_PS");
        bf.transform.SetParent(transform, false);
        bf.transform.localPosition = Vector3.zero;
        bf.transform.localScale = Vector3.one; 
        
        butterfliesPS = bf.AddComponent<ParticleSystem>();
        var bMain = butterfliesPS.main;
        bMain.loop = true;
        bMain.startLifetime = new ParticleSystem.MinMaxCurve(6f, 12f); 
        bMain.startSpeed = 0f; 
        bMain.startSize = new ParticleSystem.MinMaxCurve(0.0003f, 0.0008f); 
        bMain.simulationSpace = ParticleSystemSimulationSpace.World;
        bMain.scalingMode = ParticleSystemScalingMode.Hierarchy;
        bMain.maxParticles = 8; // Γÿà Φ«╛τ╜«µ£ÇσñºσÉîµù╢σ¡ÿσ£¿µò░ΘçÅΣ╕║ 8 σÅ¬


        var bShape = butterfliesPS.shape;
        bShape.shapeType = ParticleSystemShapeType.Box; 
        float treeH = aMaxY - aMinY;
        bShape.position = Vector3.up * ((aMinY + aMaxY) / 2f); // Γÿà σ¢₧σ╜Æσê░σ▒àΣ╕¡µáæµ£¿Σ╜ìτ╜«
        bShape.scale = new Vector3(treeH * 0.8f, treeH * 0.8f, treeH * 0.8f); // Γÿà Σ╕ÇΣ╕¬Σ╕¡τ¡ëσñºσ░ÅτÜäτ½ïµû╣Σ╜ôσî║σƒƒ∩╝ü

        
        var bVel = butterfliesPS.velocityOverLifetime;
        bVel.enabled = true;
        // Γÿà σó₧σèáΘÜÅµ£║µ╕╕ΦìíΘÇƒσ║ª∩╝îσÅûµ╢êΦ┐çΣ║Äτöƒτí¼τÜäΦ╜¿ΘüôµùïΦ╜¼
        bVel.orbitalY = new ParticleSystem.MinMaxCurve(-0.1f, 0.1f); 
        bVel.x = new ParticleSystem.MinMaxCurve(-0.5f / s, 0.5f / s); 
        bVel.y = new ParticleSystem.MinMaxCurve(-0.3f / s, 0.3f / s); // Φ╜╗σ╛«Σ╕èΣ╕ïµ╡«σè¿
        bVel.z = new ParticleSystem.MinMaxCurve(-0.5f / s, 0.5f / s);

        var bNoise = butterfliesPS.noise;
        bNoise.enabled = true;
        bNoise.strength = 1.0f; // Γÿà σèáσàÑσ╝║τâêτÜä Noise Φ«⌐σà╢Θú₧ΦíîΦ╖»τ║┐µ¥éΣ╣▒πÇüΦç¬τä╢∩╝îσâÅτ£ƒµ¡úτÜäΦ¥┤Φ¥╢
        bNoise.frequency = 0.2f;   

        var bColList = butterfliesPS.colorOverLifetime;
        bColList.enabled = true;
        Gradient bGrad = new Gradient();
        bGrad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(1f, 0.8f), new GradientAlphaKey(0f, 1f) }
        );
        bColList.color = bGrad;

        var bTrails = butterfliesPS.trails;
        bTrails.enabled = false; // Γÿà σ╜╗σ║òσà│Θù¡µïûσ░╛∩╝üΦ┐Öσ░▒µÿ»Θéúσ¢óτÖ╜Φë▓σñºτ▓Æσ¡ÉτÜäτ£ƒσç╢∩╝ü

        var texAnim = butterfliesPS.textureSheetAnimation;
        texAnim.enabled = true; 
        texAnim.numTilesX = 3;  
        texAnim.numTilesY = 1;
        texAnim.animation = ParticleSystemAnimationType.WholeSheet; 
        texAnim.cycleCount = 15; // σñºσ╣àσó₧σèáµïìτ┐àΦåÇΘóæτÄç∩╝îΘú₧Φ╡╖µ¥Ñµ¢┤σÑ╜τ£ï


        var bRender = butterfliesPS.GetComponent<ParticleSystemRenderer>();
        bRender.renderMode = ParticleSystemRenderMode.Billboard;
        bRender.trailMaterial = glowMat; // µïûσ░╛µ¥ÉΦ┤¿∩╝ü
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

        // µ│Ñσ£ƒ
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
        mEmis.rateOverTime = 30f; // µ│Ñσ£ƒσºïτ╗êσ¡ÿσ£¿
    }

    // ==========================================
    // Σ║ñΣ║ÆΣ╕Äτè╢µÇüµ¢┤µû░
    // ==========================================

    void OnTriggerStay(Collider other)
    {
        // σÉæΣ╕èσ▒éτ║ºµÉ£τ┤ó∩╝ÜΦºúσå│µëïµƒäτó░µÆ₧Σ╜ôµîéσ£¿σ¡Éτë⌐Σ╜ôΣ╕è∩╝êµ»öσªéσÉìΣ╕║ Sphere Σ╕öµùáµáçτ¡╛∩╝ëσ»╝Φç┤µ╝ÅσêñτÜäΘù«Θóÿ
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
            // µ₧üσ║ªΣ┐¥σ«êτÜäσÉìσ¡ùσî╣Θàì∩╝îΘÿ▓µ¡óΦ»»Σ╝ñτÄ⌐σ«╢τÜä Gaze Interactor, Teleport Interactor µêû PlayerController
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

    private float healDebounceTimer = 0f;
    private bool magicAudioArmed = true;

    void Update()
    {
        // σ╜╗σ║òτº╗ΘÖñσÄƒσàêτÜä Camera Φç¬σè¿ΦºªσÅæΘóäµíêΣ╕Äσà¿σ£║µÖ»µëïΘâ¿σ»╣Φ▒íµÉ£τ┤óΘÇ╗Φ╛æ
        // σ¢áΣ╕║τö¿µê╖σ£║µÖ»Σ╕¡σ╖▓τ╗Åσ£¿τó░µÆ₧Σ╜ôΣ╕èτ▓╛σçåτ╗æσ«ÜΣ║å triggersπÇé
        // τÄ░σ£¿Σ╗àσ«îσà¿Σ╛¥Θ¥áτ£ƒσ«₧τÜäτë⌐Σ╜ôτó░µÆ₧ (µëïΘâ¿/µÄºσê╢σÖ¿ΦºªσÅæ OnTriggerStay)
        
        // Γÿà σ░Åσ┐â∩╝üOnTriggerStay µÿ»µîëτë⌐τÉåσ╕º (FixedUpdate) Φ╖æτÜä∩╝îΦÇî Update µîëµ╕▓µƒôσ╕ºΦ╖æ
        // Σ╝Üσ»╝Φç┤σªéµ₧£µ╕▓µƒôσ╕ºµ»öτë⌐τÉåσ╕ºσ┐½∩╝îσ░▒Σ╝Üµ╝ÅµÄëσ»╝Φç┤τ₧¼Θù┤σêñµû¡µêÉ false∩╝îΣ╗ÄΦÇîτû»τïéΘçìσÉ»Θƒ│Σ╣Éσìíσú│πÇé
        // σ┐àΘí╗σèáσàÑ 0.1 τºÆτÜäΘÿ▓µèûµ╗ñµ│ó∩╝ü
        if (triggerOverlapDetected)
        {
            healDebounceTimer = 0.1f;
        }
        
        bool isHealing = (healDebounceTimer > 0f);
        
        if (healDebounceTimer > 0f) healDebounceTimer -= Time.deltaTime;
        
        triggerOverlapDetected = false;

        // Γÿà µÄºσê╢ Magic Θƒ│µòêτÜäΓÇ£µ¡ªΦúàΓÇ¥τè╢µÇü∩╝Ü
        // σÅ¬µ£ëσÇÆΘÇÇσ¢₧σ╜╗σ║òµ₧»µ¡╗τè╢µÇü∩╝îµëìΘçìµû░σàüΦ«╕µÆ¡µö╛
        if (energyLevel <= 0.01f) magicAudioArmed = true;
        // σªéµ₧£σ╜╗σ║òµ╗íΣ║å∩╝êµ▓╗µäêσ«îµ»ò∩╝ë∩╝îτ¢┤µÄÑτ╝┤µó░
        if (energyLevel >= 1.0f) magicAudioArmed = false;

        if (isHealing)
        {
            // σªéµ₧£Σ╕èΣ╕ÇΣ╕¬τ₧¼Θù┤µ▓íµæ╕∩╝îΦ┐ÖΣ╕¬τ₧¼Θù┤σêÜµæ╕Σ╕è∩╝îσ╣╢Σ╕öσ╜ôσëìµÿ»Φó½σàüΦ«╕µÆ¡Θ¡öµ│òτÜäΘÿ╢µ«╡
            if (!wasHealing && magicHealAudio != null && magicAudioArmed)
            {
                magicHealAudio.time = 0f;
                if (!magicHealAudio.isPlaying) magicHealAudio.Play();
            }

            energyLevel += healingRate * Time.deltaTime;
            healLingerTimer = 60.0f; // Γÿà σ╜ôµëïµæ╕τ¥ÇτÜäµù╢σÇÖ∩╝îτ╗┤µîü 60 τºÆτÜäτ¡ëσ╛àµù╢Θù┤∩╝ê1σêåΘÆƒµëìσ╝Çσºïσ╣▓µ₧»∩╝ë


            if (showDebugDistance) Debug.Log($"[TreeAudio] Healing! energy: {energyLevel:F2}, magicVol: {magicCurrentVolume:F2}");

            // Γÿà σÅ¬µ£ë Armed τè╢µÇü∩╝îΣ╕öσ£¿µæ╕τ¥Ç∩╝îµëìµ╖íσàÑΘƒ│ΘçÅ
            if (magicAudioArmed)
            {
                magicCurrentVolume = Mathf.MoveTowards(magicCurrentVolume, magicVolume, Time.deltaTime * 10f);
            }
            else
            {
                // σªéµ₧£σ╖▓τ╗Åµ╗íΣ║åΦó½τ╝┤µó░Σ║å∩╝îσì│Σ╜┐µëïΦ┐ÿµæ╕τ¥Ç∩╝îΣ╣ƒσ┐½ΘÇƒµ╖íσç║σú░Θƒ│
                magicCurrentVolume = Mathf.MoveTowards(magicCurrentVolume, 0f, Time.deltaTime * 5f);
            }
        }
        else
        {
            // Γÿà τª╗σ╝ÇσÉÄσ┐½ΘÇƒµ╖íσç║
            magicCurrentVolume = Mathf.MoveTowards(magicCurrentVolume, 0f, Time.deltaTime * 5f);
            
            // σ«îσà¿µ▓íσú░Σ║åσ░▒µÜéσü£σ╝òµôÄ∩╝îΦèéτ£üµÇºΦâ╜
            if (magicCurrentVolume == 0f && magicHealAudio != null && magicHealAudio.isPlaying)
            {
                magicHealAudio.Pause();
            }

            if (healLingerTimer > 0f)
            {
                healLingerTimer -= Time.deltaTime;
            }
            else
            {
                energyLevel -= decayRate * Time.deltaTime;
            }
        }

        // Γÿà µ»Åσ╕ºτ¢┤µÄÑΦ«╛τ╜«Θ¡öµ│òΘƒ│ΘçÅ∩╝îτ«ÇσìòσÅ»Θ¥á
        if (magicHealAudio != null)
        {
            magicHealAudio.volume = magicCurrentVolume;
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

        // 2A. Θ╗äΦë▓Σ╕¥σ╕ª
        var ysEmis = yellowScarfPS.emission;
        if (energyLevel >= 0.95f || (energyLevel > 0f && isHealing))
        {
            if (!ysEmis.enabled) { ysEmis.enabled = true; yellowScarfPS.Play(); } 
        }
        else
        {
            ysEmis.enabled = false;
        }

        // 3. µ╗íτè╢µÇüΦºªσÅæτë╣µòê∩╝Üτ▓ëΦë▓ΦÉ╜Φè▒ & Φ¥┤Φ¥╢ & Θ╕ƒσÅ½σ╛¬τÄ»
        if (energyLevel >= 1.0f && !fullyHealedTriggered)
        {
            fullyHealedTriggered = true;
            // σ╝ÇσÉ»Θ╕ƒσÅ½ΘÜÅµ£║σ╛¬τÄ»
            if (birdAudio != null && birdCoroutine == null)
            {
                birdAudio.volume = birdVolume;
                birdCoroutine = StartCoroutine(RandomBirdRoutine());
            }
        }
        else if (energyLevel < 0.95f && fullyHealedTriggered)
        {
            fullyHealedTriggered = false;
            // σü£µ¡óΘ╕ƒσÅ½σ╛¬τÄ»σ╣╢µ╖íσç║Θƒ│ΘçÅ
            if (birdCoroutine != null) { StopCoroutine(birdCoroutine); birdCoroutine = null; }
            if (birdAudio != null && birdAudio.isPlaying) StartCoroutine(FadeOutBirdAudio());
        }

        // Θÿ▓σ╛íµÇºµúÇµƒÑ∩╝Üτí«Σ┐¥σú░Θƒ│µ▓íµ£ëΦó½Θ¥ÖΘ╗ÿ
        if (birdAudio != null) birdAudio.ignoreListenerPause = true;
        if (magicHealAudio != null) magicHealAudio.ignoreListenerPause = true;

        // Σ┐¥µîüΘúÿΦÉ╜τë╣µòêτè╢µÇü∩╝êσ»åΘ¢åτÜäτƒ¡Φ╖¥µé¼µ╡«Φè▒τ░ç∩╝ë
        var pEmis = petalsPS.emission;
        if (energyLevel >= 0.95f)
        {
            pEmis.rateOverTime = 800f; // Γÿà τêåσÅæσ╝Åσó₧σèá∩╝îσ╜óµêÉσâÅΦè▒Σ╕Çµá╖Σ╕Çτ░çτ░çµ₧üσà╢σ»åΘ¢åτÜäσêåσ╕â
        }
        else
        {
            pEmis.rateOverTime = 0f;
        }

        var bEmis = butterfliesPS.emission;
        bEmis.rateOverTime = (energyLevel >= 0.95f) ? 1.0f : 0f; // Γÿà τòÑσ╛«µÅÉΘ½ÿσÅæσ░äΘÇƒτÄç∩╝îΣ╗Ñµ╗íΦ╢│µ╗íσ▒Åµ£ÇσñÜσ¡ÿσ£¿ 5-8 σÅ¬τÜäΦªüµ▒é
    }

    IEnumerator FadeOutBirdAudio()
    {
        float startVol = birdAudio.volume;
        float elapsed = 0f;
        while(elapsed < 1.0f && birdAudio != null)
        {
            elapsed += Time.deltaTime;
            birdAudio.volume = Mathf.Lerp(startVol, 0f, elapsed);
            yield return null;
        }
        if (birdAudio != null) birdAudio.Stop();
    }

    IEnumerator RandomBirdRoutine()
    {
        while (true)
        {
            // Γÿà Φ┐Öσ░▒µÿ»µ╡╖µ┤ïµ╡╖Θ╕ÑΘéúσÑùΘù┤ΘÜöτ«ùµ│ò∩╝Üµ»Åµ¼íτ╗¥σ»╣Θ¥ÖΘƒ│τ¡ëσ╛àΦ┐ÖΣ╣êΘò┐µù╢Θù┤
            float wait = Random.Range(minBirdInterval, maxBirdInterval);
            yield return new WaitForSeconds(wait);

            if (birdAudio != null && birdAudio.clip != null)
            {
                birdAudio.pitch = Random.Range(0.9f, 1.1f);
                birdAudio.volume = birdVolume; // σ«₧µù╢σÉîµ¡ÑΦ░âΘƒ│
                birdAudio.PlayOneShot(birdAudio.clip);
                
                // Γÿà µ₧üσà╢σà│Θö«∩╝Üτ¡ëΦ»Ñµ¼íΘ╕ƒσÅ½σ╜╗σ║òσ«îσà¿µÆ¡µö╛τ╗ôµ¥ƒΣ╗ÑσÉÄ∩╝îσåìσ¢₧σÄ╗Φ┐¢ΦíîΣ╕ïΣ╕ÇΦ╜«τÜäτ║»Θ¥ÖΘ╗ÿτ¡ëσ╛àσÇÆµò░∩╝ü
                // Φ┐Öµá╖τ╗¥σ»╣Σ╕ìΣ╝ÜσÅæτöƒσêÜσÅ½σ«îΣ╕ÇτºÆσÅêσÅ½πÇüτöÜΦç│Σ╕ñσú░Θ╕ƒσÅ½ΘçìσÅáσ£¿Σ╕ÇΦ╡╖Σ╣▒µêÉΣ╕ÇΘöàτ▓ÑτÜäµâàσå╡
                yield return new WaitForSeconds(birdAudio.clip.length);
            }
        }
    }

    void LateUpdate()
    {
        // 1. µ₧»µáæΘ¬¿µ₧╢τÜäΣ╕ôσ▒₧µë½µÅÅΘ½ÿσ║ª
        float wLimitY = Mathf.Lerp(wMinY, wMaxY, energyLevel);
        // 2. µ┤╗µáæµ¿íσ₧ïτÜäΣ╕ôσ▒₧µë½µÅÅΘ½ÿσ║ª
        float aLimitY = Mathf.Lerp(aMinY, aMaxY, energyLevel);

        // 1. µ₧»µáæτ▓Æσ¡É∩╝ÜσÉæΣ╕èµë½σàëµ╢êµòú
        if (witheredPS != null && witheredPS.isPlaying && witheredPS.particleCount > 0)
        {
            int count = witheredPS.GetParticles(pBuffer);
            for (int i = 0; i < count; i++)
            {
                if (pBuffer[i].position.y < wLimitY)
                    pBuffer[i].remainingLifetime = -1f; // σ╖▓µ▓╗µäêσî║σƒƒ∩╝îµ₧»µáæτ½ïσê╗µ╢êσñ▒
            }
            witheredPS.SetParticles(pBuffer, count);
        }

        // 2. µ┤╗µáæτ▓Æσ¡É∩╝Üτ║»τ▓╣Σ╜┐τö¿µ┤╗τ¥ÇτÜäσñºµáæµ¿íσ₧ï∩╝îτö▒Θ½ÿµ₧üσÇ╝σè¿µÇüΦ┐¢ΦíîΘó£Φë▓µ╕ÉσÅÿΦ«íτ«ù
        if (alivePS != null && alivePS.isPlaying && alivePS.particleCount > 0)
        {
            int count = alivePS.GetParticles(pBuffer);
            
            // 3µ«╡σ╝Åσ«îτ╛ÄΦë▓σ╜⌐µ╕ÉσÅÿ∩╝Üµá╣Θâ¿(µúò) -> µáæσ┐â(τ╗┐) -> µáæσåáσà¿τ▓ë(Pink)
            // Φ«⌐σñºΘçÅτ▓ëΦë▓τ▓Æσ¡Éσ£¿µáæσåáΣ╕èΘ¥Öµ¡óΘÖäτ¥Ç∩╝îσ«îτ╛ÄσÑæσÉêµƒ│τ╡«ΘúÿΦÉ╜µ░¢σ¢┤
            float trunkLine = aMinY + (aMaxY - aMinY) * 0.30f;
            float leafLine = aMinY + (aMaxY - aMinY) * 0.65f;
            float petalLine = aMinY + (aMaxY - aMinY) * 0.85f;

            for (int i = 0; i < count; i++)
            {
                float y = pBuffer[i].position.y;
                if (y > aLimitY)
                {
                    pBuffer[i].remainingLifetime = -1f; // Φ┐ÿµ▓íµ▓╗µäêσê░τÜäσî║σƒƒµèæσê╢µ┤╗µáæ
                }
                else
                {
                    // σ«îτ╛ÄτÜäσè¿µÇüµÅÆσÇ╝Σ╕ëµ«╡Φë▓σ╜⌐µ╕ÉσÅÿ
                    if (y <= trunkLine)
                        pBuffer[i].startColor = aliveTrunkColor;
                    else if (y <= leafLine)
                        pBuffer[i].startColor = Color.Lerp(aliveTrunkColor, aliveLeafColor, (y - trunkLine) / (leafLine - trunkLine));
                    else if (y <= petalLine)
                        pBuffer[i].startColor = Color.Lerp(aliveLeafColor, pinkPetalColor, (y - leafLine) / (petalLine - leafLine));
                    else
                        pBuffer[i].startColor = pinkPetalColor; // µáæσåáµ╗íτ▓ë
                }
            }
            alivePS.SetParticles(pBuffer, count);
        }
    }
}

/// <summary>
/// Φç¬σè¿µîéΦ╜╜σ£¿σ╕ªµ£ë Rigidbody Σ╕öµÿ» Trigger τÜäσ¡Éτë⌐Σ╜ô∩╝êσªéµáæµ₧¥σ░ÅτÉâ∩╝ëΣ╕èπÇé
/// Φºúσå│ Unity τë⌐τÉåσ╝òµôÄΣ╕¡∩╝Üσ¡Éτë⌐Σ╜ôσ╕ªµ£ëτï¼τ½ï Rigidbody µù╢∩╝îτó░µÆ₧Σ║ïΣ╗╢Σ╕ìΣ╝ÜσåÆµ│íτ╗Öτê╢τë⌐Σ╜ôΦäÜµ£¼τÜäΘù«ΘóÿπÇé
/// </summary>
public class TreeTriggerForwarder : MonoBehaviour
{
    public ParticleTreeHealer parentHealer;

    void OnTriggerStay(Collider other)
    {
        if (parentHealer == null) return;
        
        // σÉæΣ╕èσ▒éτ║ºµÉ£τ┤ó∩╝ÜΦºúσå│µëïµƒäτó░µÆ₧Σ╜ôµîéσ£¿σ¡Éτë⌐Σ╜ôΣ╕èσ»╝Φç┤µ╝ÅσêñτÜäΘù«Θóÿ
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
            // µ₧üσ║ªΣ┐¥σ«êτÜäσÉìσ¡ùσî╣Θàì∩╝îΘÿ▓µ¡óΦ»»Σ╝ñτÄ⌐σ«╢τÜä Gaze Interactor, Teleport Interactor µêû PlayerController
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
