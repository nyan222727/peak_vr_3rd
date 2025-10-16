using UnityEngine;
using Fusion;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(Rigidbody))]
public class NetworkPlate : NetworkBehaviour
{
  [Header("Select (tractor-beam) settings")]
  [SerializeField] private float minHoldDistance    = 0.25f;  // min ray distance
  [SerializeField] private float maxHoldDistance    = 1.00f;  // max ray distance
  [SerializeField] private float maxStepPerSecond   = 0.50f;  // m/s cap while dragging
  [SerializeField] private float maxRadiusFromStart = 1.00f;  // max roam from select start
  [SerializeField] private LayerMask dragMask = ~0;     // what the ray can target
  [SerializeField] private float raycastMaxDistance = 3f;
  [SerializeField] private bool useRaycastHit = true;  // steer by hit point

  [SerializeField] private Transform defaultAimSource;   // ▶ assign your controller ray here
  [SerializeField] private string rightRayPath =
  "[BuildingBlock] Camera Rig/[BuildingBlock] OVRInteractionComprehensive/RightInteractions/Interactors/Controller/ControllerRayInteractor/Visuals/ControllerRay";

  [SerializeField] private bool useNegativeForward = false; // ▶ if your ray points -Z
  [SerializeField] private bool  lockVertical = true;   // keep Y fixed while selected
  [SerializeField] private float planeYOffset = 0.0001f;  // small lift above desk
  private float _lockedY;                                // Y we’ll stick to during drag



  [Networked] private NetworkBool IsHeld     { get; set; }
  [Networked] private NetworkBool IsSelected { get; set; }

  private Rigidbody _rb;
  private Transform _selector;       // ray interactor transform
  private Vector3   _selectStartPos; // where the plate was when select began
  private float     _holdDistance;   // distance from ray origin while dragging

  void Awake() {
    _rb = GetComponent<Rigidbody>();
  }

  public override void Spawned()
  {
    // ensure we can aim even if the event passes no transform
    ResolveAimSource();
  }

  // ---- Simulation ----
  public override void FixedUpdateNetwork()
  {
    // Only the StateAuthority drives motion; others receive it via NetworkRigidbody3D.
    if (!Object.HasStateAuthority || !IsSelected || _selector == null)
      return;

    // target point along the ray
    var origin  = _selector.position;
    var forward = useNegativeForward ? -_selector.forward : _selector.forward;

    Vector3 wanted;
    if (useRaycastHit && Physics.Raycast(origin, forward, out var hit, raycastMaxDistance, dragMask, QueryTriggerInteraction.Ignore))
    {
      wanted = hit.point;
      _holdDistance = Mathf.Clamp(hit.distance, minHoldDistance, maxHoldDistance);
    }
    else
    {
      wanted = origin + forward * _holdDistance;
    }
    //var wanted  = origin + forward * _holdDistance;
    if (lockVertical) wanted.y = _lockedY;

    // clamp overall roam radius from the point where selection started
    var fromStart = wanted - _selectStartPos;
    if (fromStart.magnitude > maxRadiusFromStart)
      wanted = _selectStartPos + fromStart.normalized * maxRadiusFromStart;

    // limit how far we can move this tick (prevents big teleports)
    float maxStep = maxStepPerSecond * Runner.DeltaTime;
    Vector3 delta = wanted - _rb.position;
    if (delta.magnitude > maxStep)
      wanted = _rb.position + delta.normalized * maxStep;

    // drive the body
    if (_rb.isKinematic) {
      _rb.MovePosition(wanted);
    } else {
      var v = (wanted - _rb.position) / Runner.DeltaTime;
      if (lockVertical) v.y = 0f;          // ensure no vertical velocity when dynamic
          _rb.velocity = v;
    }   
  }

private Transform ResolveAimSource() {
  if (defaultAimSource) return defaultAimSource;

  // 1) Try a tagged object (recommended: tag your ControllerRay as "RightRay")
  var tagged = GameObject.FindWithTag("RightRay");
  if (tagged) return defaultAimSource = tagged.transform;

  // 2) Try by full path (works with your rig structure)
  var byPath = GameObject.Find(rightRayPath);
  if (byPath) return defaultAimSource = byPath.transform;

  // 3) Try by name (last resort)
  foreach (var t in Resources.FindObjectsOfTypeAll<Transform>()) {
    if (t.name == "ControllerRay") return defaultAimSource = t;
  }
  // 4) Fallback to controller anchor (always exists)
  var anchor = GameObject.Find("[BuildingBlock] Camera Rig/TrackingSpace/RightHandAnchor/RightControllerAnchor");
  if (anchor) return defaultAimSource = anchor.transform;

  Debug.LogWarning("[Plate] Could not resolve aim source.");
  return null;
}
  // ---- Grab (same pattern you used on candle) ----
  public void Grab()
  {
    if (!Object.HasStateAuthority) Object.RequestStateAuthority();
    IsHeld = true;
  }

  public void UnGrab()
  {
    if (!Object.HasStateAuthority) Object.RequestStateAuthority();
    IsHeld = false;
    RPC_UnGrab();
  }

  // ---- Select (press-and-hold ray) ----
  
  public void BeginSelect() {
  var t = ResolveAimSource();
  if (!t) return;
  BeginSelect(t);
}

public void BeginSelect(Transform interactor) {
  _selector = interactor;
  if (!Object.HasStateAuthority) Object.RequestStateAuthority();

  IsSelected      = true;
  _selectStartPos = transform.position;

  // lock Y to current plate height + tiny offset
  _lockedY = transform.position.y + planeYOffset;

  float dist = Vector3.Distance(_selector.position, transform.position);
  _holdDistance = Mathf.Clamp(dist, minHoldDistance, maxHoldDistance);

  if (Object.HasStateAuthority) {
    _rb.useGravity = false;
    if (!_rb.isKinematic) { _rb.velocity = Vector3.zero; _rb.angularVelocity = Vector3.zero; }
    _rb.isKinematic = true;
  }
}


  /*public void BeginSelect(Transform interactor) {
    _selector = interactor;
    Debug.Log($"[Plate] BeginSelect from {_selector.name} fwd={_selector.forward}");
    if (!Object.HasStateAuthority) Object.RequestStateAuthority();

    IsSelected     = true;
    _selectStartPos = transform.position;

    float dist = Vector3.Distance(_selector.position, transform.position);
    _holdDistance = Mathf.Clamp(dist, minHoldDistance, maxHoldDistance);

    if (Object.HasStateAuthority) {
      _rb.useGravity = false;
      _rb.isKinematic = true;   // we’ll MovePosition while selected
      _rb.velocity = Vector3.zero;
      _rb.angularVelocity = Vector3.zero;
    }
  }*/

  // Wire this to your ray "Select End" (button released)
  public void EndSelect() {
    IsSelected = false;
    _selector  = null;

    if (Object.HasStateAuthority) {
      _rb.isKinematic = false;  // resume physics
      _rb.useGravity  = true;
      _rb.velocity = Vector3.zero;
      _rb.angularVelocity = Vector3.zero;
    }
  }

  // Keep parity with your candle ungrab behavior
  [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
  private void RPC_UnGrab() {
    if (_rb) {
      _rb.isKinematic = false;
      _rb.useGravity  = true;
    }
  }
}
