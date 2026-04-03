using UnityEngine;
using UnityEngine.XR.Hands; 
using UnityEngine.XR.Management;
using System.Collections.Generic;

/// <summary>
/// Ghost hand visualizer with PlayerHand tagged fingertip colliders.
/// When controllers are put down, ghost hands appear and their fingertips
/// have trigger colliders for scene interaction (trees, cats, fish etc.)
/// </summary>

public class GhostHandVisualizer : MonoBehaviour
{
    public XRHandSubsystem handSubsystem;
    
    [Header("Visual Settings")]
    public Material handMaterial; 
    public float handRadius = 0.012f; // 从 3.5cm 降为 1.2cm (苗条感)
    public float handScale = 1.0f;    // 还原真人比例 (不再臃肿)

    [Header("Icons (UI 反馈)")]
    public GameObject leftHandIcon;
    public GameObject rightHandIcon;
    public GameObject leftControllerIcon;
    public GameObject rightControllerIcon;

    [Header("Tracking")]
    [Tooltip("如果为空，自动寻找 XR Origin")]
    public Transform xrOrigin;

    [Header("Controller Visuals")]
    public GameObject leftControllerVisual;
    public GameObject rightControllerVisual;

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
                // 极致透明感：0.15 Alpha
                Color ghostColor = new Color(1f, 1f, 1f, 0.15f);
                handMaterial.SetColor("_BaseColor", ghostColor);
                handMaterial.color = ghostColor;
                
                // 开启透明混合
                handMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                handMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                handMaterial.SetInt("_ZWrite", 0);
                handMaterial.renderQueue = 3000;
                handMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
        }
    }

    HandVisuals CreateHandVisuals(string name)
    {
        HandVisuals visuals = new HandVisuals();
        visuals.root = new GameObject(name);
        visuals.root.transform.parent = transform;
        visuals.root.transform.localPosition = Vector3.zero;
        visuals.root.transform.localScale = Vector3.one;

        for (int i = 0; i < 5; i++)
        {
            GameObject fingerObj = new GameObject($"Finger_{i}");
            fingerObj.transform.parent = visuals.root.transform;
            
            LineRenderer lr = fingerObj.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.startWidth = handRadius * 2f; 
            lr.endWidth = handRadius * 1.5f; // 指尖稍微更尖一点
            lr.material = handMaterial;
            lr.positionCount = 5;
            lr.numCapVertices = 8;
            lr.numCornerVertices = 8;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            
            visuals.fingers.Add(lr);

            GameObject tipObj = new GameObject($"Fingertip_{i}");
            tipObj.transform.parent = fingerObj.transform;
            tipObj.tag = "PlayerHand"; // 必须有此 Tag
            
            SphereCollider tipCol = tipObj.AddComponent<SphereCollider>();
            tipCol.isTrigger = true;
            tipCol.radius = handRadius * 1.2f; 
            
            Rigidbody tipRb = tipObj.AddComponent<Rigidbody>();
            tipRb.isKinematic = true;
            tipRb.useGravity = false;
            
            visuals.fingertipColliders.Add(tipObj);
        }

        return visuals;
    }

    void GetHandSubsystem()
    {
        var subsystems = new List<XRHandSubsystem>();
        SubsystemManager.GetInstances(subsystems);
        if (subsystems.Count > 0) handSubsystem = subsystems[0];
    }

    bool IsControllerTracked(UnityEngine.XR.XRNode node)
    {
        var devices = new List<UnityEngine.XR.InputDevice>();
        UnityEngine.XR.InputDevices.GetDevicesAtXRNode(node, devices);
        foreach (var device in devices)
        {
            if (device.isValid && (device.characteristics & UnityEngine.XR.InputDeviceCharacteristics.Controller) != 0)
                return true;
        }
        return false;
    }

    void Update()
    {
        bool leftCtrlActive = IsControllerTracked(UnityEngine.XR.XRNode.LeftHand);
        bool rightCtrlActive = IsControllerTracked(UnityEngine.XR.XRNode.RightHand);

        if (leftControllerVisual != null) leftControllerVisual.SetActive(leftCtrlActive);
        if (rightControllerVisual != null) rightControllerVisual.SetActive(rightCtrlActive);

        // UI 图标逻辑
        if (leftHandIcon != null) leftHandIcon.SetActive(!leftCtrlActive);
        if (rightHandIcon != null) rightHandIcon.SetActive(!rightCtrlActive);
        if (leftControllerIcon != null) leftControllerIcon.SetActive(leftCtrlActive);
        if (rightControllerIcon != null) rightControllerIcon.SetActive(rightCtrlActive);

        if (xrOrigin == null)
        {
            GameObject originObj = GameObject.Find("XR Origin");
            if (originObj == null) originObj = GameObject.Find("XR Rig");
            if (originObj != null) xrOrigin = originObj.transform;
        }

        if (handSubsystem != null)
        {
            UpdateHand(handSubsystem.leftHand, leftHandVisuals, !leftCtrlActive);
            UpdateHand(handSubsystem.rightHand, rightHandVisuals, !rightCtrlActive);
        }
    }

    void UpdateHand(XRHand hand, HandVisuals visuals, bool allowHand)
    {
        if (!allowHand || !hand.isTracked)
        {
            visuals.root.SetActive(false);
            return;
        }

        visuals.root.SetActive(true);

        for (int i = 0; i < 5; i++)
        {
            LineRenderer lr = visuals.fingers[i];
            XRHandJointID[] chain = fingerChains[i];
            
            lr.startWidth = handRadius * 2f;
            lr.endWidth = handRadius * 1.5f;

            for (int k = 0; k < chain.Length; k++)
            {
                var joint = hand.GetJoint(chain[k]);
                if (joint.TryGetPose(out Pose pose))
                {
                    // 坐标转换到 XR Origin 空间保持跟随
                    Vector3 worldPos = xrOrigin != null ? xrOrigin.TransformPoint(pose.position) : pose.position;
                    lr.SetPosition(k, worldPos);

                    if (k == chain.Length - 1 && i < visuals.fingertipColliders.Count)
                    {
                        visuals.fingertipColliders[i].transform.position = worldPos;
                    }
                }
            }
        }
    }
}
