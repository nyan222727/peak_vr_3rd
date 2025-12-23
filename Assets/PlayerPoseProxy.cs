using Fusion;
using UnityEngine;

/// <summary>
/// Networked pose proxy: stores pose in [Networked] fields and drives child transforms locally.
/// IMPORTANT: Remove NetworkTransform components from child nodes to avoid conflicts.
/// </summary>
public class PlayerPoseProxy : NetworkBehaviour
{
    [Header("Proxy Nodes (children under this proxy)")]
    public Transform rootNode;
    public Transform headNode;
    public Transform leftHandNode;
    public Transform rightHandNode;
    public Transform leftFootNode;   // optional
    public Transform rightFootNode;  // optional

    [Header("Debug")]
    public bool log = false;

    // Networked pose fields (Fusion supports Vector3 + Quaternion as Networked). :contentReference[oaicite:1]{index=1}
    [Networked] private Vector3 RootPos { get; set; }
    [Networked] private Quaternion RootRot { get; set; }

    [Networked] private Vector3 HeadPos { get; set; }
    [Networked] private Quaternion HeadRot { get; set; }

    [Networked] private Vector3 LHandPos { get; set; }
    [Networked] private Quaternion LHandRot { get; set; }

    [Networked] private Vector3 RHandPos { get; set; }
    [Networked] private Quaternion RHandRot { get; set; }

    [Networked] private NetworkBool HasFeet { get; set; }
    [Networked] private Vector3 LFootPos { get; set; }
    [Networked] private Quaternion LFootRot { get; set; }
    [Networked] private Vector3 RFootPos { get; set; }
    [Networked] private Quaternion RFootRot { get; set; }

    /// <summary>
    /// Called by local (non-networked) sender.
    /// Host applies directly; clients RPC to host.
    /// </summary>
    public void SubmitPoseFromLocal(
        Vector3 rootPos, Quaternion rootRot,
        Vector3 headPos, Quaternion headRot,
        Vector3 lhPos, Quaternion lhRot,
        Vector3 rhPos, Quaternion rhRot,
        bool hasFeet,
        Vector3 lfPos, Quaternion lfRot,
        Vector3 rfPos, Quaternion rfRot)
    {
        if (!Object) return;

        if (Object.HasStateAuthority)
        {
            SetNetworkedPose(
                rootPos, rootRot,
                headPos, headRot,
                lhPos, lhRot,
                rhPos, rhRot,
                hasFeet,
                lfPos, lfRot,
                rfPos, rfRot);
            return;
        }

        // Scene proxy usually has StateAuthority on host; clients won't have InputAuthority on it,
        // so allow all sources.
        RPC_SubmitPoseToHost(
            rootPos, rootRot,
            headPos, headRot,
            lhPos, lhRot,
            rhPos, rhRot,
            hasFeet,
            lfPos, lfRot,
            rfPos, rfRot);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_SubmitPoseToHost(
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

        if (log) Debug.Log($"[PlayerPoseProxyNet] Pose RPC from {info.Source}", this);

        SetNetworkedPose(
            rootPos, rootRot,
            headPos, headRot,
            lhPos, lhRot,
            rhPos, rhRot,
            hasFeet,
            lfPos, lfRot,
            rfPos, rfRot);
    }

    private void SetNetworkedPose(
        Vector3 rootPos, Quaternion rootRot,
        Vector3 headPos, Quaternion headRot,
        Vector3 lhPos, Quaternion lhRot,
        Vector3 rhPos, Quaternion rhRot,
        bool hasFeet,
        Vector3 lfPos, Quaternion lfRot,
        Vector3 rfPos, Quaternion rfRot)
    {
        // Write network state (StateAuthority only)
        RootPos = rootPos; RootRot = rootRot;
        HeadPos = headPos; HeadRot = headRot;
        LHandPos = lhPos;  LHandRot = lhRot;
        RHandPos = rhPos;  RHandRot = rhRot;

        HasFeet = hasFeet;
        if (hasFeet)
        {
            LFootPos = lfPos; LFootRot = lfRot;
            RFootPos = rfPos; RFootRot = rfRot;
        }
    }

    // Render runs every frame and applies the (interpolated) networked state to transforms.
    public override void Render()
    {
        ApplyToTransforms();
    }

    private void ApplyToTransforms()
    {
        if (rootNode) rootNode.SetPositionAndRotation(RootPos, RootRot);
        if (headNode) headNode.SetPositionAndRotation(HeadPos, HeadRot);
        if (leftHandNode) leftHandNode.SetPositionAndRotation(LHandPos, LHandRot);
        if (rightHandNode) rightHandNode.SetPositionAndRotation(RHandPos, RHandRot);

        if (HasFeet)
        {
            if (leftFootNode) leftFootNode.SetPositionAndRotation(LFootPos, LFootRot);
            if (rightFootNode) rightFootNode.SetPositionAndRotation(RFootPos, RFootRot);
        }
    }
}
