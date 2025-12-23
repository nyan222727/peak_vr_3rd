using Fusion;
using UnityEngine;

public class NetworkItemProxy_ServerAuth : NetworkBehaviour
{
    [Networked] public NetworkString<_32> ItemId { get; set; }
    [Networked] public NetworkString<_32> VisualKey { get; set; }

    // 目前誰在抓（None = 沒人抓）
    [Networked] public PlayerRef Holder { get; set; }
    [Networked] public NetworkBool IsGrabbed { get; set; }

    // Host 端暫存：最新目標姿態
    private Vector3 _targetPos;
    private Quaternion _targetRot;
    private bool _hasTarget;

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            Holder = PlayerRef.None;
            IsGrabbed = false;
        }
    }

    public override void FixedUpdateNetwork()
    {
        // 永遠只有 Host(StateAuthority) 寫 transform
        if (!Object.HasStateAuthority) return;

        if (_hasTarget)
        {
            transform.SetPositionAndRotation(_targetPos, _targetRot);
        }
    }

    // VR端：請求抓取
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestGrab(PlayerRef requester)
    {
        if (!Object.HasStateAuthority) return;

        // 你可以在這裡加規則：已被抓就拒絕，或允許搶奪
        if (IsGrabbed) return;

        Holder = requester;
        IsGrabbed = true;
        _hasTarget = false;
    }

    // VR端：放開
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_ReleaseGrab(PlayerRef requester)
    {
        if (!Object.HasStateAuthority) return;

        if (!IsGrabbed) return;
        if (Holder != requester) return;

        Holder = PlayerRef.None;
        IsGrabbed = false;
        _hasTarget = false;
    }

    // VR端：送 Pose（抓取中才會送）
    // 建議用 FixedUpdate 節流，例如 15~30Hz
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SendPose(PlayerRef sender, Vector3 pos, Quaternion rot)
    {
        if (!Object.HasStateAuthority) return;

        // 驗證：只有持有者能更新
        if (!IsGrabbed) return;
        if (Holder != sender) return;

        _targetPos = pos;
        _targetRot = rot;
        _hasTarget = true;
    }
}
