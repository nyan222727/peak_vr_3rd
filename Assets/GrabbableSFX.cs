using UnityEngine;
using Oculus.Interaction;

[DisallowMultipleComponent]
public class GrabbableGrabReleaseSfx : MonoBehaviour
{
    [Header("Debug")]
    public bool logImpactDebug = true;

    [Tooltip("Logs at most once every N seconds to avoid spam.")]
    public float logCooldownSeconds = 0.10f;
    [Header("Refs (optional)")]
    [Tooltip("If left empty, will auto-find Grabbable on this GameObject.")]
    public Grabbable grabbable;

    [Tooltip("If left empty, will auto-find/create an AudioSource on this GameObject.")]
    public AudioSource audioSource;

    [Header("Clips")]
    [Tooltip("Plays immediately when grabbed (Select). Can be 1 or many.")]
    public AudioClip[] grabClips;

    [Header("Release Impact Clips (Tiered)")]
    [Tooltip("Small impact: e.g., placing on desk. Requires the other collider to have a Rigidbody.")]
    public AudioClip[] smallImpactClips;

    [Tooltip("Big impact: e.g., dropping to ground. Does NOT require other Rigidbody (so ground counts).")]
    public AudioClip[] bigImpactClips;

    [Header("Audio Tuning")]
    [Range(0f, 1f)] public float volume = 1f;
    public float pitchMin = 0.98f;
    public float pitchMax = 1.02f;

    [Tooltip("Ensure spatial audio (3D).")]
    public bool forceSpatial3D = true;

    [Header("Release Impact Detection")]
    [Tooltip("After unselect, we wait up to this long for an impact before giving up (seconds).")]
    public float impactListenWindow = 1.2f;

    [Tooltip("Small impact minimum relative collision speed. Set 0 to disable.")]
    public float minSmallImpactSpeed = 0.12f;

    [Tooltip("Big impact minimum relative collision speed. Set 0 to disable.")]
    public float minBigImpactSpeed = 0.20f;

    [Tooltip("If time since release is >= this, treat first impact as BIG (drop).")]
    public float bigImpactMinTimeSinceRelease = 0.25f;

    [Tooltip("Prevents spamming if multiple contacts happen quickly.")]
    public float minIntervalBetweenPlays = 0.05f;

    [Tooltip("Count trigger enters as impact too (optional). Big impact via triggers is usually not desired.")]
    public bool treatTriggersAsImpact = false;

    private bool _pendingImpact;
    private float _unselectTime;
    private float _lastPlayTime;

    private Rigidbody _selfRb;
    private float _lastLogTime;


    private void Awake()
    {
        if (!grabbable) grabbable = GetComponent<Grabbable>();

        if (!audioSource)
        {
            audioSource = GetComponent<AudioSource>();
            if (!audioSource) audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        if (forceSpatial3D) audioSource.spatialBlend = 1f;

        _selfRb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        if (grabbable != null)
            grabbable.WhenPointerEventRaised += OnPointerEventRaised;
    }

    private void OnDisable()
    {
        if (grabbable != null)
            grabbable.WhenPointerEventRaised -= OnPointerEventRaised;
    }

private void LogImpact(string msg)
{
    if (!logImpactDebug) return;
    if ((Time.time - _lastLogTime) < logCooldownSeconds) return;

    _lastLogTime = Time.time;
    Debug.Log($"[GrabbableSfx:{name}] {msg}", this);
}


    private void Update()
    {
        if (_pendingImpact && (Time.time - _unselectTime) > impactListenWindow)
        {
            _pendingImpact = false;
        }
    }

    private void OnPointerEventRaised(PointerEvent evt)
    {
        switch (evt.Type)
        {
            case PointerEventType.Select:
                PlayRandom(grabClips);
                _pendingImpact = false;
                break;

            case PointerEventType.Unselect:
            case PointerEventType.Cancel:
                _pendingImpact = true;
                _unselectTime = Time.time;
                break;
        }
    }

private void OnCollisionEnter(Collision collision)
{
    if (!_pendingImpact) return;

    float tSinceRelease = Time.time - _unselectTime;
    float relSpeed = collision.relativeVelocity.magnitude;
    bool isBig = tSinceRelease >= bigImpactMinTimeSinceRelease;

    // Who did we hit?
    var otherRb = collision.rigidbody;
    string otherName = collision.collider ? collision.collider.name : "(no collider)";
    bool otherHasRb = otherRb != null;

    if (isBig)
    {
        bool speedOk = (minBigImpactSpeed <= 0f) || (relSpeed >= minBigImpactSpeed);

        LogImpact(
            $"BIG? yes | t={tSinceRelease:F3}s (th={bigImpactMinTimeSinceRelease:F3}) " +
            $"| v={relSpeed:F3} (min={minBigImpactSpeed:F3}) | speedOk={speedOk} " +
            $"| hit={otherName} | otherRB={otherHasRb}"
        );

        if (!speedOk) return;

        TryPlayImpactOnce(bigImpactClips);
    }
    else
    {
        bool rbOk = otherHasRb;
        bool speedOk = (minSmallImpactSpeed <= 0f) || (relSpeed >= minSmallImpactSpeed);

        LogImpact(
            $"BIG? no (SMALL) | t={tSinceRelease:F3}s (th={bigImpactMinTimeSinceRelease:F3}) " +
            $"| v={relSpeed:F3} (min={minSmallImpactSpeed:F3}) | rbOk={rbOk} | speedOk={speedOk} " +
            $"| hit={otherName} | otherRB={otherHasRb}"
        );

        if (!rbOk) return;
        if (!speedOk) return;

        TryPlayImpactOnce(smallImpactClips);
    }
}

private void OnTriggerEnter(Collider other)
{
    if (!treatTriggersAsImpact) return;
    if (!_pendingImpact) return;

    float tSinceRelease = Time.time - _unselectTime;
    bool isBig = tSinceRelease >= bigImpactMinTimeSinceRelease;
    bool otherHasRb = other.attachedRigidbody != null;

    LogImpact(
        $"TRIGGER impact | BIG? {(isBig ? "yes" : "no")} | t={tSinceRelease:F3}s (th={bigImpactMinTimeSinceRelease:F3}) " +
        $"| otherRB={otherHasRb} | other={other.name}"
    );

    if (!isBig)
    {
        if (!otherHasRb) return;
        TryPlayImpactOnce(smallImpactClips);
    }
    else
    {
        TryPlayImpactOnce(bigImpactClips);
    }
}


    private void TryPlayImpactOnce(AudioClip[] clips)
    {
        if ((Time.time - _lastPlayTime) < minIntervalBetweenPlays) return;

        _pendingImpact = false;
        PlayRandom(clips);
    }

    private void PlayRandom(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0) return;
        if ((Time.time - _lastPlayTime) < minIntervalBetweenPlays) return;

        var clip = clips[Random.Range(0, clips.Length)];
        if (!clip) return;

        _lastPlayTime = Time.time;

        audioSource.pitch = Random.Range(pitchMin, pitchMax);
        audioSource.PlayOneShot(clip, volume);
    }
}
