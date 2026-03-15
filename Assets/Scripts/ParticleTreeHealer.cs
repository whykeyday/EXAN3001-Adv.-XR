using UnityEngine;

public class ParticleTreeHealer : MonoBehaviour
{
    [Header("Mesh References (Swappers)")]
    public Mesh witheredMesh;
    public Mesh aliveMesh;

    [Header("Colors & Sizings")]
    public Color witheredColor = new Color(0.48f, 0.28f, 0.08f, 0.9f);
    public float witheredParticleSize = 0.0002f; 
    public float witheredEmissionRate = 120000f; 

    [Header("Gradient Colors (Alive State)")]
    public Color trunkColor = new Color(0.48f, 0.28f, 0.08f, 0.9f);
    public Color branchColor = new Color(0.6f, 0.8f, 0.2f, 0.9f);
    public Color tipColor = new Color(0.1f, 0.85f, 0.2f, 0.95f);

    [Header("Falling Sakura Effect")]
    public Color fallingSakuraColor = new Color(1.0f, 0.75f, 0.8f, 0.9f);

    [Header("Mesh Morph Manual Calibration")]
    public Vector3 aliveMeshPositionOffset = Vector3.zero;
    public Vector3 aliveMeshRotationOffset = new Vector3(-90f, 0f, 0f);
    public float aliveMeshScaleMultiplier = 1.0f;

    [Header("Withered Breathing Jitter")]
    public float jitterSpeed = 0.4f;
    public float jitterAmount = 0.0012f;

    public float aliveParticleSize = 0.0008f; 
    public float aliveEmissionRate = 200000f; 
    [Header("Interact Settings")]
    public float healingRate = 0.5f;
    public float decayRate = 0.1f;
    
    [Header("Falling Speed (Snowflake Downward)")]
    public float fallingSpeed = -0.15f; 
    public float canopyMaxHeight = 15.0f; 

    [Range(0f, 1f)]
    public float energyLevel = 0f;

    [Header("Visual Mesh Backing Options")]
    public bool enableVisualMesh = true;
    public Material witheredMaterial;
    public Material aliveMaterial;

    private GameObject visualNode;
    private MeshFilter visualMF;
    private MeshRenderer visualMR;

    private ParticleSystem ps;
    private ParticleSystem.Particle[] pBuffer;
    private Collider treeCollider;
    private bool triggerOverlapDetected = false;

    private System.Collections.Generic.List<GameObject> cachedHands = new System.Collections.Generic.List<GameObject>();
    private float scanTimer = 0f;

