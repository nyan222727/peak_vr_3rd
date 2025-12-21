using UnityEngine;
using Fusion;
using UnityEngine.VFX;

[RequireComponent(typeof(Rigidbody))]
public class PotionTwig : NetworkBehaviour
{
    [Header("Splat settings")]
    [SerializeField] private float splatLinearSpeedThreshold = 1.5f;   // m/s (transform-driven)
    [SerializeField] private float splatAngularSpeedThreshold = 250f;  // deg/s
    [SerializeField] private float minTimeBetweenSplats = 0.4f;
    [SerializeField] private bool requireGrabbed = true;              // only allow splat while grabbed

    [Header("VFX - Potion charged")]
    [SerializeField] private VisualEffect chargedVfx;           // VFX Graph
    [SerializeField] private ParticleSystem chargedParticles;   // optional if you use ParticleSystem too

    private bool _lastVfxHasPotion;
    private bool _vfxInit;


    [Header("Debug")]
    [SerializeField] private bool verboseLogs = true;
    [SerializeField] private bool logSpeedWhileCharged = true;
    [SerializeField] private float speedLogInterval = 0.25f;

    [Networked] public bool HasPotion { get; set; }


    private Rigidbody _rb;
    private float _lastSplatTime;
    private float _nextSpeedLogTime;

    private int _currentRegionIndex = -1;   // -1 = not in any region
    private bool _insideBottle = false;

    private Vector3 _lastPos;
    private Quaternion _lastRot;
    private bool _hasPoseHistory;

    public override void Spawned()
    {
        _rb = GetComponent<Rigidbody>();
        _lastPos = transform.position;
        _lastRot = transform.rotation;
        _hasPoseHistory = true;

        Log($"Spawned. HasStateAuthority={Object.HasStateAuthority}");
        _lastVfxHasPotion = HasPotion;
        _vfxInit = true;
        ApplyChargedVfx(HasPotion);
    }

private void ApplyChargedVfx(bool on)
{
    if (chargedVfx != null)
    {
        if (on) chargedVfx.Play();
        else chargedVfx.Stop();
    }

    if (chargedParticles != null)
    {
        if (on) chargedParticles.Play();
        else chargedParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}

public override void Render()
{
    if (!_vfxInit)
    {
        _lastVfxHasPotion = HasPotion;
        _vfxInit = true;
        ApplyChargedVfx(HasPotion);
        return;
    }

    if (HasPotion != _lastVfxHasPotion)
    {
        _lastVfxHasPotion = HasPotion;
        ApplyChargedVfx(HasPotion);
    }
}



    // Called by bottle trigger when twig enters / stays
    public void StartDip()
    {
        if (!_insideBottle)
        {
            _insideBottle = true;
            Log("StartDip: ENTER bottle zone.");
        }
    }

    // Called by bottle trigger when twig exits
    public void FinishDip()
    {
        if (!_insideBottle)
        {
            Log("FinishDip: EXIT bottle zone, but twig was not marked inside (ignored).");
            return;
        }

        _insideBottle = false;
        Log("FinishDip: EXIT bottle zone (valid).");

        if (Object != null && Object.HasStateAuthority)
        {
            HasPotion = true;
            Log("HasPotion = TRUE (authority).");
        }
        else
        {
            Log("Not authority -> not setting HasPotion (expected in Fusion shared mode).");
        }
    }

    private void Update()
    {
        // Only authority decides splats + consumes potion
        if (Object == null || !Object.HasStateAuthority)
            return;

        if (_rb == null)
            _rb = GetComponent<Rigidbody>();

        float dt = Time.deltaTime;
        if (!_hasPoseHistory || dt <= 0f)
        {
            _lastPos = transform.position;
            _lastRot = transform.rotation;
            _hasPoseHistory = true;
            return;
        }

        // Transform-driven “velocity” (works even when Rigidbody is kinematic)
        Vector3 pos = transform.position;
        Quaternion rot = transform.rotation;

        float linearSpeed = (pos - _lastPos).magnitude / dt;

        Quaternion dq = rot * Quaternion.Inverse(_lastRot);
        dq.ToAngleAxis(out float angleDeg, out _);
        if (angleDeg > 180f) angleDeg = 360f - angleDeg;
        float angularSpeed = angleDeg / dt;

        _lastPos = pos;
        _lastRot = rot;

        bool grabbed = _rb != null && _rb.isKinematic; // your grab system flips this
        if (requireGrabbed && !grabbed)
            return;

        if (HasPotion && logSpeedWhileCharged && Time.time >= _nextSpeedLogTime)
        {
            _nextSpeedLogTime = Time.time + speedLogInterval;
            Log($"Speed: lin={linearSpeed:F2} m/s, ang={angularSpeed:F0} deg/s, region={_currentRegionIndex}, grabbed={grabbed}");
        }

        if (!HasPotion)
            return;

        bool gesture = (linearSpeed >= splatLinearSpeedThreshold) || (angularSpeed >= splatAngularSpeedThreshold);
        if (!gesture)
            return;

        if (Time.time - _lastSplatTime < minTimeBetweenSplats)
        {
            Log($"Gesture but COOLDOWN ({Time.time - _lastSplatTime:F2}s < {minTimeBetweenSplats}s).");
            return;
        }

        _lastSplatTime = Time.time;

        if (_currentRegionIndex >= 0)
        {
            Log($"SPLAT SUCCESS in region {_currentRegionIndex}! lin={linearSpeed:F2}, ang={angularSpeed:F0}");

            var mgr = PotionRitualManager.Instance;
            if (mgr != null) mgr.RegisterSplat(_currentRegionIndex);
            else Log("PotionRitualManager.Instance == null (manager missing / not initialized).");

            HasPotion = false;
            Log("HasPotion = FALSE (consumed).");
            VfxPlayer.Instance?.PlayBurst();

        }
        else
        {
            Log($"Gesture detected but NOT in a region. lin={linearSpeed:F2}, ang={angularSpeed:F0} (potion kept).");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<PotionSplatRegion>(out var region))
        {
            _currentRegionIndex = region.RegionIndex;
            Log($"ENTER region {_currentRegionIndex}");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<PotionSplatRegion>(out var region) && region.RegionIndex == _currentRegionIndex)
        {
            Log($"EXIT region {_currentRegionIndex}");
            _currentRegionIndex = -1;
        }
    }

    private void Log(string msg)
    {
        if (!verboseLogs) return;
        var id = (Object != null) ? Object.Id.ToString() : "no-netobj";
        Debug.Log($"[PotionTwig:{name}] [NetId:{id}] {msg}", this);
    }
}
