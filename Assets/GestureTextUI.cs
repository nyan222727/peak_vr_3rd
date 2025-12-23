using UnityEngine;

public class GestureTextUI : MonoBehaviour
{
    public enum AxisMode { WorldUp, HeadUp }
    public enum LocalAxis { Up, Forward, Right }

    [System.Serializable]
    public class HandRefs
    {
        public Transform hand;     // required
        public Transform palm;     // optional; if null uses hand

        [Header("Palm Normal Axis (in palm transform local space)")]
        public LocalAxis palmNormalAxis = LocalAxis.Up;
        public bool invertPalmAxis = false;
    }

    private enum Phase { Idle, Armed, Rising, Holding, Showing, Cooldown }

    [Header("Wired in Inspector")]
    public FocusRaycaster focusRaycaster;
    public NotePanelView notePanelPrefab;
    public Transform head;

    public HandRefs leftHand;
    public HandRefs rightHand;

    [Header("Which hand can summon?")]
    public bool allowLeft = true;
    public bool allowRight = true;

    [Header("Focus (Stage A)")]
    [Tooltip("How long we allow focus to be lost while still continuing the gesture.")]
    public float focusGraceSeconds = 0.20f;

    [Header("Chest Start Zone (forgiving)")]
    [Tooltip("Offset from head to approximate chest center (local to head).")]
    public Vector3 chestOffsetFromHead = new Vector3(0f, -0.25f, 0.15f);

    [Tooltip("Extra downward allowance (lets player start lower than chest).")]
    public float chestExtraDown = 0.12f;

    [Tooltip("Zone radius (bigger = less strict).")]
    public float chestRadius = 0.22f;

    [Header("Gesture Detection (Stage B)")]
    public AxisMode upAxisMode = AxisMode.HeadUp;

    [Tooltip("Minimum upward speed to count as a 'pull out' motion (m/s).")]
    public float minUpSpeed = 0.55f;

    [Tooltip("Minimum upward travel to count as a 'pull out' motion (meters).")]
    public float minUpTravel = 0.10f;

    [Tooltip("Max time allowed from arming to completing the upward motion.")]
    public float gestureWindowSeconds = 0.60f;

    [Header("Hold Confirm (short)")]
    [Tooltip("Require palm-up to be stable for this long before showing.")]
    public float palmUpHoldSeconds = 0.18f;

    [Tooltip("Palm is considered up if dot(palmNormal, upAxis) >= this.")]
    [Range(-1f, 1f)] public float palmUpDotThreshold = 0.55f;

    [Header("Show / Hide")]
    public float showFadeIn = 0.15f;
    public float hideFadeOut = 0.12f;

    [Tooltip("After showing, keep it visible at least this long to avoid flicker.")]
    public float minVisibleSeconds = 0.60f;

    [Tooltip("Hide if palm-up condition breaks for this grace time.")]
    public float hideGraceSeconds = 0.18f;

    [Tooltip("Also hide if hand is farther than this from head for this grace time (reading moved away).")]
    public float hideHandAwayDistance = 0.75f;

    [Header("Cooldown")]
    public float cooldownSeconds = 0.80f;

    [Header("Hand-follow + Optional Pull-out Animation")]
    public bool enablePullOutAnimation = true;
    public float pullOutDuration = 0.18f;

    [Tooltip("Local offset from palm/hand to place the panel.")]
    public Vector3 panelOffsetLocal = new Vector3(0f, 0.02f, 0.04f);

    [Tooltip("Billboard panel toward head with smoothing.")]
    public float faceHeadSlerp = 14f;

    [Header("Canvas Rotation (Fix)")]
    [Tooltip("If true, canvas follows palm orientation (like a note on your hand).")]
    public bool lockRotationToPalm = true;

    [Tooltip("Extra rotation applied after palm rotation (degrees). Use this to match your prefab's 'horizontal' orientation.")]
    public Vector3 panelRotationOffsetEuler = Vector3.zero;

    [Tooltip("Optional: tilt the note for readability (degrees). Positive rotates around palm right axis.")]
    public float palmTiltDegrees = 15f;

    [Tooltip("If true, tilt direction is chosen to tilt toward the head automatically.")]
    public bool autoTiltTowardHead = true;

    [Header("UI SFX (optional)")]
    public AudioClip showUiSfx;
    public AudioClip hideUiSfx;
    [Range(0f, 1f)] public float uiSfxVolume = 1f;

