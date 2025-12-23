using UnityEngine;
using Fusion;
using UnityEngine.XR;   // for XRSettings

public class JumpScareButton : NetworkBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool log = true;

    // Called by your UI button / interactable event
    public void TriggerJumpScare()
    {
        if (log) Debug.Log($"[JumpScareButton] Local press by {Runner?.LocalPlayer} (HasStateAuth={Object?.HasStateAuthority})");

        var hub = FusionRpcHub.Instance;
        if (hub == null || hub.Object == null)
        {
            Debug.LogWarning("[JumpScareButton] No FusionRpcHub in scene (or it is not spawned). Put FusionRpcHub on a spawned NetworkObject.");
            return;
        }

        // Broadcast to everyone; each client decides whether to actually trigger
        hub.BroadcastJumpScare();
    }

    /// <summary>
    /// Called by <see cref="FusionRpcHub"/> when the JumpScare RPC arrives.
    /// Keeps your existing per-client behavior here; only the RPC transport moved to the hub.
    /// </summary>
    public void HandleJumpScareRpc(RpcInfo info = default)
    {
        bool isVrClient = XRSettings.isDeviceActive; // true on VR headset, false on phone

        if (log) Debug.Log($"[JumpScareButton] HandleJumpScareRpc received on client. source={info.Source} isVrClient={isVrClient}");

        if (!isVrClient)
            return; // mobile side receives RPC but does nothing

        if (JumpScareEffect.Local != null)
        {
            if (log) Debug.Log("[JumpScareButton] Triggering JumpScareEffect.Local on VR client.");
            JumpScareEffect.Local.Trigger();
        }
        else
        {
            Debug.LogWarning("[JumpScareButton] JumpScareEffect.Local is null (VR local player not spawned yet?).");
        }
    }
}
