using System;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Fusion.Sockets;

public class FusionDebugCallbacks : MonoBehaviour, INetworkRunnerCallbacks
{
    // ===== 你真正關心的幾個 Debug Log =====

    public void OnConnectedToServer(NetworkRunner runner)
    {
        Debug.Log("[FusionDebug] OnConnectedToServer");
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"[FusionDebug] OnPlayerJoined: {player.PlayerId}");
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.Log($"[FusionDebug] OnDisconnectedFromServer: {reason}");
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        Debug.LogError($"[FusionDebug] OnConnectFailed: {reason}");
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log($"[FusionDebug] OnShutdown: {shutdownReason}");
    }

    // ===== 其餘 INetworkRunnerCallbacks 必填的 stub =====
    // 這些先留空就好，不影響使用

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }

    public void OnInput(NetworkRunner runner, NetworkInput input) { }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        Debug.Log("[FusionDebug] OnSceneLoadDone");
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
        Debug.Log("[FusionDebug] OnSceneLoadStart");
    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player, bool isLocal)
    {
        // 如果你的 Fusion 版本沒有這個 overload，這個方法可以刪掉
    }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player, bool isLocal)
    {
        // 如果你的 Fusion 版本沒有這個 overload，這個方法可以刪掉
    }
}
