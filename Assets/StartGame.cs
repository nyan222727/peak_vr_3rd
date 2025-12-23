using System.Collections.Generic;
using UnityEngine;

public class StartGameController : MonoBehaviour
{
    public static StartGameController Instance { get; private set; }

    [Header("Global Start State")]
    [SerializeField] private bool startGame = false;

    [Header("When StartGame = true, deactivate these (old phase objects)")]
    [SerializeField] private List<GameObject> objectsToDeactivateOnStart = new List<GameObject>();

    [Header("When StartGame = true, activate these (new phase objects)")]
    [SerializeField] private List<GameObject> objectsToActivateOnStart = new List<GameObject>();

    [Header("Optional")]
    [SerializeField] private LocalSpawnManager localspawnManager;

    private bool _applied = false;

    public bool StartGame
    {
        get => startGame;
        set
        {
            startGame = value;

            if (!startGame)
            {
                CancelInvoke(nameof(ApplyStart));
                return;
            }

            // Apply after 3 seconds (only once)
            if (!_applied && !IsInvoking(nameof(ApplyStart)))
                Invoke(nameof(ApplyStart), 3f);
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // NOTE: we do NOT deactivate here anymore (per your requirement).
    }

    private void Update()
    {
        // If startGame is flipped via Inspector (not through property), still respect 3s delay
        if (!_applied && startGame && !IsInvoking(nameof(ApplyStart)))
            Invoke(nameof(ApplyStart), 3f);
    }

    private void ApplyStart()
    {
        if (_applied) return;
        _applied = true;

        // 1) Deactivate old objects
        SetObjectsActive(objectsToDeactivateOnStart, false);

        // 2) Activate new objects (special handling for Plate)
        SetObjectsActive(objectsToActivateOnStart, true);

        // 3) Optional: trigger host-only spawn logic
        //if (spawnManager != null)
          //  spawnManager.TrySpawnNow();
        if (localspawnManager != null)
            localspawnManager.TrySpawnNow();
    }

    /// Call this if you want to trigger immediately (e.g., for debugging)
    public void ForceApplyNow()
    {
        CancelInvoke(nameof(ApplyStart));
        ApplyStart();
    }

    /// Generic toggler with Plate special-case:
    /// - Plate: toggle MeshRenderer + MeshCollider (children included)
    /// - Others: toggle root SetActive
    public void SetObjectsActive(List<GameObject> list, bool active)
    {
        if (list == null) return;

        foreach (var go in list)
        {
            if (go == null) continue;

            if (go.CompareTag("Plate"))
            {
                EnablePlateVisualAndMeshCollider(go, active);
            }
            else
            {
                go.SetActive(active);
            }
        }
    }

    private static void EnablePlateVisualAndMeshCollider(GameObject plateRoot, bool enabled)
    {
        var renderer = plateRoot.GetComponent<MeshRenderer>();
        renderer.enabled = enabled;
        plateRoot.GetComponent<HandLockZone>().SetStartGameActive(true);

        var meshColliders = plateRoot.GetComponentsInChildren<MeshCollider>(true);
        foreach (var c in meshColliders) c.enabled = enabled;

        // We do NOT touch BoxCollider triggers / NetworkObject / NetworkTransform.
    }
}
