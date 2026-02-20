using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using UnityEngine.XR.Hands; 
using UnityEngine.XR.Management;
using System.Collections.Generic;

public class SceneReturnManager : MonoBehaviour
{
    [Header("Settings")]
    public string mainSceneName = "SampleScene";
    public float pinchHoldDuration = 1.0f; // Seconds to hold pinch to return

    private float pinchTimer = 0f;
    private XRHandSubsystem handSubsystem;
    private bool returnTriggered = false;

    void Start()
    {
        GetHandSubsystem();
    }

    void Update()
    {
        if (returnTriggered) return;

        // 1. Check Controller Input (Primary Button: A on Right, X on Left)
        if (CheckControllerButton(XRNode.RightHand, CommonUsages.primaryButton) ||
            CheckControllerButton(XRNode.LeftHand, CommonUsages.primaryButton))
        {
            ReturnToMainInfo();
            return;
        }

        // 2. Check Hand Gesture (Left Hand Pinch Hold)
        if (handSubsystem != null && handSubsystem.running)
        {
            CheckHandGesture();
        }
        else
        {
            GetHandSubsystem();
        }
    }

    bool CheckControllerButton(XRNode node, InputFeatureUsage<bool> button)
    {
        var devices = new List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(node, devices);
        foreach (var device in devices)
        {
            if (device.isValid && device.TryGetFeatureValue(button, out bool pressed) && pressed)
            {
                return true;
            }
        }
        return false;
    }

    void CheckHandGesture()
    {
        // Check BOTH hands for pinch gesture
        bool pinching = IsPinching(handSubsystem.leftHand) || IsPinching(handSubsystem.rightHand);

        if (pinching)
        {
            pinchTimer += Time.deltaTime;
            if (pinchTimer >= pinchHoldDuration)
            {
                ReturnToMainInfo();
            }
        }
        else
        {
            pinchTimer = 0f;
        }
    }

    bool IsPinching(XRHand hand)
    {
        if (!hand.isTracked) return false;
        var indexTip = hand.GetJoint(XRHandJointID.IndexTip);
        var thumbTip = hand.GetJoint(XRHandJointID.ThumbTip);

        if (indexTip.TryGetPose(out Pose iPose) && thumbTip.TryGetPose(out Pose tPose))
        {
            // Relaxed Pinch Threshold ~3cm
            return Vector3.Distance(iPose.position, tPose.position) < 0.03f;
        }
        return false;
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

    void ReturnToMainInfo()
    {
        if (returnTriggered) return;
        returnTriggered = true;
        Debug.Log("Returning to Main Scene...");
        SceneManager.LoadScene(mainSceneName);
    }
}
