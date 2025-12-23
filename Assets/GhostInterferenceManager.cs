using Fusion;
using UnityEngine;

public class GhostInterferenceManager : NetworkBehaviour
{
    [Header("Targeting")]
    public Transform playerHead;          // VR: CenterEyeAnchor
    public float maxTargetDistance = 5f;

    [Header("Debug")]
    [SerializeField] private bool verboseLogs = true;

    // Call this from ANY UI Button on ANY device.
    public void TriggerGhost()
    {
        var hub = FusionRpcHub.Instance;
        if (hub == null)
        {
            Debug.LogWarning("[GhostInterferenceManager] FusionRpcHub.Instance not found. Add a FusionRpcHub on a spawned NetworkObject.");
            return;
        }

        if (hub.Object == null)
        {
            Debug.LogWarning("[GhostInterferenceManager] FusionRpcHub exists but is not spawned (Object == null). Put FusionRpcHub on a spawned NetworkObject.");
            return;
        }

        if (verboseLogs)
            Debug.Log($"[GhostInterferenceManager] TriggerGhost pressed by LocalPlayer={Runner?.LocalPlayer} -> forwarding to FusionRpcHub");

        hub.BroadcastGhostTrigger();
    }

    /// <summary>
    /// Called by <see cref="FusionRpcHub"/> when the Ghost trigger RPC arrives.
    /// Keeps your existing targeting logic here; only the RPC transport moved to the hub.
    /// </summary>
    public void HandleGhostTriggerRpc(RpcInfo info = default)
    {
        if (playerHead == null)
        {
            if (verboseLogs)
                Debug.LogWarning($"[GhostInterferenceManager] HandleGhostTriggerRpc: playerHead not assigned on this client (LocalPlayer={Runner?.LocalPlayer}).");
            return;
        }

        //var all = FindObjectsOfType<GhostGrabInterference>(false);
        var all = FindObjectsOfType<LocalGhostGrabInterference>(false);

        LocalGhostGrabInterference chosen = null;
        float bestDist = float.MaxValue;

        // IMPORTANT: pick the grabbed one, not the nearest one.
        foreach (var g in all)
        {
            if (g == null) continue;
            if (!g.IsGrabbedNow) continue;

            float d = Vector3.Distance(g.transform.position, playerHead.position);
            if (d > maxTargetDistance) continue;

            if (d < bestDist)
            {
                bestDist = d;
                chosen = g;
            }
        }

        if (chosen == null)
        {
            if (verboseLogs)
                Debug.Log($"[GhostInterferenceManager] HandleGhostTriggerRpc: no grabbed target found on this client. candidates={all.Length}, player={Runner?.LocalPlayer}, source={info.Source}");
            return;
        }

        //if (verboseLogs)
          //  Debug.Log($"[GhostInterferenceManager] HandleGhostTriggerRpc: chosen={chosen.name}, dist={bestDist:F2}, grabbed={chosen.IsGrabbedNow}, stateAuth={chosen.Object?.StateAuthority}, source={info.Source}");

        // This already RPCs to the StateAuthority inside GhostGrabInterference.
        chosen.RegisterGhostHit(playerHead.position, playerHead.forward);
    }
}
