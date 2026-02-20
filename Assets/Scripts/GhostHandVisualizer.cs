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

    void Update()
    {
        if (handSubsystem == null || !handSubsystem.running)
        {
            GetHandSubsystem();
            return;
        }

        UpdateHand(handSubsystem.leftHand, leftHandVisuals);
        UpdateHand(handSubsystem.rightHand, rightHandVisuals);
    }

    void UpdateHand(XRHand hand, HandVisuals visuals)
    {
        if (!hand.isTracked)
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
