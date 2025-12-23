using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Fusion;

public sealed class RequestedObjectsState : NetworkBehaviour
{
    public static RequestedObjectsState Instance { get; private set; }

    [Header("Config (edit in Inspector)")]
    [SerializeField] private RequestedObjectId[] possibleObjects = new[]
    {
        RequestedObjectId.Candle,
        RequestedObjectId.Talisman,
        RequestedObjectId.CherryTwig,
        RequestedObjectId.Bottle
    };

    [SerializeField, Min(1)] private int requestedCount = 3;

    [Header("Debug (read only at runtime)")]
    [SerializeField, TextArea(3, 6)] private string debugRequestedList;
    [SerializeField, TextArea(2, 4)] private string debugProgress;

    // ---- Networked selection (3 slots) ----
    [Networked] public int Req0 { get; private set; }
    [Networked] public int Req1 { get; private set; }
    [Networked] public int Req2 { get; private set; }

    // ---- Networked completion ----
    [Networked] public bool Done0 { get; private set; }
    [Networked] public bool Done1 { get; private set; }
    [Networked] public bool Done2 { get; private set; }
    [Networked] public bool AllDone { get; private set; }

    public override void Spawned()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[RequestedObjectsState] Duplicate instance detected, destroying new one.");
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (Object.HasStateAuthority)
        {
            PickNewRequestedList();
        }

        RefreshDebugStrings();
    }

    private void Update()
    {
        // keep inspector debug readable even for non-authority clients
        RefreshDebugStrings();
    }

    public RequestedObjectId GetRequestedId(int slot)
    {
        return slot switch
        {
            0 => (RequestedObjectId)Req0,
            1 => (RequestedObjectId)Req1,
            2 => (RequestedObjectId)Req2,
            _ => (RequestedObjectId)(-1),
        };
    }

    public int CompletedCount => (Done0 ? 1 : 0) + (Done1 ? 1 : 0) + (Done2 ? 1 : 0);

    public void PickNewRequestedList()
    {
        if (!Object.HasStateAuthority)
        {
            Debug.LogWarning("[RequestedObjectsState] PickNewRequestedList called without StateAuthority. Ignored.");
            return;
        }

        int n = possibleObjects != null ? possibleObjects.Length : 0;
        if (n <= 0)
        {
            Debug.LogError("[RequestedObjectsState] possibleObjects is empty. Cannot pick requested list.");
            return;
        }

        int k = Mathf.Clamp(requestedCount, 1, 3);
        k = Mathf.Min(k, n);

        // unique random pick by shuffling indices
        var indices = new List<int>(n);
        for (int i = 0; i < n; i++) indices.Add(i);

        for (int i = 0; i < indices.Count; i++)
        {
            int j = Random.Range(i, indices.Count);
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }

        // Pick in shuffled order, but enforce: at most ONE request whose name ends with "Doll".
        // (We keep uniqueness for the rest.)
        var picked = new List<RequestedObjectId>(k);
        bool pickedDoll = false;

        for (int t = 0; t < indices.Count && picked.Count < k; t++)
        {
            var id = possibleObjects[indices[t]];
            bool isDoll = id.ToString().EndsWith("Doll");

            if (isDoll && pickedDoll)
                continue;

            picked.Add(id);
            if (isDoll) pickedDoll = true;
        }

        if (picked.Count < k)
        {
            Debug.LogError($"[RequestedObjectsState] Cannot fill {k} requested slots with current possibleObjects under Doll rule. " +
                           $"Picked={picked.Count}. Add more non-doll options or reduce requestedCount.");
            // Keep behavior safe: fill remaining slots with -1 (but this will block completion if you require all 3).
        }

        Req0 = (picked.Count >= 1) ? (int)picked[0] : -1;
        Req1 = (picked.Count >= 2) ? (int)picked[1] : -1;
        Req2 = (picked.Count >= 3) ? (int)picked[2] : -1;

        Done0 = Done1 = Done2 = false;
        AllDone = false;

        Debug.Log($"[RequestedObjectsState] Picked requested list: " +
                  $"slot0={(RequestedObjectId)Req0}, slot1={(RequestedObjectId)Req1}, slot2={(RequestedObjectId)Req2}");
    }

    /// <summary>
    /// Called by target zones when an object enters that zone.
    /// slotIndex decides which requested item this target expects.
    /// </summary>
public bool TrySubmitToSlot(int slotIndex, RequestedObjectId incoming, string zoneName)
{
    if (!Object.HasStateAuthority)
    {
        Debug.Log($"[RequestedObjectsState] Submit ignored (no authority). zone={zoneName}, incoming={incoming}");
        return false;
    }

    var required = GetRequestedId(slotIndex);
    Debug.Log($"[RequestedObjectsState] Submit attempt zone={zoneName}, slot={slotIndex}, incoming={incoming}, required={required}");

    if ((int)required < 0)
    {
        Debug.LogWarning($"[RequestedObjectsState] Slot {slotIndex} has no requested object (Req is -1).");
        return false;
    }

    if (incoming != required)
    {
        Debug.Log($"[RequestedObjectsState] MISMATCH: incoming {incoming} != required {required} (slot {slotIndex})");
        return false;
    }

    bool justCompleted = false;

    // mark done once
    if (slotIndex == 0 && !Done0) { Done0 = true; justCompleted = true; }
    else if (slotIndex == 1 && !Done1) { Done1 = true; justCompleted = true; }
    else if (slotIndex == 2 && !Done2) { Done2 = true; justCompleted = true; }
    else
    {
        Debug.Log($"[RequestedObjectsState] Slot {slotIndex} already done. Ignored.");
        return false;
    }

    Debug.Log($"[RequestedObjectsState] ✅ SLOT {slotIndex} COMPLETED by {incoming}. Progress {CompletedCount}/3");

    if (!AllDone && Done0 && Done1 && Done2)
    {
        AllDone = true;
        Debug.Log("[RequestedObjectsState] 🎉 ALL 3 REQUESTED OBJECTS COMPLETED. AllDone = true");
    }

    return justCompleted;
}

    private void RefreshDebugStrings()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Requested slot0: {(RequestedObjectId)Req0}");
        sb.AppendLine($"Requested slot1: {(RequestedObjectId)Req1}");
        sb.AppendLine($"Requested slot2: {(RequestedObjectId)Req2}");
        debugRequestedList = sb.ToString();

        debugProgress = $"Done0={Done0}, Done1={Done1}, Done2={Done2} | Completed={CompletedCount}/3 | AllDone={AllDone}";
    }
}
