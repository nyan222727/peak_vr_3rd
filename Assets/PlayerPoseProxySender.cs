using Fusion;
using UnityEngine;

/// <summary>
/// Attach this to the NETWORKED player object (spawned per player).
/// Uses a SINGLE scene PlayerPoseProxy (already in the scene, only one) and streams poses to it.
/// - InputAuthority: reads local tracker transforms and sends to host via RPC.
/// - StateAuthority (host): applies to the scene proxy (replicated via NetworkTransform on proxy children).
/// </summary>
public class PlayerPoseProxySender : NetworkBehaviour
{
    [Header("Scene Proxy (single instance)")]
    [Tooltip("Optional: assign a name to find the proxy GameObject in the scene. Leave empty to find by type.")]
    public string sceneProxyName = "";

    [Tooltip("If true, will search inactive objects too.")]
    public bool includeInactiveInFind = true;

    [Header("Pose Sources (assign from your PlayerPrefab)")]
    [Tooltip("Usually the moving root of your rig / player (e.g., CameraRig root).")]
    public Transform rootSource;

    [Tooltip("Head tracker transform (final pose).")]
    public Transform headSource;

    [Tooltip("Left hand tracker transform (final pose).")]
    public Transform leftHandSource;

    [Tooltip("Right hand tracker transform (final pose).")]
    public Transform rightHandSource;

    [Header("Feet (optional)")]
    public bool syncFeet = false;

    [Tooltip("Humanoid Animator root (TempAvatar). Used only if syncFeet=true.")]
    public Animator humanoidAnimator;

    [Header("Send Rate")]
    [Tooltip("How many pose updates per second are sent from owner to host.")]
    [Range(5, 90)] public int sendRateHz = 30;

    private PlayerPoseProxy _proxy;
    private float _nextSendTime;
    private bool _warnedMissingProxy;

    public override void Spawned()
    {
        if (!rootSource) rootSource = transform;

        ResolveSceneProxy();
    }

    private void ResolveSceneProxy()
    {
        if (_proxy != null) return;

        // 1) If name provided, try find by name first (cheapest + deterministic)
        if (!string.IsNullOrWhiteSpace(sceneProxyName))
        {
            var go = GameObject.Find(sceneProxyName);
            if (go != null) _proxy = go.GetComponent<PlayerPoseProxy>();
        }

        // 2) Fallback: find by type
        if (_proxy == null)
        {
            // FindObjectOfType doesn't include inactive objects; use Resources when needed.
            if (!includeInactiveInFind)
            {
               _proxy = UnityEngine.Object.FindFirstObjectByType<PlayerPoseProxy>();
            }
            else
            {
                var all = Resources.FindObjectsOfTypeAll<PlayerPoseProxy>();
                // pick the first one that is in a loaded scene
                foreach (var p in all)
                {
                    if (p != null && p.gameObject.scene.IsValid())
                    {
                        _proxy = p;
                        break;
                    }
                }
            }
        }

        if (_proxy == null && !_warnedMissingProxy)
        {
            _warnedMissingProxy = true;
            Debug.LogWarning($"[{nameof(PlayerPoseProxySender)}] Could not find scene {nameof(PlayerPoseProxy)}. " +
                             $"Make sure there is exactly one in the scene and it has a NetworkObject + child NetworkTransforms.");
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object || !Object.HasInputAuthority)
            return;

        if (sendRateHz <= 0) return;

        // Rate limit
        if (Time.time < _nextSendTime) return;
        _nextSendTime = Time.time + (1f / sendRateHz);

        // Sources must exist
        if (!rootSource || !headSource || !leftHandSource || !rightHandSource)
            return;

        // Ensure proxy exists (in case Spawned order differs)
        if (_proxy == null) ResolveSceneProxy();
        if (_proxy == null) return;

        // Feet (optional)
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

        // If we are also host, apply directly (no RPC)
        if (Object.HasStateAuthority)
        {
            _proxy.ApplyPose(
                rootSource.position, rootSource.rotation,
                headSource.position, headSource.rotation,
                leftHandSource.position, leftHandSource.rotation,
                rightHandSource.position, rightHandSource.rotation,
                hasFeet,
                lfPos, lfRot,
                rfPos, rfRot
            );
            return;
        }

        // Client -> Host
        RPC_SendPoseToHost(
            rootSource.position, rootSource.rotation,
            headSource.position, headSource.rotation,
            leftHandSource.position, leftHandSource.rotation,
            rightHandSource.position, rightHandSource.rotation,
            hasFeet,
            lfPos, lfRot,
            rfPos, rfRot
        );
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SendPoseToHost(
        Vector3 rootPos, Quaternion rootRot,
        Vector3 headPos, Quaternion headRot,
        Vector3 lhPos, Quaternion lhRot,
        Vector3 rhPos, Quaternion rhRot,
        bool hasFeet,
        Vector3 lfPos, Quaternion lfRot,
        Vector3 rfPos, Quaternion rfRot,
        RpcInfo info = default)
    {
        if (!Object.HasStateAuthority) return;

        if (_proxy == null) ResolveSceneProxy();
        if (_proxy == null) return;

        _proxy.ApplyPose(
            rootPos, rootRot,
            headPos, headRot,
            lhPos, lhRot,
            rhPos, rhRot,
            hasFeet,
            lfPos, lfRot,
            rfPos, rfRot
        );
    }
}
