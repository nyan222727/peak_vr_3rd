using System;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class SpawnManager2 : NetworkBehaviour
{
    [Header("Fixed Spawns")]
    public NetworkPrefabRef[] NPCPrefab;
    public Transform[] NPCPos;

    [Header("Random Spawns")]
    public NetworkPrefabRef[] randomPrefabs;
    public Transform candidatePointsParent;
    public int randomSpawnCount = 9;

    private Transform[] _candidatePoints;

    // Host-only: spawn once and replicate to everyone
    [Networked] private NetworkBool WorldSpawned { get; set; }

    private void Awake()
    {
        BuildCandidatePoints();
    }

    public override void Spawned()
    {
        Debug.Log($"[SpawnManager] Spawned. HasStateAuthority={Object.HasStateAuthority}, IsServer={Runner.IsServer}, WorldSpawned={WorldSpawned}");
    }

    private void BuildCandidatePoints()
    {
        if (candidatePointsParent == null)
        {
            _candidatePoints = null;
            Debug.Log("[SpawnManager] candidatePointsParent is NULL -> random spawns disabled (fixed spawns still work).");
            return;
        }

        int childCount = candidatePointsParent.childCount;
        _candidatePoints = new Transform[childCount];
        for (int i = 0; i < childCount; i++)
            _candidatePoints[i] = candidatePointsParent.GetChild(i);

        Debug.Log($"[SpawnManager] Candidate points built: {childCount}");
    }

    /// <summary>
    /// Call this ONLY from host side, after notebook burn (or whenever you want).
    /// Safe to call multiple times; it will spawn only once.
    /// </summary>
    public void TrySpawnWorld()
    {
        if (Object == null)
        {
            Debug.LogError("[SpawnManager] TrySpawnWorld called but NetworkObject is null.");
            return;
        }

        if (!Object.HasStateAuthority || !Runner.IsServer)
        {
            Debug.Log($"[SpawnManager] TrySpawnWorld ignored (not host authority). HasStateAuthority={Object.HasStateAuthority}, IsServer={Runner.IsServer}");
            return;
        }

        if (WorldSpawned)
        {
            Debug.Log("[SpawnManager] TrySpawnWorld ignored (already spawned).");
            return;
        }

        Debug.Log("[SpawnManager] Host is spawning world NOW.");
        WorldSpawned = true;

        SpawnFixed();
        SpawnRandom();
    }

    private void SpawnFixed()
    {
        int count = Mathf.Min(NPCPrefab?.Length ?? 0, NPCPos?.Length ?? 0);
        Debug.Log($"[SpawnManager] Spawning fixed objects: {count}");

        for (int i = 0; i < count; i++)
        {
            if (NPCPos[i] == null)
            {
                Debug.LogWarning($"[SpawnManager] NPCPos[{i}] is null, skipping.");
                continue;
            }

            var obj = Runner.Spawn(NPCPrefab[i], NPCPos[i].position, NPCPos[i].rotation, inputAuthority: null);
            Debug.Log($"[SpawnManager] Spawned fixed [{i}] -> {obj.name} at {NPCPos[i].position}");
        }
    }

    private void SpawnRandom()
    {
        if (randomPrefabs == null || randomPrefabs.Length == 0)
        {
            Debug.Log("[SpawnManager] No randomPrefabs -> skipping random spawn.");
            return;
        }

        if (_candidatePoints == null || _candidatePoints.Length == 0)
        {
            Debug.Log("[SpawnManager] No candidate points -> skipping random spawn.");
            return;
        }

        int spawnSlots = Mathf.Min(randomSpawnCount, _candidatePoints.Length);
        int guaranteeCount = Mathf.Min(spawnSlots, randomPrefabs.Length);

        Debug.Log($"[SpawnManager] Random spawn: spawnSlots={spawnSlots}, prefabs={randomPrefabs.Length}, guaranteeCount={guaranteeCount}");

        // shuffle point indices (no reuse)
        List<int> pointIndices = new List<int>(_candidatePoints.Length);
        for (int i = 0; i < _candidatePoints.Length; i++) pointIndices.Add(i);

        for (int i = pointIndices.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (pointIndices[i], pointIndices[j]) = (pointIndices[j], pointIndices[i]);
        }

        int spawnIndex = 0;

        // 1) guarantee each prefab once if we have enough slots
        for (int i = 0; i < guaranteeCount; i++, spawnIndex++)
        {
            Transform point = _candidatePoints[pointIndices[spawnIndex]];
            var obj = Runner.Spawn(randomPrefabs[i], point.position, point.rotation);
            Debug.Log($"[SpawnManager] Random GUARANTEE [{i}] -> {obj.name} at {point.position}");
        }

        // 2) fill the rest randomly
        for (; spawnIndex < spawnSlots; spawnIndex++)
        {
            Transform point = _candidatePoints[pointIndices[spawnIndex]];
            int prefabIndex = UnityEngine.Random.Range(0, randomPrefabs.Length);
            var obj = Runner.Spawn(randomPrefabs[prefabIndex], point.position, point.rotation);
            Debug.Log($"[SpawnManager] Random FILL -> {obj.name} (prefabIndex={prefabIndex}) at {point.position}");
        }
    }
}
