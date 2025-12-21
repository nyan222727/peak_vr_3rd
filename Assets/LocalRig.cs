using UnityEngine;

public class LocalRig : MonoBehaviour
{
    [Header("Source (from Camera Rig)")]
    public Transform headSource;      // e.g., CenterEyeAnchor
    public Transform leftHandSource;  // e.g., LeftHandAnchor
    public Transform rightHandSource; // e.g., RightHandAnchor

    [Header("Targets (your existing trackers used by IK)")]
    public Transform headTracker;     
    public Transform leftHandTracker;
    public Transform rightHandTracker;

    [Header("Optional offsets (if your IK expects different alignment)")]
    public Vector3 headPosOffset;
    public Vector3 leftHandPosOffset;
    public Vector3 rightHandPosOffset;

    public Vector3 headRotOffsetEuler;
    public Vector3 leftHandRotOffsetEuler;
    public Vector3 rightHandRotOffsetEuler;

    void LateUpdate()
    {
        Apply(headSource, headTracker, headPosOffset, headRotOffsetEuler);
        Apply(leftHandSource, leftHandTracker, leftHandPosOffset, leftHandRotOffsetEuler);
        Apply(rightHandSource, rightHandTracker, rightHandPosOffset, rightHandRotOffsetEuler);
    }

    private static void Apply(Transform src, Transform dst, Vector3 posOffset, Vector3 rotOffsetEuler)
    {
        if (!src || !dst) return;

        dst.position = src.TransformPoint(posOffset);
        dst.rotation = src.rotation * Quaternion.Euler(rotOffsetEuler);
    }
}
