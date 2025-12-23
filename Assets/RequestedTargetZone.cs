using UnityEngine;
using UnityEngine.VFX;

[RequireComponent(typeof(Collider))]
public sealed class RequestedTargetZone : MonoBehaviour
{
    [SerializeField, Range(0, 2)] private int slotIndex = 0;
    [SerializeField] private bool debugLogs = true;

    private void Reset()
    {
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
            Debug.LogError($"[RequestedTargetZone:{name}] RequestedObjectsState.Instance is null.");
            return;
        }

        if (debugLogs)
            Debug.Log($"[RequestedTargetZone:{name}] Enter: incoming={reqObj.ObjectId}, slot={slotIndex}");

        // ✅ Minimal: use return value to decide whether to play effects
        bool success = RequestedObjectsState.Instance.TrySubmitToSlot(slotIndex, reqObj.ObjectId, name);
        if (!success) return;

        // 1) placed object VFX: child named "Smoke 3"
        PlayChildVfxByExactName(reqObj.transform, "Smoke 3");

        // 2) target VFX: child named "Smoke2" (no space)
        PlayChildVfxByExactName(transform, "Smoke2");

        // 3) enable glow* mesh renderers under target
        EnableGlowRenderers(transform);
    }

    private static void PlayChildVfxByExactName(Transform root, string exactName)
    {
        // find child transform by name (include inactive)
        var all = root.GetComponentsInChildren<Transform>(true);
        foreach (var t in all)
        {
            if (t.name != exactName) continue;

            // VFX Graph
            var vfx = t.GetComponent<VisualEffect>();
            if (vfx != null)
            {
                vfx.Play();
                if(t.name == "Smoke2")
                {
                    Debug.Log("play vfx of target.");
                }
                else
                {
                    Debug.Log("play vfx of placed object.");
                }
                
            } 

            // ParticleSystem fallback (if some prefabs use PS instead of VFX)
            var ps = t.GetComponent<ParticleSystem>();
            if (ps != null) ps.Play();

            return;
        }
    }

    private static void EnableGlowRenderers(Transform root)
    {
        var all = root.GetComponentsInChildren<Transform>(true);
        foreach (var t in all)
        {
            if (!t.name.StartsWith("glow", System.StringComparison.OrdinalIgnoreCase))
                continue;

            var renderers = t.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var r in renderers)
                r.enabled = true;
        }
    }
}
