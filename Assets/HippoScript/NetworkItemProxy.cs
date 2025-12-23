using Fusion;
using UnityEngine;

public class NetworkItemProxy : NetworkBehaviour
{
    [Networked] public NetworkString<_32> ItemId { get; set; }
    [Networked] public NetworkString<_32> VisualKey { get; set; }

    // XR端用：對應到本地真實XR物件
    public Transform xrSource;

    // XR端用：是否由XRSource驅動Proxy（未被拿走/或抓取中也可）
    public bool followXRSource = true;

    public override void FixedUpdateNetwork()
    {
        // 只有有權限的一端才寫入transform（避免抖動/互相搶）
        // 這裡採「有 InputAuthority 或 StateAuthority 的人」更新Proxy位置
        if (followXRSource && xrSource != null && (Object.HasInputAuthority || Object.HasStateAuthority))
        {
            transform.SetPositionAndRotation(xrSource.position, xrSource.rotation);
        }
    }

    // 給XR抓取事件呼叫：要求拿取權
    public void RequestGrab(PlayerRef grabber)
    {
        if (Object.HasStateAuthority)
        {
            // 由Host直接轉權
            Object.AssignInputAuthority(grabber);
        }
        else
        {
            // 非Host端，丟RPC給Host
            RPC_RequestGrab(grabber);
        }
    }

    public void ReleaseGrab()
    {
        if (Object.HasStateAuthority)
        {
            Object.RemoveInputAuthority();
        }
        else
        {
            RPC_ReleaseGrab();
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestGrab(PlayerRef grabber)
    {
        Object.AssignInputAuthority(grabber);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_ReleaseGrab()
    {
        Object.RemoveInputAuthority();
    }
}
