using UnityEngine;
using Fusion;

/// <summary>
/// Local-only sampler. Not networked.
/// Reads tracker transforms and submits to PlayerPoseProxyNet (which handles networking).
/// </summary>
public class PlayerPoseProxyLocalSender : MonoBehaviour
{
    [Header("Scene Proxy (networked)")]
    public PlayerPoseProxy proxy;        // assign if you want
    public string proxyObjectName = "";     // optional deterministic find

    [Header("Optional authority gate")]
    [Tooltip("If assigned, only streams when this NetworkObject has InputAuthority (prevents non-owner sending).")]
    public NetworkObject inputAuthorityGate;

    [Header("Pose Sources")]
    public Transform rootSource;
    public Transform headSource;
    public Transform leftHandSource;
    public Transform rightHandSource;

    [Header("Feet (optional)")]
    public bool syncFeet = false;
    public Animator humanoidAnimator;

    [Header("Send Rate")]
    [Range(5, 90)] public int sendRateHz = 30;

    private float _nextSendTime;
    private bool _warnedMissingProxy;

    private void Awake()
    {
        ResolveProxy();
    }

    private void Update()
    {
        if (sendRateHz <= 0) return;

        if (inputAuthorityGate != null && !inputAuthorityGate.HasInputAuthority)
            return;

        if (Time.time < _nextSendTime) return;
        _nextSendTime = Time.time + (1f / sendRateHz);

        if (!rootSource || !headSource || !leftHandSource || !rightHandSource)
            return;

        if (proxy == null) ResolveProxy();
        if (proxy == null) return;

        bool hasFeet = false;
        Vector3 lfPos = default; Quaternion lfRot = default;
        Vector3 rfPos = default; Quaternion rfRot = default;

        if (syncFeet && humanoidAnimator != null && humanoidAnimator.isHuman)
        {
            var lf = humanoidAnimator.GetBoneTransform(HumanBodyBones.LeftFoot);
            var rf = humanoidAnimator.GetBoneTransform(HumanBodyBones.RightFoot);
            if (lf != null && rf != null)
            {
                hasFeet = true;
                lfPos = lf.position; lfRot = lf.rotation;
                rfPos = rf.position; rfRot = rf.rotation;
            }
        }

        proxy.SubmitPoseFromLocal(
            rootSource.position, rootSource.rotation,
            headSource.position, headSource.rotation,
            leftHandSource.position, leftHandSource.rotation,
            rightHandSource.position, rightHandSource.rotation,
            hasFeet,
            lfPos, lfRot,
            rfPos, rfRot
        );
    }

    private void ResolveProxy()
    {
        if (proxy != null) return;

        if (!string.IsNullOrWhiteSpace(proxyObjectName))
        {
            var go = GameObject.Find(proxyObjectName);
            if (go != null) proxy = go.GetComponent<PlayerPoseProxy>();
        }

        if (proxy == null)
            proxy = FindObjectOfType<PlayerPoseProxy>();

        if (proxy == null && !_warnedMissingProxy)
        {
            _warnedMissingProxy = true;
            Debug.LogWarning("[PlayerPoseProxyLocalSender] Could not find PlayerPoseProxyNet in scene.");
        }
    }
}
