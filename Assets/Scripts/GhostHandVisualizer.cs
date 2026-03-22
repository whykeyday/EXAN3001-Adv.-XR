using UnityEngine;
using UnityEngine.XR.Hands; 
using UnityEngine.XR.Management;
using System.Collections.Generic;

public class GhostHandVisualizer : MonoBehaviour
{
    public XRHandSubsystem handSubsystem;
    
    [Header("Visual Settings")]
    public Material handMaterial; // Assign "ChalkGhost" material here
    public float handRadius = 0.035f; // Radius of the tube (3.5cm base)
    public float handScale = 1.8f;    // Global Scale Multiplier (1.8x)

    [Header("Controller Visuals (拿手柄时隐藏手)")]
    [Tooltip("把你的左手柄模型拖到这里")]
    public GameObject leftControllerVisual;
    [Tooltip("把你的右手柄模型拖到这里")]
    public GameObject rightControllerVisual;

    // Data Structures for tracking instances
    private class HandVisuals
    {
        public GameObject root;
        public List<LineRenderer> fingers = new List<LineRenderer>();
    }

    private HandVisuals leftHandVisuals;
    private HandVisuals rightHandVisuals;

    // Finger Joint Chains (All start at Wrist to fan out)
    private readonly XRHandJointID[][] fingerChains = new XRHandJointID[][]
    {
        new XRHandJointID[] { XRHandJointID.Wrist, XRHandJointID.ThumbMetacarpal, XRHandJointID.ThumbProximal, XRHandJointID.ThumbDistal, XRHandJointID.ThumbTip }, // Thumb (5 points)
        new XRHandJointID[] { XRHandJointID.Wrist, XRHandJointID.IndexProximal, XRHandJointID.IndexIntermediate, XRHandJointID.IndexDistal, XRHandJointID.IndexTip }, // Index (5 points)
        new XRHandJointID[] { XRHandJointID.Wrist, XRHandJointID.MiddleProximal, XRHandJointID.MiddleIntermediate, XRHandJointID.MiddleDistal, XRHandJointID.MiddleTip }, // Middle (5 points)
        new XRHandJointID[] { XRHandJointID.Wrist, XRHandJointID.RingProximal, XRHandJointID.RingIntermediate, XRHandJointID.RingDistal, XRHandJointID.RingTip }, // Ring (5 points)
        new XRHandJointID[] { XRHandJointID.Wrist, XRHandJointID.LittleProximal, XRHandJointID.LittleIntermediate, XRHandJointID.LittleDistal, XRHandJointID.LittleTip } // Pinky (5 points)
    };

    void Start()
    {
        CreateMaterial();
        GetHandSubsystem();
        
        leftHandVisuals = CreateHandVisuals("LeftHandTube");
        rightHandVisuals = CreateHandVisuals("RightHandTube");
    }

    void CreateMaterial()
    {
        if (handMaterial == null)
        {
            Shader s = Shader.Find("Custom/ChalkGhost");
            if (s == null) s = Shader.Find("Sprites/Default"); 

            if (s != null)
            {
                handMaterial = new Material(s);
                if (s.name == "Custom/ChalkGhost")
                {
                    handMaterial.SetColor("_MainColor", new Color(1f, 1f, 1f, 0.3f));
                    handMaterial.SetColor("_RimColor", Color.white);
                    handMaterial.SetFloat("_RimPower", 2.0f);
                    handMaterial.SetFloat("_Transparency", 0.3f);
                }
                else
                {
                    handMaterial.color = new Color(1f, 1f, 1f, 0.4f);
                }
            }
        }
    }

