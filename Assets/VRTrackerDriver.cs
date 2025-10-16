using UnityEngine;
using Fusion;
#if USING_XR_MANAGEMENT
using Unity.XR.CoreUtils;       // for XROrigin if you use XR Interaction Toolkit
#endif

/// Drives PlayerPrefab's Head/HandL/HandR trackers from the XR rig.
/// Robust: keeps searching until anchors are found; works with OVR + XR Origin.
[DefaultExecutionOrder(1000)]   // run late so it updates after the rig
public class VRTrackerDriver : NetworkBehaviour {
  [Header("PlayerPrefab trackers (required)")]
  public Transform headTracker;     // PlayerPrefab/Head
  public Transform leftHandTracker; // PlayerPrefab/HandL
  public Transform rightHandTracker;// PlayerPrefab/HandR

  [Header("XR Anchors (auto-resolved; leave empty)")]
  public Transform centerEyeAnchor;
  public Transform leftControllerAnchor;
  public Transform rightControllerAnchor;

  [Header("Debug")]
  [Tooltip("Ignore input authority while debugging single-player.")]
  public bool forceUpdateEvenWithoutInputAuthority = false;
  bool _loggedOnce;

  public override void Spawned() {
    // Don't gate resolution to Spawned() – anchors may come later. We resolve every frame until found.
    TryResolveAnchors(); 
  }

  void LateUpdate() {
    // 1) Make sure this is the local player, unless you’re debugging
    if (!forceUpdateEvenWithoutInputAuthority && !Object.HasInputAuthority)
      return;

    // 2) Ensure we have anchors; keep trying until we do
    if (!AnchorsValid()) {
      TryResolveAnchors();
      if (!AnchorsValid()) return;
    }

    // 3) Drive the trackers
    if (headTracker)  headTracker.SetPositionAndRotation(centerEyeAnchor.position, centerEyeAnchor.rotation);
    if (leftHandTracker)  leftHandTracker.SetPositionAndRotation(leftControllerAnchor.position, leftControllerAnchor.rotation);
    if (rightHandTracker) rightHandTracker.SetPositionAndRotation(rightControllerAnchor.position, rightControllerAnchor.rotation);
  }

  bool AnchorsValid() =>
    centerEyeAnchor && leftControllerAnchor && rightControllerAnchor;

  void TryResolveAnchors() {
    // A) Try OVRCameraRig first (most reliable with Meta)
    var ovrRig = FindObjectOfType<OVRCameraRig>(true);
    if (ovrRig) {
      centerEyeAnchor      = ovrRig.centerEyeAnchor;
      leftControllerAnchor = ovrRig.leftControllerAnchor;
      rightControllerAnchor= ovrRig.rightControllerAnchor;
      LogFound("OVRCameraRig");
      return;
    }

    // B) Try common scene paths from Meta XR BuildingBlocks
    centerEyeAnchor      = centerEyeAnchor      ? centerEyeAnchor      : FindByPath("[BuildingBlock] Camera Rig/TrackingSpace/CenterEyeAnchor");
    leftControllerAnchor = leftControllerAnchor ? leftControllerAnchor : FindByPath("[BuildingBlock] Camera Rig/TrackingSpace/LeftHandAnchor/LeftControllerAnchor");
    rightControllerAnchor= rightControllerAnchor? rightControllerAnchor: FindByPath("[BuildingBlock] Camera Rig/TrackingSpace/RightHandAnchor/RightControllerAnchor");
    if (AnchorsValid()) { LogFound("BuildingBlock paths"); return; }

    // C) Try XR Interaction Toolkit (XR Origin)
#if USING_XR_MANAGEMENT
    var origin = FindObjectOfType<XROrigin>(true);
    if (origin) {
      centerEyeAnchor = origin.Camera ? origin.Camera.transform : centerEyeAnchor;
      // Note: you'd still need to assign controller anchors from your interactor objects if using XRI.
      // Do nothing here unless you know their transforms.
    }
#endif

    // D) Try best-effort name search
    if (!centerEyeAnchor)      centerEyeAnchor      = FindByName("CenterEyeAnchor");
    if (!leftControllerAnchor) leftControllerAnchor = FindByName("LeftControllerAnchor");
    if (!rightControllerAnchor)rightControllerAnchor= FindByName("RightControllerAnchor");
    if (AnchorsValid()) { LogFound("name search"); return; }

    if (!_loggedOnce) {
      Debug.LogWarning("[VRTrackerDriver] XR anchors NOT found yet; will keep searching...");
      _loggedOnce = true;
    }
  }

  Transform FindByPath(string path) {
    var go = GameObject.Find(path);
    return go ? go.transform : null;
  }
  Transform FindByName(string name) {
    foreach (var t in Resources.FindObjectsOfTypeAll<Transform>()) {
      if (t.name == name && t.gameObject.scene.IsValid()) return t;
    }
    return null;
  }
  void LogFound(string how) {
    if (_loggedOnce) return;
    _loggedOnce = true;
    Debug.Log($"[VRTrackerDriver] Anchors resolved via {how}: " +
              $"{(centerEyeAnchor?centerEyeAnchor.name:"null")}, " +
              $"{(leftControllerAnchor?leftControllerAnchor.name:"null")}, " +
              $"{(rightControllerAnchor?rightControllerAnchor.name:"null")}");
  }
}
