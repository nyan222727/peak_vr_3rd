using UnityEngine;
using UnityEngine.XR;

public class HandLock : MonoBehaviour
{
    
    public enum HandSide { Left, Right }
    [SerializeField] private bool debugLogs = true;

    [Header("Haptics (XR)")]
    [SerializeField] private bool enableHaptics = true;

    // continuous haptics state
    private bool _leftHapticsActive, _rightHapticsActive;
    private float _leftAmp, _rightAmp;
    private float _leftPulseInterval = 0.06f, _rightPulseInterval = 0.06f;
    private float _leftPulseDuration = 0.03f, _rightPulseDuration = 0.03f;
    private float _nextLeftPulseTime, _nextRightPulseTime;


    [Header("These are the transforms your IK uses as hand targets (trackers)")]
    [SerializeField] private Transform leftHandTracker;
    [SerializeField] private Transform rightHandTracker;

    private bool _leftLocked, _rightLocked;
    private Transform _leftTarget, _rightTarget;

    // offsets stored in target local space (used if you DON'T snap)
    private Vector3 _leftLocalPosOffset, _rightLocalPosOffset;
    private Quaternion _leftLocalRotOffset, _rightLocalRotOffset;
    private VRTrackerDriver _trackerDriver;

    private void Awake()
    {
        _trackerDriver = GetComponentInParent<VRTrackerDriver>();
    }

    void LateUpdate()
    {
        // LateUpdate so we overwrite any tracking scripts that ran earlier this frame
        if (_leftLocked && leftHandTracker && _leftTarget)
        {
            leftHandTracker.position = _leftTarget.TransformPoint(_leftLocalPosOffset);
            leftHandTracker.rotation = _leftTarget.rotation * _leftLocalRotOffset;
            TickHaptics(Time.unscaledTime);
        }

        if (_rightLocked && rightHandTracker && _rightTarget)
        {
            rightHandTracker.position = _rightTarget.TransformPoint(_rightLocalPosOffset);
            rightHandTracker.rotation = _rightTarget.rotation * _rightLocalRotOffset;
            TickHaptics(Time.unscaledTime);
        }

        
    }

    /// <param name="snapToTargetPose">
    /// true = snap hand to exact target pose (nice pose via HandLockPoint rotation),
    /// false = preserve current hand offset relative to target.
    /// </param>
    public void LockHand(HandSide side, Transform stickTo, bool snapToTargetPose = true)
    {
        if (stickTo == null) return;
        if (debugLogs)
            Debug.Log($"[HandLock] LockHand side={side} stickTo='{stickTo.name}' snap={snapToTargetPose}", this);


        if (side == HandSide.Left)
        {
            if (side == HandSide.Left && !leftHandTracker && debugLogs)
                Debug.Log("[HandLock] leftHandTracker is NULL -> cannot lock", this);
            if (!leftHandTracker) return;

            _leftTarget = stickTo;

            if (_trackerDriver)
            {
                _trackerDriver.overrideLeftHand = true;
                _trackerDriver.leftHandOverrideTarget = stickTo;
            }

            if (snapToTargetPose)
            {
                // exact pose of lockpoint
                _leftLocalPosOffset = Vector3.zero;
                _leftLocalRotOffset = Quaternion.identity;

                // snap immediately (optional, LateUpdate will enforce anyway)
                leftHandTracker.position = stickTo.position;
                leftHandTracker.rotation = stickTo.rotation;
            }
            else
            {
                // keep current offset
                _leftLocalPosOffset = stickTo.InverseTransformPoint(leftHandTracker.position);
                _leftLocalRotOffset = Quaternion.Inverse(stickTo.rotation) * leftHandTracker.rotation;
            }

            _leftLocked = true;
        }
        else
        {
            if (!rightHandTracker) return;
            if (side == HandSide.Right && !rightHandTracker && debugLogs)
                Debug.Log("[HandLock] rightHandTracker is NULL -> cannot lock", this);

            _rightTarget = stickTo;

            if (_trackerDriver)
            {
                _trackerDriver.overrideRightHand = true;
                _trackerDriver.rightHandOverrideTarget = stickTo;
            }

            if (snapToTargetPose)
            {
                _rightLocalPosOffset = Vector3.zero;
                _rightLocalRotOffset = Quaternion.identity;

                rightHandTracker.position = stickTo.position;
                rightHandTracker.rotation = stickTo.rotation;
            }
            else
            {
                _rightLocalPosOffset = stickTo.InverseTransformPoint(rightHandTracker.position);
                _rightLocalRotOffset = Quaternion.Inverse(stickTo.rotation) * rightHandTracker.rotation;
            }

            _rightLocked = true;
        }
    }

    public void UnlockHand(HandSide side)
    {
        if (debugLogs)
            Debug.Log($"[HandLock] UnlockHand side={side}", this);
        if (side == HandSide.Left)
        {
            if (_trackerDriver)
            {
                _trackerDriver.overrideLeftHand = false;
                _trackerDriver.leftHandOverrideTarget = null;
            }
            _leftLocked = false;
            _leftTarget = null;
        }
        else
        {
            if (_trackerDriver)
            {
                _trackerDriver.overrideRightHand = false;
                _trackerDriver.rightHandOverrideTarget = null;
            }
            _rightLocked = false;
            _rightTarget = null;
        }
    }

    public void UnlockAll()
    {
        UnlockHand(HandSide.Left);
        UnlockHand(HandSide.Right);
    }

    public void HapticPulse(HandSide side, float amplitude, float duration)
{
    if (!enableHaptics) return;
    amplitude = Mathf.Clamp01(amplitude);
    duration = Mathf.Max(0f, duration);

    var dev = GetDevice(side);
    if (!dev.isValid) return;

    if (dev.TryGetHapticCapabilities(out var caps) && caps.supportsImpulse)
    {
        dev.SendHapticImpulse(0u, amplitude, duration);
    }
}

public void SetContinuousHaptics(HandSide side, float amplitude, float pulseInterval, float pulseDuration)
{
    if (!enableHaptics) return;
    amplitude = Mathf.Clamp01(amplitude);
    pulseInterval = Mathf.Max(0.01f, pulseInterval);
    pulseDuration = Mathf.Max(0.005f, pulseDuration);

    if (side == HandSide.Left)
    {
        _leftHapticsActive = amplitude > 0f;
        _leftAmp = amplitude;
        _leftPulseInterval = pulseInterval;
        _leftPulseDuration = pulseDuration;
    }
    else
    {
        _rightHapticsActive = amplitude > 0f;
        _rightAmp = amplitude;
        _rightPulseInterval = pulseInterval;
        _rightPulseDuration = pulseDuration;
    }
}

public void StopContinuousHaptics(HandSide side)
{
    if (side == HandSide.Left) _leftHapticsActive = false;
    else _rightHapticsActive = false;
}

private InputDevice GetDevice(HandSide side)
{
    return InputDevices.GetDeviceAtXRNode(side == HandSide.Left ? XRNode.LeftHand : XRNode.RightHand);
}

private void TickHaptics(float now)
{
    if (!enableHaptics) return;

    if (_leftHapticsActive && now >= _nextLeftPulseTime)
    {
        HapticPulse(HandSide.Left, _leftAmp, _leftPulseDuration);
        _nextLeftPulseTime = now + _leftPulseInterval;
    }

    if (_rightHapticsActive && now >= _nextRightPulseTime)
    {
        HapticPulse(HandSide.Right, _rightAmp, _rightPulseDuration);
        _nextRightPulseTime = now + _rightPulseInterval;
    }
}

}
