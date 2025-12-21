using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Fusion;
using Fusion.Sockets;
using System.Threading.Tasks;

public class SpawnManager : NetworkBehaviour, INetworkRunnerCallbacks
{
    public NetworkPrefabRef[] NPCPrefab;
    public Transform[] NPCPos;
    
    public NetworkPrefabRef[] randomPrefabs;   // prefabs that will be placed randomly
    //public Transform[] candidatePoints;        // all candidate spawn locations
    [Header("Random Spawn")]
    public Transform candidatePointsParent;   // drag `CandidatePoints` here

    private Transform[] _candidatePoints;    // filled automatically at runtime

    [Tooltip("How many random objects to spawn each round")]
    public int randomSpawnCount = 9;


    [Networked] private NetworkBool NpcSpawned { get; set; }

    private void Awake()
    {
        // Original behaviour – make sure SpawnManager gets runner callbacks
        BasicSpawner.Instance.runner.AddCallbacks(this);

        // New behaviour – build candidate point array for random spawns
        BuildCandidatePoints();
    }

public override void Spawned()
{
    // Runner is valid here
    Runner.AddCallbacks(this);
    TrySpawnNow();
}

public void TrySpawnNow()
{
    // Must have state authority to spawn (important in many setups)
    if (Runner == null || !Runner.IsRunning) return;
    if (!Object.HasStateAuthority) return;

    if (!NpcSpawned)
        SpawnAll(Runner);
}

private void BuildCandidatePoints()
{
    if (candidatePointsParent == null)
    {
        // No random candidate points in this scene; that's fine.
        // Keep _candidatePoints as null so SpawnRandomPrefabs() skips gracefully.
        _candidatePoints = null;
        return;
    }

    int childCount = candidatePointsParent.childCount;
    _candidatePoints = new Transform[childCount];

    for (int i = 0; i < childCount; i++)
    {
        _candidatePoints[i] = candidatePointsParent.GetChild(i);
    }
}

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (runner.LocalPlayer != player) return;

        if (!NpcSpawned) SpawnAll(runner);
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

    /*private void SpawnAll(NetworkRunner runner)
    {
        NpcSpawned = true;
        int num = 0;
        foreach (NetworkPrefabRef obj in NPCPrefab)
        {
            var npc = runner.Spawn(obj, NPCPos[num].position, NPCPos[num].rotation, inputAuthority: null);
            var behavior = npc.GetComponent<NPCBehavior>();
            if (behavior != null) NPCRegistry.Register(behavior);
            num++;
        }
    }*/

    private void SpawnAll(NetworkRunner runner)
    {
        NpcSpawned = true;

        // --- fixed spawns (existing behaviour, but with bounds check) ---
        int count = Mathf.Min(NPCPrefab.Length, NPCPos.Length);
        for (int i = 0; i < count; i++)
        {
            var npc = runner.Spawn(NPCPrefab[i], NPCPos[i].position, NPCPos[i].rotation, inputAuthority: null);
            var behavior = npc.GetComponent<NPCBehavior>();
            if (behavior != null) NPCRegistry.Register(behavior);
        }

        // --- extra random spawns ---
        SpawnRandomPrefabs(runner);
    }

private void SpawnRandomPrefabs(NetworkRunner runner)
{
    if (randomPrefabs == null || randomPrefabs.Length == 0)
        return;
    if (_candidatePoints == null || _candidatePoints.Length == 0)
        return;

    int spawnSlots = Mathf.Min(randomSpawnCount, _candidatePoints.Length);

    // we can only guarantee each prefab once if we have enough slots
    int guaranteeCount = Mathf.Min(spawnSlots, randomPrefabs.Length);

    // --- shuffled list of candidate point indices (no reuse) ---
    List<int> pointIndices = new List<int>(_candidatePoints.Length);
    for (int i = 0; i < _candidatePoints.Length; i++)
        pointIndices.Add(i);

    // Fisher–Yates shuffle
    for (int i = pointIndices.Count - 1; i > 0; i--)
    {
        int j = UnityEngine.Random.Range(0, i + 1);
        int tmp = pointIndices[i];
        pointIndices[i] = pointIndices[j];
        pointIndices[j] = tmp;
    }

    int spawnIndex = 0;

    // 1) guarantee: each prefab at least once (up to spawnSlots)
    for (int i = 0; i < guaranteeCount; i++, spawnIndex++)
    {
        int pointIndex = pointIndices[spawnIndex];
        Transform point = _candidatePoints[pointIndex];

        NetworkPrefabRef prefab = randomPrefabs[i];

        var obj = runner.Spawn(prefab, point.position, point.rotation);
        var behavior = obj.GetComponent<NPCBehavior>();
        if (behavior != null) NPCRegistry.Register(behavior);
    }

    // 2) fill remaining slots with random prefabs
    for (; spawnIndex < spawnSlots; spawnIndex++)
    {
        int pointIndex = pointIndices[spawnIndex];
        Transform point = _candidatePoints[pointIndex];

        int prefabIndex = UnityEngine.Random.Range(0, randomPrefabs.Length);
        NetworkPrefabRef prefab = randomPrefabs[prefabIndex];

        var obj = runner.Spawn(prefab, point.position, point.rotation);
        var behavior = obj.GetComponent<NPCBehavior>();
        if (behavior != null) NPCRegistry.Register(behavior);
    }
}


  



    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner r, NetDisconnectReason reason) { }
    public void OnConnectedToServer(NetworkRunner runner, NetAddress address, NetConnectFailedReason reason) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress address, NetConnectFailedReason reason) { }
    public void OnConnectRequest(NetworkRunner r, NetworkRunnerCallbackArgs.ConnectRequest req, byte[] token) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
}
