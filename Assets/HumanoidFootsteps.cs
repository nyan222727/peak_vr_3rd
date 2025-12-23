using UnityEngine;

public class HumanoidFootsteps : MonoBehaviour
{
    private enum Foot { Left, Right }

    [Header("Audio Emitter")]
    public Transform audioEmitterRoot;  // drag a child transform here (e.g., "AudioEmitter")
    public bool createEmitterIfMissing = true;
    public string emitterName = "AudioEmitter";

    [Header("Wired in Inspector")]
    public Animator animator;                 // Humanoid Animator (required)
    public Transform locomotionRoot;          // The transform that moves in world space (rig root). If null, uses this.transform.
    public FootstepLibrary library;           // ScriptableObject mapping surface -> clips
    public LayerMask groundLayers = ~0;       // Floor layers

    [Header("Step timing (distance clock)")]
    public float minSpeedToStep = 0.15f;      // m/s; below this, no steps
    public float stepDistanceAtWalk = 0.70f;  // meters per step around walking pace
    public float stepDistanceAtRun = 0.45f;   // meters per step at faster speed
    public float speedForRun = 2.0f;          // m/s; lerp between walk/run step distance
    public float minStepInterval = 0.22f;     // seconds; hard guard against spam

    [Header("Foot selection (correct L/R)")]
    [Tooltip("Use foot 'stance' heuristic: foot with smaller horizontal velocity relative to root is considered planted.")]
    public bool useStanceHeuristic = true;

    [Tooltip("If both feet scores are too close, we alternate to avoid weird repeats.")]
    public float stanceScoreTieEpsilon = 0.08f;

    [Header("Ground raycast (works even if feet are below ground)")]
    public float rayStartUp = 0.60f;          // start ray above foot to ensure we’re above ground even if foot penetrates
    public float rayLength = 2.0f;

    [Header("Audio playback")]
    public float spatialBlend = 1f;           // 1 = fully 3D
    public float minVolumeScale = 0.9f;
    public float maxVolumeScale = 1.1f;

    [Tooltip("Two alternating sources prevent cutting off tails. If both are busy, can spawn a temporary one-shot.")]
    public bool spawnTempIfBothBusy = true;

    // Runtime refs
    private Transform _root;
    private Transform _leftFoot;
    private Transform _rightFoot;

    // Audio sources on this GameObject
    private AudioSource _a;
    private AudioSource _b;

    // State
    private Vector3 _lastRootPos;
    private float _distanceAccum;
    private float _stepCooldown;

    private Foot _lastFoot = Foot.Right;

    // For stance heuristic
    private Vector3 _lastLeftPos;
    private Vector3 _lastRightPos;
    private Vector3 _lastRootVel;
    private bool _inited;

private void Awake()
{
    _root = locomotionRoot ? locomotionRoot : transform;

    if (!animator) animator = GetComponentInChildren<Animator>();

    Transform emitter = audioEmitterRoot;

    if (!emitter && createEmitterIfMissing)
    {
        var go = new GameObject(emitterName);
        go.transform.SetParent(transform, false);
        emitter = go.transform;
        audioEmitterRoot = emitter;
    }

    if (!emitter) emitter = transform;

    _a = emitter.gameObject.AddComponent<AudioSource>();
    _b = emitter.gameObject.AddComponent<AudioSource>();
    ConfigureSource(_a);
    ConfigureSource(_b);
}


    private void Start()
    {
        if (!animator || !animator.isHuman)
        {
            Debug.LogError("[HumanoidFootstepsByRootDistance] Animator missing or not Humanoid.");
            enabled = false;
            return;
        }

        _leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        _rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);

        if (!_leftFoot || !_rightFoot)
        {
            Debug.LogError("[HumanoidFootstepsByRootDistance] Could not resolve LeftFoot/RightFoot from Humanoid bones.");
            enabled = false;
            return;
        }

