using UnityEngine;
using System.Collections;
using System.Reflection;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;

/// <summary>
/// LOCAL-ONLY version of GhostGrabInterference:
/// - No Fusion, no NetworkObject, no RPC.
/// - Intended for VR host-only gameplay where mobile clients observe via a separate networked proxy.
/// - All effects (shake + final impact) are applied locally to this real interactable object.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class LocalGhostGrabInterference : MonoBehaviour
{
    public bool IsGrabbedNow => IsProbablyGrabbed();

    [Header("Debug")]
    [SerializeField] private bool verboseLogs = true;

    [Header("Charge / Combo")]
    [SerializeField] private int hitsToTriggerFinal = 3;
    [SerializeField] private float comboWindowSeconds = 1.0f;

    [Header("Intermediate Shake (visual)")]
    [Tooltip("Set this to a CHILD transform (mesh root). Do NOT shake the physics root.")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private float shakeDuration = 0.18f;
    [SerializeField] private float maxPosShake = 0.02f;
    [SerializeField] private float maxRotShakeDeg = 8.0f;

    [Header("Final Impact")]
    [SerializeField] private float teleportRadius = 1.5f;
    [SerializeField] private float teleportUpOffset = 0.3f;
    [SerializeField] private float throwForce = 2.0f;
    [SerializeField] private float disableGrabDuration = 0.15f;

    // Local “held” cache (optional, for debugging/other logic).
    public bool IsHeldLocal { get; private set; }

    private Rigidbody _rb;
    private Grabbable _grabbable;
    private GrabInteractable _grabInteractable;
    private HandGrabInteractable _handGrabInteractable;

    private int _comboHits = 0;
    private double _lastHitTime = -999;

    private Coroutine _shakeRoutine;
    private Vector3 _shakeBaseLocalPos;
    private Quaternion _shakeBaseLocalRot;

    private bool _lastHeldLocal = false;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();

        _grabbable = GetComponentInChildren<Grabbable>();
        _grabInteractable = GetComponentInChildren<GrabInteractable>();
        _handGrabInteractable = GetComponentInChildren<HandGrabInteractable>();

        if (visualRoot == null) visualRoot = transform;

        _shakeBaseLocalPos = visualRoot.localPosition;
        _shakeBaseLocalRot = visualRoot.localRotation;
    }

    private void FixedUpdate()
    {
        // Track local held state (for debugging + to keep parity with the networked version).
        bool heldNow = IsProbablyGrabbed();
        IsHeldLocal = heldNow;

        if (heldNow != _lastHeldLocal)
        {
            _lastHeldLocal = heldNow;
            VLog($"IsHeldLocal -> {heldNow} (rb.isKinematic={_rb.isKinematic})");
        }
    }

    /// <summary>
    /// Called by GhostInterferenceManager on the VR host when the ghost trigger arrives.
    /// </summary>
    public void RegisterGhostHit(Vector3 playerPos, Vector3 playerForward)
    {
        if (verboseLogs)
            Debug.Log($"[LocalGhostGrabInterference] RegisterGhostHit -> {name} | grabbed={IsGrabbedNow}");

        HandleGhostHitLocal(playerPos, playerForward);
    }

    private void HandleGhostHitLocal(Vector3 playerPos, Vector3 playerForward)
    {
        bool held = IsHeldLocal || IsProbablyGrabbed(); // safety

        VLog($"HandleGhostHitLocal. held={held} frame={Time.frameCount}");

        if (!held)
        {
            VLog("HandleGhostHitLocal: ignored because object not held.");
            return;
        }

        double now = Time.timeAsDouble;

        if (now - _lastHitTime > comboWindowSeconds)
            _comboHits = 0;

        _comboHits++;
        _lastHitTime = now;

        float charge01 = Mathf.Clamp01((float)_comboHits / Mathf.Max(1, hitsToTriggerFinal));

        int seed = Time.frameCount ^ (_comboHits * 997);
        PlayShake(charge01, seed);

        VLog($"Hit accepted. comboHits={_comboHits}/{hitsToTriggerFinal} charge01={charge01:0.00}");

        if (_comboHits >= hitsToTriggerFinal)
        {
            _comboHits = 0;

            int whichMode = Random.Range(0, 2);
            VLog($"FINAL IMPACT! mode={whichMode}");
            StartCoroutine(FinalImpactRoutine(playerPos, playerForward, whichMode));
        }
    }

    private void PlayShake(float charge01, int seed)
    {
        if (_shakeRoutine != null) StopCoroutine(_shakeRoutine);
        _shakeRoutine = StartCoroutine(ShakeVisualRoutine(charge01, seed));
    }

    private IEnumerator ShakeVisualRoutine(float charge01, int seed)
    {
        float posAmp = maxPosShake * charge01;
        float rotAmp = maxRotShakeDeg * charge01;

        float t0 = Time.time;
        float tEnd = t0 + shakeDuration;

        _shakeBaseLocalPos = visualRoot.localPosition;
        _shakeBaseLocalRot = visualRoot.localRotation;

        var rng = new System.Random(seed);

        while (Time.time < tEnd)
        {
            float u = (Time.time - t0) / shakeDuration;
            float env = Mathf.Sin(u * Mathf.PI);

            float rx = ((float)rng.NextDouble() * 2f - 1f) * rotAmp * env;
            float ry = ((float)rng.NextDouble() * 2f - 1f) * rotAmp * env;
            float rz = ((float)rng.NextDouble() * 2f - 1f) * rotAmp * env;

            float px = ((float)rng.NextDouble() * 2f - 1f) * posAmp * env;
            float py = ((float)rng.NextDouble() * 2f - 1f) * posAmp * env;
            float pz = ((float)rng.NextDouble() * 2f - 1f) * posAmp * env;

            visualRoot.localPosition = _shakeBaseLocalPos + new Vector3(px, py, pz);
            visualRoot.localRotation = _shakeBaseLocalRot * Quaternion.Euler(rx, ry, rz);

            yield return null;
        }

        visualRoot.localPosition = _shakeBaseLocalPos;
        visualRoot.localRotation = _shakeBaseLocalRot;
        _shakeRoutine = null;
    }

    private IEnumerator FinalImpactRoutine(Vector3 playerPos, Vector3 playerForward, int whichMode)
    {
        DisableGrabComponentsTemporarily();

        _rb.isKinematic = false;
        _rb.velocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;

        if (whichMode == 0)
        {
            _rb.AddForce(Vector3.down * 1.5f, ForceMode.VelocityChange);
        }
        else
        {
            Vector3 flatForward = new Vector3(playerForward.x, 0f, playerForward.z);
            if (flatForward.sqrMagnitude < 0.0001f) flatForward = Vector3.forward;
            flatForward.Normalize();

            Vector3 side = Vector3.Cross(Vector3.up, flatForward).normalized;
            float sideSign = Random.value > 0.5f ? 1f : -1f;

            float forwardDist = Random.Range(0.4f * teleportRadius, teleportRadius);
            float sideDist = Random.Range(0.4f * teleportRadius, teleportRadius);

            Vector3 targetPos =
                playerPos
                + Vector3.up * teleportUpOffset
                + flatForward * forwardDist
                + side * sideSign * sideDist;

            transform.position = targetPos;

            Vector3 awayDir = (targetPos - playerPos).normalized;
            _rb.AddForce(awayDir * throwForce, ForceMode.VelocityChange);
        }

        yield return new WaitForSeconds(disableGrabDuration);

        ReEnableGrabComponents();
    }

    private bool _grabWasEnabled;
    private bool _grabInteractableWasEnabled;
    private bool _handGrabWasEnabled;

    private void DisableGrabComponentsTemporarily()
    {
        if (_grabbable != null) { _grabWasEnabled = _grabbable.enabled; _grabbable.enabled = false; }
        if (_grabInteractable != null) { _grabInteractableWasEnabled = _grabInteractable.enabled; _grabInteractable.enabled = false; }
        if (_handGrabInteractable != null) { _handGrabWasEnabled = _handGrabInteractable.enabled; _handGrabInteractable.enabled = false; }
    }

    private void ReEnableGrabComponents()
    {
        if (_grabbable != null) _grabbable.enabled = _grabWasEnabled;
        if (_grabInteractable != null) _grabInteractable.enabled = _grabInteractableWasEnabled;
        if (_handGrabInteractable != null) _handGrabInteractable.enabled = _handGrabWasEnabled;
    }

    private bool IsProbablyGrabbed()
    {
        return IsSelectedByReflection(_grabbable) ||
               IsSelectedByReflection(_grabInteractable) ||
               IsSelectedByReflection(_handGrabInteractable);
    }

    private static bool IsSelectedByReflection(object obj)
    {
        if (obj == null) return false;

        var t = obj.GetType();

        PropertyInfo p = t.GetProperty("SelectingPointsCount", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (p != null && p.PropertyType == typeof(int))
        {
            int v = (int)p.GetValue(obj);
            return v > 0;
        }

        p = t.GetProperty("IsSelected", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (p != null && p.PropertyType == typeof(bool))
        {
            return (bool)p.GetValue(obj);
        }

        p = t.GetProperty("SelectingPoints", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (p != null)
        {
            var val = p.GetValue(obj);
            if (val is System.Collections.ICollection col) return col.Count > 0;
        }

        return false;
    }

    private void VLog(string msg)
    {
        if (!verboseLogs) return;
        Debug.Log($"[LocalGhostGrabInterference:{name}] {msg}");
    }
}
