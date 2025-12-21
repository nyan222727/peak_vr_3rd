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
        if (Object == null)
        {
            Debug.LogWarning("[GhostInterferenceManager] Not spawned / no NetworkObject. Put this on a spawned NetworkObject.");
            return;
        }

        if (verboseLogs)
            Debug.Log($"[GhostInterferenceManager] TriggerGhost pressed by LocalPlayer={Runner?.LocalPlayer} -> broadcasting RPC");

        RPC_BroadcastGhostTrigger();
    }

    // Key idea: broadcast to ALL. Only the VR client will find a grabbed object locally.
    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_BroadcastGhostTrigger()
    {
        if (playerHead == null)
        {
            if (verboseLogs)
                Debug.LogWarning($"[GhostInterferenceManager] RPC_BroadcastGhostTrigger: playerHead not assigned on this client (LocalPlayer={Runner?.LocalPlayer}).");
            return;
        }

        var all = FindObjectsOfType<GhostGrabInterference>(false);

        GhostGrabInterference chosen = null;
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
                Debug.Log($"[GhostInterferenceManager] RPC_BroadcastGhostTrigger: no grabbed target found on this client. candidates={all.Length}, player={Runner?.LocalPlayer}");
            return;
        }

        if (verboseLogs)
            Debug.Log($"[GhostInterferenceManager] RPC_BroadcastGhostTrigger: chosen={chosen.name}, dist={bestDist:F2}, grabbed={chosen.IsGrabbedNow}, stateAuth={chosen.Object?.StateAuthority}");

        // This already RPCs to the StateAuthority inside GhostGrabInterference.
        chosen.RegisterGhostHit(playerHead.position, playerHead.forward);
    }
}