    // AudioSource should be on this GestureTextUI GameObject (you can add one manually).
    // If null, it will auto-find/create on this same object.
    [Tooltip("If empty, we'll auto-find an AudioSource on the instantiated note panel prefab (canvas).")]
    public AudioSource uiSfxSourceOnPanel;

    public bool autoFindSfxSourceInPanelChildren = true;

    // Runtime
    private NotePanelView _panel;
    private Phase _phase = Phase.Idle;

    private ObjectDescription _focused;
    private float _focusLostTimer;

    private Transform _activeHand;
    private HandRefs _activeRefs;

    private Vector3 _lastHandPos;
    private float _armedTimer;
    private float _upTravelAccum;
    private float _holdTimer;

    private float _visibleTimer;
    private float _hideGraceTimer;
    private float _cooldownTimer;

    private float _alpha;
    private float _animT;
    private Vector3 _animFromPos;
    private Quaternion _animFromRot;

    private void Awake()
    {
        if (!head) head = Camera.main ? Camera.main.transform : null;

        _panel = Instantiate(notePanelPrefab);
        _panel.gameObject.SetActive(true);
        _panel.SetAlpha(0f);
        // Auto-find AudioSource on the note panel prefab (canvas) if not assigned
        if (uiSfxSourceOnPanel == null && _panel != null)
        {
            uiSfxSourceOnPanel = _panel.GetComponent<AudioSource>();
            if (uiSfxSourceOnPanel == null && autoFindSfxSourceInPanelChildren)
                uiSfxSourceOnPanel = _panel.GetComponentInChildren<AudioSource>(true);
        }
        _alpha = 0f;
    }

    private void Update()
    {
        if (!focusRaycaster) return;
        if (!head) return;

        UpdateFocus();

        if (_phase == Phase.Cooldown)
        {
            _cooldownTimer -= Time.deltaTime;
            if (_cooldownTimer <= 0f) _phase = Phase.Idle;
            FadeTo(0f);
            return;
        }

        if (_phase == Phase.Showing)
        {
            _visibleTimer += Time.deltaTime;
            UpdatePanelPoseFollow();

            bool palmUp = IsPalmUp(_activeRefs);
            bool away = (_activeHand && Vector3.Distance(_activeHand.position, head.position) > hideHandAwayDistance);

            // Hide conditions with short grace (and respect minimum visible time)
            if (_visibleTimer >= minVisibleSeconds)
            {
                if (!palmUp || away || _focused == null)
                    _hideGraceTimer += Time.deltaTime;
                else
                    _hideGraceTimer = 0f;

                if (_hideGraceTimer >= hideGraceSeconds)
                    BeginHide();
            }

            FadeTo(1f);
            return;
        }

        // Not showing => drive gesture state machine
        TryAdvanceGesture();
        FadeTo(0f);
    }

    private void UpdateFocus()
    {
        var curr = focusRaycaster.Current;
        if (curr != null)
        {
            _focused = curr;
            _focusLostTimer = 0f;
        }
        else
        {
            _focusLostTimer += Time.deltaTime;
            if (_focusLostTimer > focusGraceSeconds)
                _focused = null;
        }
    }

