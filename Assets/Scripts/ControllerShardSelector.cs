using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;
using Unity.XR.CoreUtils;

/// <summary>
/// Standalone controller trigger selector — completely independent of HandRayPointer.
///
/// SETUP in Unity:
///   1. Add this script to any persistent GameObject (e.g., XR Origin or a Manager object).
///   2. Assign "Shard Layer" in the Inspector to the layer your shards are on,
///      OR leave as "Everything" and it will hit any PhysicsCollider.
///   3. The red laser line shows you where the controller is pointing.
///
/// HOW IT WORKS:
///   - Every frame, reads the right (and optionally left) controller's world position + rotation.
///   - SphereCasts a ray to find ShardInteraction objects.
///   - On trigger press (rising edge), calls PublicSelect() on the hovered shard.
///   - Completely bypasses hand tracking — works whenever a Touch controller is active.
/// </summary>
public class ControllerShardSelector : MonoBehaviour
{
    [Header("Ray Settings")]
    public XRNode controllerNode = XRNode.RightHand; // Which controller to use
    public float rayDistance = 6f;
    public float sphereRadius = 0.06f;
    public LayerMask shardLayer = ~0; // Default: hit everything; narrow to shard layer if needed

    [Header("Visual")]
    public bool showRay = true;
    public Color rayColor = new Color(1f, 0.8f, 0f, 0.6f); // Gold tint
    public float lineWidth = 0.004f;

    private LineRenderer line;
    private ShardInteraction hoveredShard;
    private bool triggerWasDown;
    private XROrigin xrOrigin;

    void Start()
    {
        xrOrigin = FindObjectOfType<XROrigin>();

        // Create a simple laser line renderer
        var go = new GameObject("ControllerLaser");
        go.transform.SetParent(transform);
        line = go.AddComponent<LineRenderer>();
        line.startWidth = lineWidth;
        line.endWidth   = 0f;
        line.positionCount = 2;
        var mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = rayColor;
        line.material = mat;
        line.startColor = rayColor;
        line.endColor   = new Color(rayColor.r, rayColor.g, rayColor.b, 0f);
        line.enabled = false;
    }

    void Update()
    {
        // ── 1. Get controller pose ──────────────────────────────────────────
        if (!TryGetControllerPose(controllerNode, out Vector3 pos, out Quaternion rot))
        {
            line.enabled = false;
            ClearHover();
            return;
        }

        // ── 2. Build world-space ray ───────────────────────────────────────
        // devicePosition is in XR tracking space; convert to world via XR Origin
        Vector3 worldPos;
        Vector3 worldFwd;
        if (xrOrigin != null)
        {
            worldPos = xrOrigin.transform.TransformPoint(pos);
            worldFwd = xrOrigin.transform.TransformDirection(rot * Vector3.forward);
        }
        else
        {
            worldPos = pos;
            worldFwd = rot * Vector3.forward;
        }

        // ── 3. SphereCast for shards ───────────────────────────────────────
        ShardInteraction hitShard = null;
        Vector3 endPoint = worldPos + worldFwd * rayDistance;

        if (Physics.SphereCast(worldPos, sphereRadius, worldFwd, out RaycastHit hit, rayDistance, shardLayer))
        {
            endPoint = hit.point;
            hitShard = hit.collider.GetComponent<ShardInteraction>()
                    ?? hit.collider.GetComponentInParent<ShardInteraction>();
        }

        // ── 4. Update hover state ──────────────────────────────────────────
        if (hitShard != hoveredShard)
        {
            if (hoveredShard != null) hoveredShard.PublicHoverExit();
            hoveredShard = hitShard;
            if (hoveredShard != null) hoveredShard.PublicHoverEnter();
        }

        // ── 5. Draw laser line ──────────────────────────────────────────────
        if (showRay)
        {
            line.enabled = true;
            line.SetPosition(0, worldPos);
            line.SetPosition(1, endPoint);
        }

        // ── 6. Trigger press (rising edge) ─────────────────────────────────
        bool triggerDown = IsTriggerDown(controllerNode);
        if (triggerDown && !triggerWasDown)
        {
            Debug.Log($"[ControllerShardSelector] Trigger pressed. Hovered: {(hoveredShard != null ? hoveredShard.name : "none")}");
            if (hoveredShard != null)
                hoveredShard.PublicSelect();
        }
        triggerWasDown = triggerDown;
    }

    void ClearHover()
    {
        if (hoveredShard != null) hoveredShard.PublicHoverExit();
        hoveredShard = null;
    }

    // Returns true while the trigger is significantly pressed on the given node
    bool IsTriggerDown(XRNode node)
    {
        var devices = new List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(node, devices);
        foreach (var d in devices)
        {
            if (!d.isValid) continue;
            if (d.TryGetFeatureValue(CommonUsages.triggerButton, out bool btn) && btn) return true;
            if (d.TryGetFeatureValue(CommonUsages.trigger,       out float ax) && ax > 0.75f) return true;
        }
        return false;
    }

    bool TryGetControllerPose(XRNode node, out Vector3 pos, out Quaternion rot)
    {
        pos = Vector3.zero;
        rot = Quaternion.identity;
        var devices = new List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(node, devices);
        foreach (var d in devices)
        {
            if (d.isValid &&
                d.TryGetFeatureValue(CommonUsages.devicePosition, out pos) &&
                d.TryGetFeatureValue(CommonUsages.deviceRotation, out rot))
                return true;
        }
        return false;
    }
}
