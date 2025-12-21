using UnityEngine;

[RequireComponent(typeof(Collider))]
public sealed class RequestedTargetZone : MonoBehaviour
{
    [SerializeField, Range(0, 2)] private int slotIndex = 0;
    [SerializeField] private bool debugLogs = true;

    private void Reset()
    {
        // Make sure it’s a trigger zone
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        var reqObj = other.GetComponentInParent<RequestableObject>();
        if (reqObj == null)
        {
            if (debugLogs) Debug.Log($"[RequestedTargetZone:{name}] Enter by {other.name} but no RequestableObject found in parent.");
            return;
        }

        if (RequestedObjectsState.Instance == null)
        {
            Debug.LogError($"[RequestedTargetZone:{name}] RequestedObjectsState.Instance is null. Did you add it to RequestedObjects with NetworkObject?");
            return;
        }

        if (debugLogs)
        {
            Debug.Log($"[RequestedTargetZone:{name}] Enter: incoming={reqObj.ObjectId}, slot={slotIndex}");
        }

        RequestedObjectsState.Instance.TrySubmitToSlot(slotIndex, reqObj.ObjectId, name);
    }
}
