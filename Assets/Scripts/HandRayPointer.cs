using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;
using System.Collections.Generic;

public class HandRayPointer : MonoBehaviour
{
    public XRHandSubsystem handSubsystem;

    [Header("Configuration")]
    public float pinchThreshold = 0.02f;
    public float releaseThreshold = 0.03f;
    public LayerMask interactableLayer = -1;
    public float rayDistance = 5.0f;
    public float visualHandScale = 1.8f;

    [Header("Visuals")]
    public bool showDebugRay = true;
    public Material laserMaterial;
    public float laserWidth = 0.005f;
    public Color laserColor = new Color(1f, 1f, 1f, 0.2f);

    private LineRenderer laserLine;
    private Transform debugSphere;
    private ShardInteraction currentHoveredShard;
    private bool isPinching = false;

    void Start()
    {
        GetHandSubsystem();
        SetupVisuals();
    }

    void SetupVisuals()
    {
        GameObject laserObj = new GameObject("HandLaser");
        laserObj.transform.parent = transform;
        laserLine = laserObj.AddComponent<LineRenderer>();
        laserLine.startWidth = laserWidth;
        laserLine.endWidth = 0f;
        laserLine.material = laserMaterial != null ? laserMaterial : new Material(Shader.Find("Sprites/Default"));
        laserLine.startColor = laserColor;
        laserLine.endColor = new Color(laserColor.r, laserColor.g, laserColor.b, 0f);
        laserLine.positionCount = 2;
        laserLine.enabled = false;

        debugSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
        debugSphere.parent = transform;
        debugSphere.localScale = Vector3.one * 0.05f;
        Destroy(debugSphere.GetComponent<Collider>());
        debugSphere.gameObject.SetActive(false);
    }

    void Update()
    {
        // If hand subsystem is absent or not running, hide ray and do nothing.
        // This prevents a second laser line appearing when the user holds controllers.
        if (handSubsystem == null || !handSubsystem.running)
        {
            laserLine.enabled = false;
            debugSphere.gameObject.SetActive(false);
            GetHandSubsystem();
            return;
        }

        ProcessHand(handSubsystem.rightHand);
    }

    void GetHandSubsystem()
    {
        var subsystems = new List<XRHandSubsystem>();
        SubsystemManager.GetInstances(subsystems);
        if (subsystems.Count > 0) handSubsystem = subsystems[0];
    }

    void ProcessHand(XRHand hand)
    {
        // Hide everything if hand is not tracked (e.g. holding controllers)
        if (!hand.isTracked)
        {
            laserLine.enabled = false;
            debugSphere.gameObject.SetActive(false);
            if (currentHoveredShard != null)
            {
                currentHoveredShard.PublicHoverExit();
                currentHoveredShard = null;
            }
            return;
        }

        var wrist         = hand.GetJoint(XRHandJointID.Wrist);
        var indexProximal = hand.GetJoint(XRHandJointID.IndexProximal);
        var thumbTip      = hand.GetJoint(XRHandJointID.ThumbTip);
        var indexTip      = hand.GetJoint(XRHandJointID.IndexTip);

        if (!wrist.TryGetPose(out Pose wristPose)          ||
            !indexProximal.TryGetPose(out Pose idxProxPose) ||
            !thumbTip.TryGetPose(out Pose thumbTipPose)     ||
            !indexTip.TryGetPose(out Pose indexTipPose))
            return;

        // Scale joint positions to match the visual hand scale
        Vector3 scaledTip  = wristPose.position + (indexTipPose.position  - wristPose.position) * visualHandScale;
        Vector3 scaledProx = wristPose.position + (idxProxPose.position   - wristPose.position) * visualHandScale;

        Vector3 rayOrigin    = scaledTip;
        Vector3 rayDirection = (scaledTip - scaledProx).normalized;
        Vector3 endPoint     = rayOrigin + rayDirection * rayDistance;

        // Raycast
        ShardInteraction hitShard = null;
        if (Physics.SphereCast(rayOrigin, 0.05f, rayDirection, out RaycastHit hit, rayDistance, interactableLayer))
        {
            endPoint  = hit.point;
            hitShard  = hit.collider.GetComponent<ShardInteraction>()
                     ?? hit.collider.GetComponentInParent<ShardInteraction>();
            debugSphere.gameObject.SetActive(true);
            debugSphere.position = hit.point;
        }
        else
        {
            debugSphere.gameObject.SetActive(false);
        }

        // Laser visual
        if (showDebugRay)
        {
            laserLine.enabled = true;
            laserLine.SetPosition(0, rayOrigin);
            laserLine.SetPosition(1, endPoint);
        }

        // Hover state
        if (hitShard != currentHoveredShard)
        {
            if (currentHoveredShard != null) currentHoveredShard.PublicHoverExit();
            currentHoveredShard = hitShard;
            if (currentHoveredShard != null) currentHoveredShard.PublicHoverEnter();
        }

        // Pinch detection
        float dist = Vector3.Distance(indexTipPose.position, thumbTipPose.position);
        if (!isPinching && dist < pinchThreshold)
        {
            isPinching = true;
            debugSphere.GetComponent<Renderer>().material.color = Color.green;
            if (currentHoveredShard != null) currentHoveredShard.PublicSelect();
        }
        else if (isPinching && dist > releaseThreshold)
        {
            isPinching = false;
            debugSphere.GetComponent<Renderer>().material.color = Color.red;
        }
    }
}
