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

        // Broadcast to everyone; each client decides whether to actually trigger
        RPC_TriggerJumpScare();
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_TriggerJumpScare(RpcInfo info = default)
    {
        bool isVrClient = XRSettings.isDeviceActive; // true on VR headset, false on phone

        if (log) Debug.Log($"[JumpScareButton] RPC received on client. source={info.Source} isVrClient={isVrClient}");

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
