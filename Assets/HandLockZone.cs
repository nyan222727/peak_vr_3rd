using UnityEngine;

public class HandLockZone : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    [Header("Hover Glow")]
    [SerializeField] private MeshRenderer baseRenderer;   // parent Plate_Net renderer (normal)
    [SerializeField] private MeshRenderer glowRenderer;   // child Plate_Net renderer (glow shader)
    [SerializeField] private Transform hoverCenter;       // optional (defaults to transform)
    [SerializeField] private float hoverRadius = 0.25f;
    [SerializeField] private LayerMask hoverHandMask;     // set to Hand layer in inspector

    [Header("Start Game Gate")]
    [SerializeField] private bool startGameActive = false;

    [Header("Lock Points (pose + position). Use at least DefaultLockPoint.")]
    [SerializeField] private Transform defaultLockPoint;
    [SerializeField] private Transform leftLockPoint;
    [SerializeField] private Transform rightLockPoint;

    [Header("Rules")]
    [SerializeField] private bool onlyOneHandTotal = true;

    [Header("Haptics")]
    [SerializeField] private bool enableHaptics = true;

    // pulse on lock
    [SerializeField] private float lockPulseAmplitude = 0.6f;
    [SerializeField] private float lockPulseDuration = 0.08f;

    // continuous while moving
    [SerializeField] private float moveSpeedThreshold = 0.01f;   // meters/sec
    [SerializeField] private float moveToAmplitude = 0.6f;       // amplitude per (m/s)
    [SerializeField] private float maxMoveAmplitude = 1.0f;
    [SerializeField] private float movePulseInterval = 0.06f;
    [SerializeField] private float movePulseDuration = 0.03f;

    private Vector3 _lastPos;
    private Quaternion _lastRot;
    private bool _initializedMotion;
    private bool _isLocked;
    private HandColliderId _lockedHand;
    private HandLock.HandSide _lockedSide;
    private int _touchingHandCount = 0;
    private static readonly Collider[] _hoverHits = new Collider[16];

private void Awake()
{
    _lastPos = transform.position;
    _lastRot = transform.rotation;
    _initializedMotion = true;
}
private void Update()
{
    UpdateVisuals(force: false);
    UpdateLockedHaptics();
}

private void UpdateVisuals(bool force)
{
    if (baseRenderer == null) baseRenderer = GetComponent<MeshRenderer>();
    if (hoverCenter == null) hoverCenter = transform;

    // StartGame false: both OFF
    if (!startGameActive)
    {
        if (baseRenderer && baseRenderer.enabled) baseRenderer.enabled = false;
        if (glowRenderer && glowRenderer.enabled) glowRenderer.enabled = false;
        return;
    }

    // Locked or touching: glow OFF, base ON
    if (_isLocked || _touchingHandCount > 0)
    {
        if (glowRenderer && glowRenderer.enabled) glowRenderer.enabled = false;
        if (baseRenderer && !baseRenderer.enabled) baseRenderer.enabled = true;
        return;
    }

    // Hover (near but not touching): glow ON, base OFF
    bool hovering = IsAnyHandHovering();
    if (hovering)
    {
        if (baseRenderer && baseRenderer.enabled) baseRenderer.enabled = false;
        if (glowRenderer && !glowRenderer.enabled) glowRenderer.enabled = true;
    }
    else
    {
        // Not hovering: glow OFF, base ON
        if (glowRenderer && glowRenderer.enabled) glowRenderer.enabled = false;
        if (baseRenderer && !baseRenderer.enabled) baseRenderer.enabled = true;
    }
}

private bool IsAnyHandHovering()
{
    // Only check if we have a mask set
    if (hoverHandMask.value == 0) return false;

    int n = Physics.OverlapSphereNonAlloc(
        hoverCenter.position,
        hoverRadius,
        _hoverHits,
        hoverHandMask,
        QueryTriggerInteraction.Collide
    );

    for (int i = 0; i < n; i++)
    {
        var c = _hoverHits[i];
        if (!c) continue;

        // Filter to "real hand colliders" only
        if (c.GetComponentInParent<HandColliderId>() != null)
            return true;
    }
    return false;
}

