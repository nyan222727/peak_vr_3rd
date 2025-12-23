using UnityEngine;

public class FocusRaycaster : MonoBehaviour
{
    [Header("Wired in Inspector")]
    public Transform headCameraTransform;

    [Header("Focus Settings")]
    public LayerMask focusLayers = ~0;
    public float maxDistance = 3.0f;

    [Tooltip("0 = Raycast. >0 = SphereCast radius.")]
    public float sphereRadius = 0.03f;

    [Tooltip("Optional: only accept hits that have ObjectDescription somewhere up the hierarchy.")]
    public bool requireDescriptionComponent = true;

    public ObjectDescription Current { get; private set; }
    public RaycastHit LastHit { get; private set; }

    private void Reset()
    {
        headCameraTransform = Camera.main ? Camera.main.transform : null;
    }

    private void Update()
    {
        Current = null;

        if (!headCameraTransform) return;

        var origin = headCameraTransform.position;
        var dir = headCameraTransform.forward;

        bool hit;
        RaycastHit h;

        if (sphereRadius > 0f)
            hit = Physics.SphereCast(origin, sphereRadius, dir, out h, maxDistance, focusLayers, QueryTriggerInteraction.Ignore);
        else
            hit = Physics.Raycast(origin, dir, out h, maxDistance, focusLayers, QueryTriggerInteraction.Ignore);

        if (!hit) return;

        LastHit = h;

        var desc = h.collider.GetComponentInParent<ObjectDescription>();
        if (!requireDescriptionComponent || desc != null)
            Current = desc;
    }
}
