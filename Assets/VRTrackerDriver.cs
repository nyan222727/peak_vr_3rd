using UnityEngine;

#if USING_XR_MANAGEMENT
using Unity.XR.CoreUtils;       // for XROrigin if you use XR Interaction Toolkit
#endif

/// Local (non-Fusion) version: drives PlayerPrefab's head/hand trackers from the XR rig.
/// Robust: keeps searching until anchors are found; works with OVRCameraRig + Meta BuildingBlocks paths + (optionally) XROrigin.
[DefaultExecutionOrder(1000)]   // run late so it updates after the rig
public class VRTrackerDriver : MonoBehaviour
{
    [Header("Hand Override (optional)")]
    public bool overrideLeftHand;
    public bool overrideRightHand;
    public Transform leftHandOverrideTarget;
    public Transform rightHandOverrideTarget;

    [Header("PlayerPrefab trackers (required)")]
    public Transform headTracker;      // PlayerPrefab/Head
    public Transform leftHandTracker;  // PlayerPrefab/HandL
    public Transform rightHandTracker; // PlayerPrefab/HandR

    [Header("XR Anchors (auto-resolved; leave empty)")]
    public Transform centerEyeAnchor;
    public Transform leftControllerAnchor;
    public Transform rightControllerAnchor;

    [Header("Debug")]
    public bool logResolvedOnce = true;

    private bool _loggedOnce;

    private void Start()
    {
        // Anchors may spawn late, so we don't rely only on Start; we still retry in LateUpdate.
        TryResolveAnchors();
    }

    private void LateUpdate()
    {
        // Keep trying until anchors exist
        if (!AnchorsValid())
        {
            TryResolveAnchors();
            if (!AnchorsValid()) return;
        }

        // Drive the trackers
        if (headTracker && centerEyeAnchor)
            headTracker.SetPositionAndRotation(centerEyeAnchor.position, centerEyeAnchor.rotation);

        if (leftHandTracker && leftControllerAnchor)
        {
            if (overrideLeftHand && leftHandOverrideTarget)
                leftHandTracker.SetPositionAndRotation(leftHandOverrideTarget.position, leftHandOverrideTarget.rotation);
            else
                leftHandTracker.SetPositionAndRotation(leftControllerAnchor.position, leftControllerAnchor.rotation);
        }

        if (rightHandTracker && rightControllerAnchor)
        {
            if (overrideRightHand && rightHandOverrideTarget)
                rightHandTracker.SetPositionAndRotation(rightHandOverrideTarget.position, rightHandOverrideTarget.rotation);
            else
                rightHandTracker.SetPositionAndRotation(rightControllerAnchor.position, rightControllerAnchor.rotation);
        }
    }

    private bool AnchorsValid() =>
        centerEyeAnchor && leftControllerAnchor && rightControllerAnchor;

    private void TryResolveAnchors()
    {
        // A) Try OVRCameraRig first (most reliable with Meta)
        var ovrRig = FindObjectOfType<OVRCameraRig>(true);
        if (ovrRig)
        {
            centerEyeAnchor       = ovrRig.centerEyeAnchor;
            leftControllerAnchor  = ovrRig.leftControllerAnchor;
            rightControllerAnchor = ovrRig.rightControllerAnchor;
            LogFound("OVRCameraRig");
            return;
        }

        // B) Try common scene paths from Meta XR BuildingBlocks
        if (!centerEyeAnchor)
            centerEyeAnchor = FindByPath("[BuildingBlock] Camera Rig/TrackingSpace/CenterEyeAnchor");

        if (!leftControllerAnchor)
            leftControllerAnchor = FindByPath("[BuildingBlock] Camera Rig/TrackingSpace/LeftHandAnchor/LeftControllerAnchor");

        if (!rightControllerAnchor)
            rightControllerAnchor = FindByPath("[BuildingBlock] Camera Rig/TrackingSpace/RightHandAnchor/RightControllerAnchor");

        if (AnchorsValid())
        {
            LogFound("BuildingBlock paths");
            return;
        }

        // C) Try XR Interaction Toolkit (XR Origin) - only resolves center eye by default
#if USING_XR_MANAGEMENT
        var origin = FindObjectOfType<XROrigin>(true);
        if (origin)
        {
            if (!centerEyeAnchor && origin.Camera)
                centerEyeAnchor = origin.Camera.transform;

            // NOTE: controller anchors vary per XRI setup; keep your own assignments if needed.
        }
#endif

        // D) Best-effort name search
        if (!centerEyeAnchor)       centerEyeAnchor       = FindByName("CenterEyeAnchor");
        if (!leftControllerAnchor)  leftControllerAnchor  = FindByName("LeftControllerAnchor");
        if (!rightControllerAnchor) rightControllerAnchor = FindByName("RightControllerAnchor");

        if (AnchorsValid())
        {
            LogFound("name search");
            return;
        }

        if (logResolvedOnce && !_loggedOnce)
        {
            Debug.LogWarning("[VRTrackerDriverLocal] XR anchors NOT found yet; will keep searching...");
            _loggedOnce = true;
        }
    }

    private Transform FindByPath(string path)
    {
        var go = GameObject.Find(path);
        return go ? go.transform : null;
    }

    private Transform FindByName(string name)
    {
        foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (t.name == name && t.gameObject.scene.IsValid())
                return t;
        }
        return null;
    }

    private void LogFound(string how)
    {
        if (!logResolvedOnce) return;
        if (_loggedOnce) return;

        _loggedOnce = true;
        Debug.Log($"[VRTrackerDriverLocal] Anchors resolved via {how}: " +
                  $"{(centerEyeAnchor ? centerEyeAnchor.name : "null")}, " +
                  $"{(leftControllerAnchor ? leftControllerAnchor.name : "null")}, " +
                  $"{(rightControllerAnchor ? rightControllerAnchor.name : "null")}");
    }
}