        _lastRootPos = _root.position;
        _lastLeftPos = _leftFoot.position;
        _lastRightPos = _rightFoot.position;
        _inited = true;
    }

    private void ConfigureSource(AudioSource s)
    {
        s.playOnAwake = false;
        s.loop = false;
        s.spatialBlend = spatialBlend;
        s.rolloffMode = AudioRolloffMode.Logarithmic;
        s.dopplerLevel = 0f; // footsteps usually shouldn’t doppler
    }

    private void Update()
    {
        if (!_inited || !library) return;

        float dt = Time.deltaTime;
        _stepCooldown -= dt;

        // Root horizontal speed / distance
        Vector3 rootPos = _root.position;
        Vector3 delta = rootPos - _lastRootPos;
        Vector3 deltaH = new Vector3(delta.x, 0f, delta.z);

        float distH = deltaH.magnitude;
        float speedH = distH / Mathf.Max(dt, 1e-5f);

        // Estimate root velocity for stance heuristic
        _lastRootVel = (rootPos - _lastRootPos) / Mathf.Max(dt, 1e-5f);

        _lastRootPos = rootPos;

        if (speedH < minSpeedToStep)
        {
            _distanceAccum = 0f;
            // Update foot positions for better future velocity estimates
            _lastLeftPos = _leftFoot.position;
            _lastRightPos = _rightFoot.position;
            return;
        }

        // Choose step distance based on speed (walk/run blend)
        float tRun = Mathf.Clamp01(speedH / Mathf.Max(speedForRun, 1e-5f));
        float stepDist = Mathf.Lerp(stepDistanceAtWalk, stepDistanceAtRun, tRun);

        _distanceAccum += distH;

        // Trigger step event by distance clock
        if (_distanceAccum >= stepDist && _stepCooldown <= 0f)
        {
            _distanceAccum = 0f;
            _stepCooldown = minStepInterval;

            Foot footToPlay = ChooseFoot(dt);
            PlayFootstep(footToPlay);
            _lastFoot = footToPlay;
        }

        // Update last foot positions
        _lastLeftPos = _leftFoot.position;
        _lastRightPos = _rightFoot.position;
    }

    private Foot ChooseFoot(float dt)
    {
        if (!useStanceHeuristic)
        {
            return (_lastFoot == Foot.Left) ? Foot.Right : Foot.Left;
        }

        // Foot horizontal velocity relative to root
        Vector3 lPos = _leftFoot.position;
        Vector3 rPos = _rightFoot.position;

        Vector3 lVel = (lPos - _lastLeftPos) / Mathf.Max(dt, 1e-5f);
        Vector3 rVel = (rPos - _lastRightPos) / Mathf.Max(dt, 1e-5f);

        // Remove root motion (approx)
        Vector3 lRel = lVel - _lastRootVel;
        Vector3 rRel = rVel - _lastRootVel;

        float lScore = new Vector2(lRel.x, lRel.z).magnitude; // lower = more planted
        float rScore = new Vector2(rRel.x, rRel.z).magnitude;

        // If too close, alternate to avoid repeating the same foot from jitter
        if (Mathf.Abs(lScore - rScore) <= stanceScoreTieEpsilon)
            return (_lastFoot == Foot.Left) ? Foot.Right : Foot.Left;

        return (lScore < rScore) ? Foot.Left : Foot.Right;
    }

    private void PlayFootstep(Foot which)
    {
        Transform footT = (which == Foot.Left) ? _leftFoot : _rightFoot;

        // Raycast from above foot downward (works even if foot is below ground)
        Vector3 origin = footT.position + Vector3.up * rayStartUp;
        bool hitOk = Physics.Raycast(origin, Vector3.down, out RaycastHit hit, rayLength, groundLayers, QueryTriggerInteraction.Ignore);

        FootstepSurface surface = FootstepSurface.Default;
        Vector3 pos = footT.position;

        if (hitOk)
        {
            pos = hit.point;

            // Surface mapping via component (recommended)
            var tag = hit.collider.GetComponentInParent<FootstepSurfaceTag>();
            if (tag) surface = tag.surface;
        }

        if (!library.TryGet(surface, out var entry) || entry == null || entry.clips == null || entry.clips.Length == 0)
        {
            // fallback to Default if missing
            if (!library.TryGet(FootstepSurface.Default, out entry) || entry == null || entry.clips == null || entry.clips.Length == 0)
                return;
        }

        AudioClip clip = entry.clips[Random.Range(0, entry.clips.Length)];
        if (!clip) return;

        float volScale = Random.Range(minVolumeScale, maxVolumeScale);
        float pitch = Random.Range(entry.pitchMin, entry.pitchMax);

        // Play through an available source, else spawn temp
        AudioSource src = GetAvailableSource();
        if (src == null)
        {
            if (!spawnTempIfBothBusy) return;
            PlayTempOneShot(pos, clip, entry.volume * volScale, pitch);
            return;
        }

        src.transform.position = pos;
        src.clip = clip;
        src.volume = entry.volume * volScale;
        src.pitch = pitch;
        src.Play();
    }

    private AudioSource GetAvailableSource()
    {
        // Prefer the one not playing; otherwise return null
        if (!_a.isPlaying) return _a;
        if (!_b.isPlaying) return _b;
        return null;
    }

    private void PlayTempOneShot(Vector3 pos, AudioClip clip, float vol, float pitch)
    {
        var go = new GameObject("Footstep_OneShot");
        go.transform.position = pos;

        var s = go.AddComponent<AudioSource>();
        ConfigureSource(s);
        s.clip = clip;
        s.volume = vol;
        s.pitch = pitch;
        s.Play();

        Destroy(go, clip.length + 0.2f);
    }
}
