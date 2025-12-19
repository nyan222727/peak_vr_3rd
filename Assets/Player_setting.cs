using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;

public class Player_setting : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("Assign in Inspector")]
    public NetworkObject playerPrefab;   // ✅ 不是 GameObject
    public Transform spawnPoint;         // 可選，不填就用 Vector3.zero

    private NetworkRunner _runner;
    private NetworkObject _localPlayerObj;

    void Start()
    {
        // 1) 找到場上正在跑的 Runner（通常是 FusionLauncher Instantiate 出來的那個）
        _runner = FindObjectOfType<NetworkRunner>();

        if (_runner == null)
        {
            Debug.LogError("[Player_setting] Can't find NetworkRunner in scene.");
            return;
        }

        // 2) 註冊 callbacks，讓 OnPlayerJoined 會被呼叫
        _runner.AddCallbacks(this);
    }

    // ✅ 玩家進房回呼：Spawn 自己的角色
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        // 只在「本機玩家」加入時，生自己的角色（避免每個人都幫別人生）
        if (player != runner.LocalPlayer) return;

        if (_localPlayerObj != null) return; // 避免重複生成

        Vector3 pos = spawnPoint ? spawnPoint.position : Vector3.zero;
        Quaternion rot = spawnPoint ? spawnPoint.rotation : Quaternion.identity;

        _localPlayerObj = runner.Spawn(playerPrefab, pos, rot, player);
        Debug.Log($"[Player_setting] Spawn local player: {player}");
    }

    // ===== 其他 callbacks 先留空就好 =====
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
}
