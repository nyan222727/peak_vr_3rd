using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Fusion;
using Fusion.Sockets;
using System.Threading.Tasks;

public class SpawnManager : NetworkBehaviour, INetworkRunnerCallbacks
{
    public NetworkPrefabRef[] NPCPrefab;
    public Transform[] NPCPos;

    [Networked] private NetworkBool NpcSpawned { get; set; }

    // Start is called before the first frame update
    void Awake()
    {
        BasicSpawner.Instance.runner.AddCallbacks(this);
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (runner.LocalPlayer != player) return;

        if(!NpcSpawned) SpawnAll(runner);
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        // 剩餘玩家排序，挑最小當下一任
        var remaining = runner.ActivePlayers.OrderBy(p => p.RawEncoded).ToList();
        if (remaining.Count == 0) return;
        
        var nextOwner = remaining.First();
        bool iAmNext = runner.LocalPlayer == nextOwner;

        // 當前沒有權限、且我被選中 → 主動請求接手 SpawnManager 與所有 NPC
        if (iAmNext)
        {
            Debug.Log("me!");
            foreach (var npc in NPCRegistry.AllNPCsSnapshot())
            {
                if (npc && npc.Object && !npc.Object.HasStateAuthority)
                    npc.Object.RequestStateAuthority();
            }
        }
    }

    private void SpawnAll(NetworkRunner runner)
    {
        NpcSpawned = true;
        int num = 0;
        foreach(NetworkPrefabRef obj in NPCPrefab)
        {
            var npc = runner.Spawn(obj, NPCPos[num].position, NPCPos[num].rotation, inputAuthority: null);
            var behavior = npc.GetComponent<NPCBehavior>();
            if (behavior != null) NPCRegistry.Register(behavior);
            num++;
        }
    }

    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner r, NetDisconnectReason reason) { }
    public void OnConnectedToServer(NetworkRunner runner, NetAddress address, NetConnectFailedReason reason) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress address, NetConnectFailedReason reason) { }
    public void OnConnectRequest(NetworkRunner r, NetworkRunnerCallbackArgs.ConnectRequest req, byte[] token) {}
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player){ }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player){ }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data){ }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress){ }
}
