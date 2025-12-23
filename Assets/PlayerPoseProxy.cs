using System;
using Fusion;
using UnityEngine;

/// <summary>
/// Lightweight transform proxy for mobile/remote clients.
/// Put this on a prefab with a NetworkObject + children (Root/Head/LeftHand/RightHand/[Feet]).
/// Add NetworkTransform components to each child Transform you want replicated.
/// The StateAuthority (host) sets the transforms based on data received from the owning player.
/// </summary>
public class PlayerPoseProxy : NetworkBehaviour
{
    [Header("Proxy Nodes (children)")]
    public Transform rootNode;
    public Transform headNode;
    public Transform leftHandNode;
    public Transform rightHandNode;
    public Transform leftFootNode;   // optional
    public Transform rightFootNode;  // optional

    public bool HasFeetNodes => leftFootNode != null && rightFootNode != null;

    public static event Action<PlayerPoseProxy> OnAnyProxySpawned;

    public override void Spawned()
    {
        OnAnyProxySpawned?.Invoke(this);
    }

    /// <summary>
    /// Apply the proxy pose on the StateAuthority (host).
    /// </summary>
    public void ApplyPose(
            Vector3 rootPos, Quaternion rootRot,
            Vector3 headPos, Quaternion headRot,
            Vector3 lhPos, Quaternion lhRot,
            Vector3 rhPos, Quaternion rhRot,
            bool hasFeet,
            Vector3 lfPos, Quaternion lfRot,
            Vector3 rfPos, Quaternion rfRot)
    {
        // Safety: allow calling only on state authority (the only side that replicates NetworkTransform state). citeturn1search2
        if (!Object || !Object.HasStateAuthority)
            return;

        if (rootNode) { rootNode.SetPositionAndRotation(rootPos, rootRot); }
        if (headNode) { headNode.SetPositionAndRotation(headPos, headRot); }
        if (leftHandNode) { leftHandNode.SetPositionAndRotation(lhPos, lhRot); }
        if (rightHandNode) { rightHandNode.SetPositionAndRotation(rhPos, rhRot); }

        if (hasFeet && HasFeetNodes)
        {
            leftFootNode.SetPositionAndRotation(lfPos, lfRot);
            rightFootNode.SetPositionAndRotation(rfPos, rfRot);
        }
    }
}