    HandVisuals CreateHandVisuals(string name)
    {
        HandVisuals visuals = new HandVisuals();
        visuals.root = new GameObject(name);
        visuals.root.transform.parent = transform;
        visuals.root.transform.localPosition = Vector3.zero;
        visuals.root.transform.localRotation = Quaternion.identity;
        visuals.root.transform.localScale = Vector3.one;

        // Create 5 LineRenderers (Th, In, Mi, Ri, Li)
        for (int i = 0; i < 5; i++)
        {
            GameObject fingerObj = new GameObject($"Finger_{i}");
            fingerObj.transform.parent = visuals.root.transform;
            
            LineRenderer lr = fingerObj.AddComponent<LineRenderer>();
            lr.useWorldSpace = true; // Essential for manual vertex positioning
            lr.startWidth = handRadius * 2f * handScale;
            lr.endWidth = handRadius * 2f * handScale; // Uniform tube
            lr.material = handMaterial;
            lr.positionCount = 5; // All chains have 5 points
            lr.numCapVertices = 8; // Very Round caps for smooth tips
            lr.numCornerVertices = 8; // Very Round corners for smooth joints
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            
            visuals.fingers.Add(lr);
        }

        return visuals;
    }

    void GetHandSubsystem()
    {
        var subsystems = new List<XRHandSubsystem>();
        SubsystemManager.GetInstances(subsystems);
        if (subsystems.Count > 0)
        {
            handSubsystem = subsystems[0];
        }
    }

    // ★ 新增判定：检查某个节点（左手或右手）是否有实体手柄正在被追踪
    bool IsControllerActive(UnityEngine.XR.XRNode node)
    {
        var devices = new List<UnityEngine.XR.InputDevice>();
        UnityEngine.XR.InputDevices.GetDevicesAtXRNode(node, devices);
        foreach (var device in devices)
        {
            // 只要设备包含控制器特征，说明是实体手柄
            if ((device.characteristics & UnityEngine.XR.InputDeviceCharacteristics.Controller) != 0)
            {
                return true;
            }
        }
        return false;
    }

    void Update()
    {
        // 核心切换逻辑：如果拿了手柄（有 Controller 特征的设备），就关掉对应侧的虚拟手
        bool leftCtrlActive = IsControllerActive(UnityEngine.XR.XRNode.LeftHand);
        bool rightCtrlActive = IsControllerActive(UnityEngine.XR.XRNode.RightHand);

        // 如果你绑定了手柄模型，顺便帮你自动显示/隐藏它们！
        if (leftControllerVisual != null) leftControllerVisual.SetActive(leftCtrlActive);
        if (rightControllerVisual != null) rightControllerVisual.SetActive(rightCtrlActive);

        if (handSubsystem == null || !handSubsystem.running)
        {
            GetHandSubsystem();
            return;
        }

        // !leftCtrlActive 表示“只要没拿手柄”，才允许显示这只追踪手
        UpdateHand(handSubsystem.leftHand, leftHandVisuals, !leftCtrlActive);
        UpdateHand(handSubsystem.rightHand, rightHandVisuals, !rightCtrlActive);
    }

    void UpdateHand(XRHand hand, HandVisuals visuals, bool allowHand)
    {
        // 如果不允许显示手（拿了手柄），或者手部没有被摄像头捕捉到追踪，隐藏它！
        if (!allowHand || !hand.isTracked)
        {
            visuals.root.SetActive(false);
            return;
        }

        // Get Wrist Pose (Anchor for scaling)
        var wristJoint = hand.GetJoint(XRHandJointID.Wrist);
        if (!wristJoint.TryGetPose(out Pose wristPose))
        {
            visuals.root.SetActive(false);
            return;
        }
        
        visuals.root.SetActive(true);

        // Update Fingers
        for (int i = 0; i < 5; i++)
        {
            LineRenderer lr = visuals.fingers[i];
            XRHandJointID[] chain = fingerChains[i];
            
            // Adjust Width dynamically (in case runtime tweak)
            // Scaling logic: Radius * 2 (Diameter) * HandScale
            float currentWidth = handRadius * 2f * handScale;
            lr.startWidth = currentWidth;
            lr.endWidth = currentWidth; 

            for (int k = 0; k < chain.Length; k++)
            {
                var joint = hand.GetJoint(chain[k]);
                if (joint.TryGetPose(out Pose pose))
                {
                    // Calculate Scaled World Position (Relative to Wrist)
                    // This expands the skeleton outward from the wrist.
                    Vector3 rawOffset = pose.position - wristPose.position;
                    Vector3 scaledPos = wristPose.position + (rawOffset * handScale);
                    
                    lr.SetPosition(k, scaledPos);
                }
            }
        }
    }
}