public void SetStartGameActive(bool active)
{
    startGameActive = active;
    UpdateVisuals(force: true);
}
private void OnTriggerEnter(Collider other)
{
    var mr = GetComponent<MeshRenderer>();
    /*if (mr == null || !mr.enabled)
    {
        if (debugLogs)
            Debug.Log("[HandLockZone] MeshRenderer missing or disabled -> ignore trigger", this);
        return;
    }*/

    if (debugLogs)
    {
        Debug.Log($"[HandLockZone] OnTriggerEnter on '{name}'. other='{other.name}' " +
                  $"layer={LayerMask.LayerToName(other.gameObject.layer)} " +
                  $"isTrigger={other.isTrigger}", this);
    }

    // Only consider real hand colliders for touching/locking
    var hand = other.GetComponentInParent<HandColliderId>();
    if (hand != null)
    {
        _touchingHandCount = Mathf.Max(0, _touchingHandCount + 1);
        UpdateVisuals(force: true);
    }

    // Gate: do nothing unless StartGame is active
    if (!startGameActive)
    {
        if (debugLogs) Debug.Log("[HandLockZone] StartGame not active -> ignore trigger", this);
        return;
    }


    // stick once on touch
    if (_isLocked && onlyOneHandTotal)
    {
        if (debugLogs) Debug.Log("[HandLockZone] Already locked -> return", this);
        return;
    }

    // hand collider might be on child -> use GetComponentInParent
    //var hand = other.GetComponentInParent<HandColliderId>();
    if (hand == null)
    {
        if (debugLogs)
            Debug.Log("[HandLockZone] No HandColliderId found in parent chain -> return", this);
        return;
    }

    if (debugLogs)
    {
        string stickLockName = hand.stickLock ? hand.stickLock.name : "NULL";
        Debug.Log($"[HandLockZone] HandColliderId found. side={hand.side}, stickLock={stickLockName}", this);
    }

    // Important: only lock for LOCAL player's hand (avoid remote collisions)
    //if (!hand.IsLocalPlayerHand) return;

    if (hand.stickLock == null)
    {
        if (debugLogs) Debug.Log("[HandLockZone] hand.stickLock is NULL -> return", this);
        return;
    }

    // Decide which lock point to use
    Transform lockPoint = GetLockPointFor(hand.side);
    if (lockPoint == null)
    {
        if (debugLogs)
            Debug.Log("[HandLockZone] lockPoint is NULL (default/left/right not assigned) -> return", this);
        return;
    }

    if (debugLogs)
    {
        Debug.Log($"[HandLockZone] Locking now. lockPoint='{lockPoint.name}' pos={lockPoint.position} rot={lockPoint.rotation.eulerAngles}", this);
    }

    // Lock and snap to lockpoint pose ("nice pose")
    hand.stickLock.LockHand(hand.side, lockPoint, snapToTargetPose: true);

    if (enableHaptics)
        hand.stickLock.HapticPulse(hand.side, lockPulseAmplitude, lockPulseDuration);

    _isLocked = true;
    _lockedHand = hand;
    _lockedSide = hand.side;

    if (debugLogs)
        Debug.Log($"[HandLockZone] Locked OK. _isLocked={_isLocked}, _lockedSide={_lockedSide}", this);
}

private void OnTriggerExit(Collider other)
{
    var hand = other.GetComponentInParent<HandColliderId>();
    if (hand == null) return;

    _touchingHandCount = Mathf.Max(0, _touchingHandCount - 1);
    UpdateVisuals(force: true);
}



private Transform GetLockPointFor(HandLock.HandSide side)
{
    // IMPORTANT: Only touch left/right fields if they are actually assigned.
    // Always allow DefaultLockPoint as the fallback.

    Transform chosen = null;

    if (side == HandLock.HandSide.Left)
    {
        chosen = leftLockPoint != null ? leftLockPoint : defaultLockPoint;
    }
    else // Right
    {
        chosen = rightLockPoint != null ? rightLockPoint : defaultLockPoint;
    }

    if (debugLogs)
    {
        Debug.Log($"[HandLockZone] GetLockPointFor side={side} -> chosen={(chosen ? chosen.name : "NULL")} " +
                  $"(default={(defaultLockPoint ? defaultLockPoint.name : "NULL")})", this);
    }

    return chosen;
}
private void UpdateLockedHaptics()
{
    if (!enableHaptics) return;
    if (!_isLocked) return;
    if (_lockedHand == null || _lockedHand.stickLock == null) return;

    if (!_initializedMotion)
    {
        _lastPos = transform.position;
        _lastRot = transform.rotation;
        _initializedMotion = true;
        return;
    }

    float dt = Time.deltaTime;
    if (dt <= 0f) return;

    Vector3 pos = transform.position;
    Quaternion rot = transform.rotation;

    float linearSpeed = (pos - _lastPos).magnitude / dt;

    _lastPos = pos;
    _lastRot = rot;

    if (linearSpeed < moveSpeedThreshold)
    {
        _lockedHand.stickLock.StopContinuousHaptics(_lockedSide);
        return;
    }

    float amp = Mathf.Clamp(linearSpeed * moveToAmplitude, 0f, maxMoveAmplitude);
    _lockedHand.stickLock.SetContinuousHaptics(_lockedSide, amp, movePulseInterval, movePulseDuration);
}


/// Call this from your button / gameplay logic to revert control.
public void Unlock()
{
    if (!_isLocked)
    {
        if (debugLogs) Debug.Log("[HandLockZone] Unlock called but not locked -> return", this);
        return;
    }

    if (debugLogs)
        Debug.Log($"[HandLockZone] Unlocking. side={_lockedSide}, lockedHand={_lockedHand?.name ?? "NULL"}", this);

    if (_lockedHand != null && _lockedHand.stickLock != null)
        _lockedHand.stickLock.UnlockHand(_lockedSide);

    _isLocked = false;
    _lockedHand = null;

    if (debugLogs) Debug.Log("[HandLockZone] Unlock complete.", this);

    UpdateVisuals(force: true);

    if (_lockedHand != null && _lockedHand.stickLock != null)
        _lockedHand.stickLock.StopContinuousHaptics(_lockedSide);

}

}