    private void TryAdvanceGesture()
    {
        // Need a focused target to even start / continue
        if (_focused == null)
        {
            ResetGesture();
            return;
        }

        // Choose a hand that is in/near chest zone (forgiving) and allowed
        Transform candidateHand = null;
        HandRefs candidateRefs = null;

        Vector3 chestCenter = head.TransformPoint(chestOffsetFromHead);
        float extraDown = chestExtraDown;

        if (allowLeft && leftHand != null && leftHand.hand)
        {
            if (InChestZone(leftHand.hand.position, chestCenter, chestRadius, extraDown))
            {
                candidateHand = leftHand.hand;
                candidateRefs = leftHand;
            }
        }

        if (allowRight && rightHand != null && rightHand.hand)
        {
            if (InChestZone(rightHand.hand.position, chestCenter, chestRadius, extraDown))
            {
                // If both are valid, pick the closer one
                if (!candidateHand ||
                    Vector3.Distance(rightHand.hand.position, chestCenter) < Vector3.Distance(candidateHand.position, chestCenter))
                {
                    candidateHand = rightHand.hand;
                    candidateRefs = rightHand;
                }
            }
        }

        Vector3 upAxis = GetUpAxis();

        switch (_phase)
        {
            case Phase.Idle:
            {
                if (candidateHand == null) return;

                // Arm
                _activeHand = candidateHand;
                _activeRefs = candidateRefs;
                _lastHandPos = _activeHand.position;
                _armedTimer = 0f;
                _upTravelAccum = 0f;
                _holdTimer = 0f;
                _phase = Phase.Armed;
                break;
            }

            case Phase.Armed:
            {
                if (_activeHand == null) { ResetGesture(); return; }

                _armedTimer += Time.deltaTime;
                if (_armedTimer > gestureWindowSeconds) { ResetGesture(); return; }

                Vector3 currPos = _activeHand.position;
                Vector3 vel = (currPos - _lastHandPos) / Mathf.Max(Time.deltaTime, 1e-5f);
                float upSpeed = Vector3.Dot(vel, upAxis);

                // Accumulate upward travel only (ignore downward jitter)
                float upDelta = Vector3.Dot(currPos - _lastHandPos, upAxis);
                if (upDelta > 0f) _upTravelAccum += upDelta;

                _lastHandPos = currPos;

                // Rising trigger: either speed or travel satisfies
                if (upSpeed >= minUpSpeed || _upTravelAccum >= minUpTravel)
                {
                    _phase = Phase.Rising;
                }

                // If player leaves chest zone entirely, don’t be strict: allow it, but still within window.
                break;
            }

            case Phase.Rising:
            {
                if (_activeHand == null) { ResetGesture(); return; }

                _armedTimer += Time.deltaTime;
                if (_armedTimer > gestureWindowSeconds) { ResetGesture(); return; }

                // Confirm palm-up hold
                if (IsPalmUp(_activeRefs))
                {
                    _holdTimer += Time.deltaTime;
                    if (_holdTimer >= palmUpHoldSeconds)
                    {
                        BeginShow();
                    }
                }
                else
                {
                    _holdTimer = 0f;
                }
                break;
            }

            case Phase.Holding:
                // (not used; Rising handles hold)
                break;
        }
    }

    private void BeginShow()
    {
        if (_focused == null || _activeHand == null) { ResetGesture(); return; }

        _panel.SetContent(_focused.Title, _focused.Body);

        _visibleTimer = 0f;
        _hideGraceTimer = 0f;

        if (enablePullOutAnimation)
        {
            _animT = 0f;
            _animFromPos = head.TransformPoint(chestOffsetFromHead);
            _animFromRot = Quaternion.LookRotation((head.position - _animFromPos).normalized, GetUpAxis());
        }

        _phase = Phase.Showing;
        PlayUiSfx(showUiSfx);
    }

    private void BeginHide()
    {
        PlayUiSfx(hideUiSfx);
        _phase = Phase.Cooldown;
        _cooldownTimer = cooldownSeconds;

        // Don’t immediately nuke active refs; allow fade to finish smoothly
        ResetGesture(keepActiveHand: true);
    }

    private void ResetGesture(bool keepActiveHand = false)
    {
        _phase = (_phase == Phase.Showing || _phase == Phase.Cooldown) ? _phase : Phase.Idle;

        _armedTimer = 0f;
        _upTravelAccum = 0f;
        _holdTimer = 0f;

        if (!keepActiveHand)
        {
            _activeHand = null;
            _activeRefs = null;
        }
    }

    private void FadeTo(float target)
    {
        float dt = Time.deltaTime;
        float speed = target > _alpha
            ? (showFadeIn <= 1e-4f ? 999f : 1f / showFadeIn)
            : (hideFadeOut <= 1e-4f ? 999f : 1f / hideFadeOut);

        _alpha = Mathf.MoveTowards(_alpha, target, speed * dt);
        _panel.SetAlpha(_alpha);

        // Keep it out of the way when hidden
        if (_alpha <= 0.001f)
        {
            _panel.transform.position = head.position + head.forward * 10f; // park far away
        }
    }
    private void PlayUiSfx(AudioClip clip)
    {
        if (clip == null) return;

        // Prefer the panel (canvas) audio source
        if (uiSfxSourceOnPanel == null && _panel != null)
        {
            uiSfxSourceOnPanel = _panel.GetComponent<AudioSource>();
            if (uiSfxSourceOnPanel == null && autoFindSfxSourceInPanelChildren)
                uiSfxSourceOnPanel = _panel.GetComponentInChildren<AudioSource>(true);
        }

        if (uiSfxSourceOnPanel == null) return;

        uiSfxSourceOnPanel.PlayOneShot(clip, uiSfxVolume);
    }


