using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

/// <summary>
/// Centralizes (collapses) Fusion RPC transport in one place.
/// Your gameplay scripts keep their public entry points (e.g. TriggerGhost/TriggerJumpScare),
/// but they forward to this hub instead of containing [Rpc] methods themselves.
/// </summary>
public class FusionRpcHub : NetworkBehaviour
{
    public static FusionRpcHub Instance { get; private set; }

    [Header("Debug")]
    [SerializeField] private bool log = true;

    /// <summary>zoneId -> locked?</summary>
    private readonly Dictionary<string, bool> _handLockStates = new();

    /// <summary>
    /// Fired when a HandLockZone reports a lock/unlock state over RPC.
    /// (Useful for phone-side UI, logging, etc.)
    /// </summary>
    public event Action<string, bool, PlayerRef> HandLockStateChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[FusionRpcHub] Multiple instances detected. Keeping the first one.", this);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // -----------------------------
    // Public API (called by others)
    // -----------------------------

    public void BroadcastGhostTrigger()
    {
        if (Object == null)
        {
            Debug.LogWarning("[FusionRpcHub] BroadcastGhostTrigger called but this hub is not spawned (Object == null).", this);
            return;
        }

        if (log)
            Debug.Log($"[FusionRpcHub] BroadcastGhostTrigger by LocalPlayer={Runner?.LocalPlayer}", this);

        RPC_GhostTrigger();
    }

    public void BroadcastJumpScare()
    {
        if (Object == null)
        {
            Debug.LogWarning("[FusionRpcHub] BroadcastJumpScare called but this hub is not spawned (Object == null).", this);
            return;
        }

        if (log)
            Debug.Log($"[FusionRpcHub] BroadcastJumpScare by LocalPlayer={Runner?.LocalPlayer}", this);

        RPC_JumpScare();
    }

    public void ReportHandLockState(string zoneId, bool locked)
    {
        if (string.IsNullOrWhiteSpace(zoneId))
            zoneId = gameObject.name;

        if (Object == null)
        {
            Debug.LogWarning($"[FusionRpcHub] ReportHandLockState called but hub is not spawned (Object == null). zoneId={zoneId}", this);
            return;
        }

        var id = new NetworkString<_64>(zoneId);

        if (log)
            Debug.Log($"[FusionRpcHub] ReportHandLockState zoneId='{zoneId}' locked={locked} by LocalPlayer={Runner?.LocalPlayer}", this);

        RPC_HandLockState(id, locked);
    }

    public bool TryGetHandLockState(string zoneId, out bool locked)
        => _handLockStates.TryGetValue(zoneId, out locked);

    // -----------------------------
    // RPCs (transport only)
    // -----------------------------

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_GhostTrigger(RpcInfo info = default)
    {
        if (log)
            Debug.Log($"[FusionRpcHub] RPC_GhostTrigger received. source={info.Source} local={Runner?.LocalPlayer}", this);

        // Dispatch to any manager(s) in scene.
        var managers = FindObjectsOfType<GhostInterferenceManager>(false);
        foreach (var m in managers)
        {
            if (m == null) continue;
            m.HandleGhostTriggerRpc(info);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_JumpScare(RpcInfo info = default)
    {
        if (log)
            Debug.Log($"[FusionRpcHub] RPC_JumpScare received. source={info.Source} local={Runner?.LocalPlayer}", this);

        // Dispatch to any button(s) in scene.
        var buttons = FindObjectsOfType<JumpScareButton>(false);
        foreach (var b in buttons)
        {
            if (b == null) continue;
            b.HandleJumpScareRpc(info);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_HandLockState(NetworkString<_64> zoneId, bool locked, RpcInfo info = default)
    {
        string id = zoneId.ToString();
        _handLockStates[id] = locked;

        if (log)
            Debug.Log($"[FusionRpcHub] RPC_HandLockState received. zoneId='{id}' locked={locked} source={info.Source} local={Runner?.LocalPlayer}", this);

        HandLockStateChanged?.Invoke(id, locked, info.Source);
    }
}
