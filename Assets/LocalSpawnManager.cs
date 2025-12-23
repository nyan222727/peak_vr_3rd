using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LocalSpawnManager : MonoBehaviour
{
    [Header("Fixed Spawns")]
    public GameObject[] NPCPrefab;   // was NetworkPrefabRef[]
    public Transform[] NPCPos;

    [Header("Random Spawn")]
    public GameObject[] randomPrefabs;         // was NetworkPrefabRef[]
    public Transform candidatePointsParent;    // drag CandidatePoints parent here
    private Transform[] _candidatePoints;

    [Tooltip("How many random objects to spawn each round")]
    public int randomSpawnCount = 9;

    [Header("Spawn Timing")]
    [Tooltip("If true, spawn automatically on Start(). Otherwise call TrySpawnNow() manually.")]
    public bool spawnOnStart = true;

    private bool _spawned;

    private void Awake()
    {
        BuildCandidatePoints();
    }

    private void Start()
    {
        if (spawnOnStart)
            TrySpawnNow();
    }

    public void TrySpawnNow()
    {
        if (_spawned) return;
        SpawnAll();
    }

    private void BuildCandidatePoints()
    {
        if (candidatePointsParent == null)
        {
            _candidatePoints = null;
            return;
        }

        int childCount = candidatePointsParent.childCount;
        _candidatePoints = new Transform[childCount];
        for (int i = 0; i < childCount; i++)
            _candidatePoints[i] = candidatePointsParent.GetChild(i);
    }

    private void SpawnAll()
    {
        _spawned = true;

        // --- fixed spawns ---
        int count = Mathf.Min(NPCPrefab?.Length ?? 0, NPCPos?.Length ?? 0);
        for (int i = 0; i < count; i++)
        {
            if (NPCPrefab[i] == null || NPCPos[i] == null) continue;

            var go = Instantiate(NPCPrefab[i], NPCPos[i].position, NPCPos[i].rotation);

            // Keep your registry behavior if you still use it locally
            var behavior = go.GetComponent<NPCBehavior>();
            if (behavior != null) NPCRegistry.Register(behavior);
        }

        // --- random spawns ---
        SpawnRandomPrefabs();
    }

    private void SpawnRandomPrefabs()
    {
        if (randomPrefabs == null || randomPrefabs.Length == 0) return;
        if (_candidatePoints == null || _candidatePoints.Length == 0) return;

        int spawnSlots = Mathf.Min(randomSpawnCount, _candidatePoints.Length);
        int guaranteeCount = Mathf.Min(spawnSlots, randomPrefabs.Length);

        // shuffled candidate point indices (no reuse)
        List<int> pointIndices = new List<int>(_candidatePoints.Length);
        for (int i = 0; i < _candidatePoints.Length; i++)
            pointIndices.Add(i);

        // Fisher–Yates shuffle
        for (int i = pointIndices.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (pointIndices[i], pointIndices[j]) = (pointIndices[j], pointIndices[i]);
        }

        int spawnIndex = 0;

        // 1) guarantee each prefab once (up to spawnSlots)
        for (int i = 0; i < guaranteeCount; i++, spawnIndex++)
        {
            int pointIndex = pointIndices[spawnIndex];
            Transform point = _candidatePoints[pointIndex];
            var prefab = randomPrefabs[i];
            if (prefab == null || point == null) continue;

            var go = Instantiate(prefab, point.position, point.rotation);
            var behavior = go.GetComponent<NPCBehavior>();
            if (behavior != null) NPCRegistry.Register(behavior);
        }

        // 2) fill remaining slots with random prefabs
        for (; spawnIndex < spawnSlots; spawnIndex++)
        {
            int pointIndex = pointIndices[spawnIndex];
            Transform point = _candidatePoints[pointIndex];
            if (point == null) continue;

            int prefabIndex = Random.Range(0, randomPrefabs.Length);
            var prefab = randomPrefabs[prefabIndex];
            if (prefab == null) continue;

            var go = Instantiate(prefab, point.position, point.rotation);
            var behavior = go.GetComponent<NPCBehavior>();
            if (behavior != null) NPCRegistry.Register(behavior);
        }
    }
}
