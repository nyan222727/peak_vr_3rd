using UnityEngine;
using RootMotion.FinalIK;
using Fusion;
public class AvatarAutoAlign : NetworkBehaviour {
  [Header("Inputs")]
  public Transform headTracker;    // PlayerPrefab/Head tracker
  public VRIK vrik;                // VRIK on this avatar

  [Header("Follow tuning")]
  public float yawFollowSpeed  = 16f;
  public float moveFollowSpeed = 18f;

  [Header("Floor clamp (optional)")]
  public bool  clampToFloorY = true;
  public float floorY        = 0.01f;

  // cached bones/offsets
  Transform _headBone;
  Vector3   _headLocalOffset;   // avatar-root → head-bone (in root local space)
  bool      _ready;

public float firstPersonForwardOffset = 15f; // meters
public float firstPersonDownOffset    = 0.0f; // meters
public bool  applyLocalFirstPersonOffset = true;
public NetworkObject ownerNo; // assign the player’s NetworkObject once

bool IsLocal => ownerNo && ownerNo.HasInputAuthority;

    void Reset() { vrik = GetComponent<VRIK>(); }

  void Start() {
    if (!vrik) vrik = GetComponent<VRIK>();

    // Resolve tracker if empty
    if (!headTracker && transform.root) {
      var p = transform.root;
      headTracker = p.Find("Head");
    }

    if (vrik != null && vrik.references != null && vrik.references.head != null) {
      _headBone = vrik.references.head;
      // where is the head bone relative to the avatar root?
      _headLocalOffset = transform.InverseTransformPoint(_headBone.position);
      _ready = true;
    } else {
      Debug.LogWarning("[AvatarAutoAlign] Missing VRIK head reference.");
    }
  }

  void LateUpdate() {
    if (!_ready || !headTracker) return;

    // Yaw from head forward (ignore pitch/roll)
    Vector3 fwd = Vector3.ProjectOnPlane(headTracker.forward, Vector3.up);
    if (fwd.sqrMagnitude < 1e-4f) fwd = transform.forward;
    Quaternion targetYaw = Quaternion.LookRotation(fwd, Vector3.up);

    // Place the avatar root so that: headBone_world == headTracker_world
    Vector3 desiredRootPos = headTracker.position - (targetYaw * _headLocalOffset);
    if (applyLocalFirstPersonOffset && IsLocal) {
        desiredRootPos += targetYaw * Vector3.forward * firstPersonForwardOffset;
        desiredRootPos += Vector3.down * firstPersonDownOffset;
    }

    // Optional floor clamp (keeps avatar from sinking below Y floor)
    if (clampToFloorY)
      desiredRootPos.y = Mathf.Max(desiredRootPos.y, floorY);

    // Smooth move/rotate
    transform.position = Vector3.Lerp(transform.position, desiredRootPos, Time.deltaTime * moveFollowSpeed);
    transform.rotation = Quaternion.Slerp(transform.rotation, targetYaw, Time.deltaTime * yawFollowSpeed);
  }
}