    void Start()
    {
        ps = GetComponent<ParticleSystem>();
        treeCollider = GetComponent<Collider>();
        energyLevel = 0f;

        if (ps != null)
        {
            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            
            var shape = ps.shape;
            shape.mesh = witheredMesh; 
        }

        // --- Create Automatic Visual Mesh Backing ---
        if (enableVisualMesh)
        {
            Transform child = transform.Find("VisualMeshBacking");
            if (child == null)
            {
                visualNode = new GameObject("VisualMeshBacking");
                visualNode.transform.SetParent(this.transform);
            }
            else
            {
                visualNode = child.gameObject;
            }

            visualNode.transform.localPosition = Vector3.zero;
            visualNode.transform.localRotation = Quaternion.identity;
            visualNode.transform.localScale = Vector3.one;

            visualMF = visualNode.GetComponent<MeshFilter>();
            if (visualMF == null) visualMF = visualNode.AddComponent<MeshFilter>();

            visualMR = visualNode.GetComponent<MeshRenderer>();
            if (visualMR == null) visualMR = visualNode.AddComponent<MeshRenderer>();

            visualMF.sharedMesh = witheredMesh;
            if (witheredMaterial != null) visualMR.sharedMaterial = witheredMaterial;
        }

        ApplyTreeState(); 
    }

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("PlayerHand") || other.CompareTag("Player") || other.name.ToLower().Contains("hand") || other.name.ToLower().Contains("controller"))
        {
            triggerOverlapDetected = true;
            energyLevel += healingRate * Time.deltaTime * 1.5f; 
        }
    }

    void Update()
    {
        scanTimer += Time.deltaTime;
        if (scanTimer > 0.5f || cachedHands.Count == 0)
        {
            scanTimer = 0f;
            cachedHands.Clear();
            var allTransforms = FindObjectsByType<Transform>(FindObjectsSortMode.None);
            foreach (var t in allTransforms)
            {
                if (t.CompareTag("PlayerHand") || t.CompareTag("Player") || t.name.ToLower().Contains("hand") || t.name.ToLower().Contains("controller"))
                    cachedHands.Add(t.gameObject);
            }
        }

        bool isHealing = triggerOverlapDetected;
        triggerOverlapDetected = false;
        
        if (!isHealing && treeCollider != null)
        {
            foreach (var hand in cachedHands)
            {
                if (hand != null && Vector3.Distance(hand.transform.position, treeCollider.ClosestPoint(hand.transform.position)) < 1.5f)
                {
                    isHealing = true;
                    break;
                }
            }
        }

        if (isHealing) energyLevel += healingRate * Time.deltaTime; 
        else energyLevel -= decayRate * Time.deltaTime;

        energyLevel = Mathf.Clamp01(energyLevel);
        ApplyTreeState();
    }

    private void ApplyTreeState()
    {
        if (ps == null) return;

        var shape = ps.shape;
        var emission = ps.emission;

        bool isGrown = energyLevel > 0.5f;

        // Swap Mesh shape module
        if (isGrown)
        {
            if (shape.mesh != aliveMesh && aliveMesh != null)
            {
                shape.mesh = aliveMesh;
                shape.position = aliveMeshPositionOffset;
                shape.rotation = aliveMeshRotationOffset;
                shape.scale = Vector3.one * aliveMeshScaleMultiplier;
                emission.rateOverTime = aliveEmissionRate;
            }

            // Sync Visual Mesh
            if (enableVisualMesh && visualNode != null)
            {
                visualNode.SetActive(true); // Show visual mesh node for alive tree
                visualMF.sharedMesh = aliveMesh;
                if (aliveMaterial != null) visualMR.sharedMaterial = aliveMaterial;

                visualNode.transform.localPosition = aliveMeshPositionOffset;
                visualNode.transform.localRotation = Quaternion.Euler(aliveMeshRotationOffset);
                visualNode.transform.localScale = Vector3.one * aliveMeshScaleMultiplier;
            }
        }
        else
        {
            if (shape.mesh != witheredMesh && witheredMesh != null)
            {
                shape.mesh = witheredMesh;
                shape.position = Vector3.zero;
                shape.rotation = Vector3.zero;
                shape.scale = Vector3.one;
                emission.rateOverTime = witheredEmissionRate;
            }

            // Sync Visual Mesh
            if (enableVisualMesh && visualNode != null)
            {
                visualNode.SetActive(false); // Hide completely transparent dead tree mesh backup
            }
        }
    }

    void LateUpdate()
    {
        if (ps == null) return;

        int max = ps.main.maxParticles;
        if (pBuffer == null || pBuffer.Length < max) pBuffer = new ParticleSystem.Particle[max];
        int count = ps.GetParticles(pBuffer);

        float treeBaseY = transform.position.y;
        float scaledCanopyMaxHeight = canopyMaxHeight * transform.lossyScale.y;

        bool isGrown = energyLevel > 0.5f;

        float waveHeight = energyLevel * canopyMaxHeight;

        for (int i = 0; i < count; i++)
        {
            float yPos = pBuffer[i].position.y;
            float heightRatio = Mathf.InverseLerp(0f, canopyMaxHeight, yPos);

            // True Healing Wave Logic: Particles below Wave move up
            bool isAliveParticle = yPos < waveHeight && energyLevel > 0.05f;

            if (isAliveParticle)
            {
                pBuffer[i].startSize = aliveParticleSize;

                // Gradient: Trunk -> Branch -> Tip
                Color particleColor = trunkColor;
                if (heightRatio < 0.25f)
                {
                    particleColor = trunkColor;
                }
                else if (heightRatio < 0.65f)
                {
                    float t = Mathf.InverseLerp(0.25f, 0.65f, heightRatio);
                    particleColor = Color.Lerp(trunkColor, branchColor, t);
                }
                else
                {
                    float t = Mathf.InverseLerp(0.65f, 1.0f, heightRatio);
                    particleColor = Color.Lerp(branchColor, tipColor, t);
                }

                // Falling Leaves Logic (Only tips fall, above 65% height, when shape has swapped)
                bool wasFalling = pBuffer[i].velocity.y < (fallingSpeed * 0.5f);
                bool canFall = heightRatio > 0.65f && energyLevel > 0.5f;

                if (canFall || wasFalling)
                {
                    Vector3 vel = pBuffer[i].velocity;
                    
                    // --- AUTOMATIC TWEAK: Divide by Scale so raw slider speed maps to real World-meters/sec ---
                    float scaleY = transform.lossyScale.y;
                    if (scaleY <= 0) scaleY = 1.0f;
                    
                    vel.y = fallingSpeed / scaleY;
                    
                    // Sway effect (also divide by scale to maintain visual sway size)
                    vel.x = (Mathf.Sin(Time.time * 2f + i) * 0.25f) / scaleY; 
                    vel.z = (Mathf.Cos(Time.time * 2f + i * 1.3f) * 0.25f) / scaleY;
                    pBuffer[i].velocity = vel;

                    // Sakura pink color on falling leaves
                    Color sakuraColor = Color.Lerp(particleColor, fallingSakuraColor, 0.85f);
                    
                    // --- AUTOMATIC TWEAK: Dissipate smoothly as it falls over height difference ---
                    if (wasFalling)
                    {
                        float startFallHeight = 0.65f; 
                        float fallDepth = startFallHeight - heightRatio;
                        float opacity = Mathf.Clamp01(1.0f - (fallDepth / 0.25f)); // Fully fades after dropping 25% height
                        sakuraColor.a *= opacity;
                        
                        // Kill once fully invisible to maintain memory efficiency
                        if (opacity <= 0.01f) pBuffer[i].remainingLifetime = 0f;
                    }
                    pBuffer[i].startColor = sakuraColor;
                }
                else
                {
                    pBuffer[i].startColor = particleColor;
                    
                    // Connected/Breathing to branch, clear falling force if any
                    Vector3 vel = pBuffer[i].velocity;
                    if (vel.y < 0) vel.y = 0; 
                    pBuffer[i].velocity = vel;
                }
            }
            else
            {
                // Withered / Default State
                Color c = witheredColor;
                if (isGrown) c.a = 0f; // Hide top brown particles when tree has grown!
                
                pBuffer[i].startColor = c;
                pBuffer[i].startSize = witheredParticleSize;

                // --- AUTOMATIC TWEAK: Restore slow Breathing Jitter calibrated to scale ---
                float scaleY = transform.lossyScale.y;
                if (scaleY <= 0) scaleY = 1.0f;

                Vector3 jitter = new Vector3(
                    (Mathf.Sin(Time.time * jitterSpeed + i) * jitterAmount) / scaleY,
                    (Mathf.Cos(Time.time * jitterSpeed + i * 1.5f) * jitterAmount) / scaleY,
                    (Mathf.Sin(Time.time * jitterSpeed + i * 0.7f) * jitterAmount) / scaleY
                );

                pBuffer[i].velocity = jitter; 
            }
        }
        ps.SetParticles(pBuffer, count);
    }
}
