using UnityEngine;
using UnityEngine.XR.Hands; 
using UnityEngine.XR.Management;
using System.Collections.Generic;

/// <summary>
/// Tube Hand Version with Aggressive Hiding.
/// Ensures standard controllers are hidden in ALL scenes automatically.
/// </summary>
public class GhostHandVisualizer : MonoBehaviour
{
    public XRHandSubsystem handSubsystem;
    
    [Header("Visual Settings")]
    public Material handMaterial; 
    public float handRadius = 0.012f; 
    public float handScale = 1.0f;    

    [Header("Hiding (Auto-finds if null)")]
    public List<GameObject> manualHideList = new List<GameObject>();

    [Header("Tracking")]
    public Transform xrOrigin;

    private class HandVisuals
    {
        public GameObject root;
        public List<LineRenderer> fingers = new List<LineRenderer>();
        public List<GameObject> fingertipColliders = new List<GameObject>(); 
    }

    private HandVisuals leftHandVisuals;
    private HandVisuals rightHandVisuals;

    private readonly XRHandJointID[][] fingerChains = new XRHandJointID[][]
    {
        new XRHandJointID[] { XRHandJointID.Wrist, XRHandJointID.ThumbMetacarpal, XRHandJointID.ThumbProximal, XRHandJointID.ThumbDistal, XRHandJointID.ThumbTip },
        new XRHandJointID[] { XRHandJointID.Wrist, XRHandJointID.IndexProximal, XRHandJointID.IndexIntermediate, XRHandJointID.IndexDistal, XRHandJointID.IndexTip },
        new XRHandJointID[] { XRHandJointID.Wrist, XRHandJointID.MiddleProximal, XRHandJointID.MiddleIntermediate, XRHandJointID.MiddleDistal, XRHandJointID.MiddleTip },
        new XRHandJointID[] { XRHandJointID.Wrist, XRHandJointID.RingProximal, XRHandJointID.RingIntermediate, XRHandJointID.RingDistal, XRHandJointID.RingTip },
        new XRHandJointID[] { XRHandJointID.Wrist, XRHandJointID.LittleProximal, XRHandJointID.LittleIntermediate, XRHandJointID.LittleDistal, XRHandJointID.LittleTip }
    };

    void Start()
    {
        CreateMaterial();
        GetHandSubsystem();
        
        leftHandVisuals = CreateHandVisuals("LeftHandTube");
        rightHandVisuals = CreateHandVisuals("RightHandTube");

        if (xrOrigin == null)
        {
            var origin = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
            if (origin != null) xrOrigin = origin.transform;
        }
    }

    void CreateMaterial()
    {
        if (handMaterial == null)
        {
            Shader s = Shader.Find("Universal Render Pipeline/Unlit");
            if (s == null) s = Shader.Find("Sprites/Default"); 
            if (s != null)
            {
                handMaterial = new Material(s);
                Color ghostColor = new Color(0.4f, 0.7f, 1f, 0.2f);
                handMaterial.SetColor("_BaseColor", ghostColor);
                handMaterial.color = ghostColor;
                handMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                handMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                handMaterial.SetInt("_ZWrite", 0);
                handMaterial.renderQueue = 3000;
            }
        }
    }

    HandVisuals CreateHandVisuals(string name)
    {
        HandVisuals visuals = new HandVisuals();
        visuals.root = new GameObject(name);
        visuals.root.transform.parent = transform;

        for (int i = 0; i < 5; i++)
        {
            GameObject fingerObj = new GameObject($"Finger_{i}");
            fingerObj.transform.parent = visuals.root.transform;
            
            LineRenderer lr = fingerObj.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.startWidth = handRadius * 2f; 
            lr.endWidth = handRadius * 1.5f; 
            lr.material = handMaterial;
            lr.positionCount = 5;
            lr.numCapVertices = 8;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            visuals.fingers.Add(lr);

            GameObject tipObj = new GameObject($"Tip_{i}");
            tipObj.transform.parent = fingerObj.transform;
            tipObj.tag = "HandFingertip"; // 交互标签
            tipObj.layer = 2; // Ignore Raycast
            
            var col = tipObj.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = handRadius * 1.5f; 
            
            var rb = tipObj.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            
            // 绑定交互触发器
            var trigger = tipObj.AddComponent<HandTriggerFeedback>();
            visuals.fingertipColliders.Add(tipObj);
        }
        return visuals;
    }

    void GetHandSubsystem()
    {
        var subs = new List<XRHandSubsystem>();
        SubsystemManager.GetInstances(subs);
        if (subs.Count > 0) handSubsystem = subs[0];
    }

    void Update()
    {
        // 强力隐藏旧模型
        KillOldVisuals();

        if (handSubsystem != null)
        {
            UpdateHand(handSubsystem.leftHand, leftHandVisuals);
            UpdateHand(handSubsystem.rightHand, rightHandVisuals);
        }
    }

    void KillOldVisuals()
    {
        foreach (var obj in manualHideList) if (obj != null) obj.SetActive(false);
        
        var controllers = FindObjectsOfType<UnityEngine.XR.Interaction.Toolkit.ActionBasedController>();
        foreach (var ctrl in controllers)
        {
            foreach (Transform child in ctrl.transform)
            {
                string n = child.name.ToLower();
                if ((n.Contains("visual") || n.Contains("model") || n.Contains("hand")) && !n.Contains("ray"))
                {
                    if (child.gameObject.activeSelf) child.gameObject.SetActive(false);
                }
            }
        }
    }

    void UpdateHand(XRHand hand, HandVisuals visuals)
    {
        bool tracked = hand.isTracked;
        visuals.root.SetActive(tracked);
        if (!tracked) return;

        for (int i = 0; i < 5; i++)
        {
            XRHandJointID[] chain = fingerChains[i];
            for (int k = 0; k < chain.Length; k++)
            {
                if (hand.GetJoint(chain[k]).TryGetPose(out Pose pose))
                {
                    Vector3 worldPos = xrOrigin != null ? xrOrigin.TransformPoint(pose.position) : pose.position;
                    visuals.fingers[i].SetPosition(k, worldPos);
                    if (k == chain.Length - 1) visuals.fingertipColliders[i].transform.position = worldPos;
                }
            }
        }
    }
}

// 交互桥梁
public class HandTriggerFeedback : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        other.GetComponent<TreeHealer>()?.ReceiveTouch();
        other.GetComponent<CatInteract>()?.ReceiveTouch();
    }
    void OnTriggerStay(Collider other)
    {
        other.GetComponent<TreeHealer>()?.ReceiveTouch();
        other.GetComponent<CatInteract>()?.ReceiveTouch();
    }
}