    private void UpdatePanelPoseFollow()
    {
        if (_activeHand == null) return;

        // Target pose on palm/hand
        Transform palmT = _activeRefs != null && _activeRefs.palm ? _activeRefs.palm : _activeHand;

        Vector3 targetPos = palmT.TransformPoint(panelOffsetLocal);
        Quaternion targetRot;

        if (lockRotationToPalm)
        {
            // Base rotation: palm orientation + your prefab offset
            Vector3 palmNormal = palmT.up;

            // Direction from note to head, projected onto palm plane so the note stays "flat" on the hand.
            Vector3 toHead = (head.position - targetPos).normalized;
            Vector3 forwardOnPalm = Vector3.ProjectOnPlane(toHead, palmNormal);

            if (forwardOnPalm.sqrMagnitude < 1e-6f)
                forwardOnPalm = Vector3.ProjectOnPlane(palmT.forward, palmNormal); // fallback

            forwardOnPalm.Normalize();

            // This makes the canvas lie on the palm plane AND face the player.
            targetRot = Quaternion.LookRotation(forwardOnPalm, palmNormal)
                                 * Quaternion.Euler(panelRotationOffsetEuler);

            // Optional readability tilt around the palm's right axis
            /*if (Mathf.Abs(palmTiltDegrees) > 0.001f)
            {
                float tilt = palmTiltDegrees;

                if (autoTiltTowardHead)
                {
                    // Choose sign so it tilts toward the head (not away)
                    Vector3 toHead = (head.position - targetPos).normalized;
                    float dir = Mathf.Sign(Vector3.Dot(toHead, palmT.forward));
                    // If forward points "away" from head, flip tilt
                    tilt *= (dir >= 0f) ? -1f : 1f;
                }

                targetRot = Quaternion.AngleAxis(tilt, palmT.right) * targetRot;
            }*/
        }
        else
        {
            // Old behavior (billboard) if you ever want it
            targetRot = Quaternion.LookRotation((head.position - targetPos).normalized, GetUpAxis());
        }


        if (enablePullOutAnimation && _visibleTimer < pullOutDuration)
        {
            _animT = Mathf.Clamp01(_visibleTimer / Mathf.Max(pullOutDuration, 1e-4f));
            _panel.transform.position = Vector3.Lerp(_animFromPos, targetPos, _animT);
            _panel.transform.rotation = Quaternion.Slerp(_animFromRot, targetRot, _animT);
        }
        else
        {
            _panel.transform.position = targetPos;
            _panel.transform.rotation = Quaternion.Slerp(_panel.transform.rotation, targetRot, faceHeadSlerp * Time.deltaTime);
        }
    }

    private Vector3 GetUpAxis()
    {
        return upAxisMode == AxisMode.HeadUp ? head.up : Vector3.up;
    }

    private bool InChestZone(Vector3 handPos, Vector3 chestCenter, float radius, float extraDown)
    {
        // Forgiving sphere-ish zone that extends downward a bit:
        // Treat as distance to a vertical segment [center-downExtra, center] with radius.
        Vector3 a = chestCenter + Vector3.down * extraDown;
        Vector3 b = chestCenter;

        Vector3 ap = handPos - a;
        Vector3 ab = b - a;
        float t = Vector3.Dot(ap, ab) / Mathf.Max(Vector3.Dot(ab, ab), 1e-5f);
        t = Mathf.Clamp01(t);

        Vector3 closest = a + ab * t;
        return Vector3.Distance(handPos, closest) <= radius;
    }

    private bool IsPalmUp(HandRefs refs)
    {
        if (refs == null || (!refs.palm && !refs.hand)) return false;

        Transform t = refs.palm ? refs.palm : refs.hand;
        Vector3 n = GetLocalAxis(t, refs.palmNormalAxis);
        if (refs.invertPalmAxis) n = -n;

        float dot = Vector3.Dot(n.normalized, GetUpAxis().normalized);
        return dot >= palmUpDotThreshold;
    }

    private Vector3 GetLocalAxis(Transform t, LocalAxis axis)
    {
        switch (axis)
        {
            case LocalAxis.Forward: return t.forward;
            case LocalAxis.Right: return t.right;
            default: return t.up;
        }
    }
}
