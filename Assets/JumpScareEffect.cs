using System.Collections;
using UnityEngine;
using Fusion;

public class JumpScareEffect : NetworkBehaviour
{
    [Header("References")]
    [Tooltip("Head transform (use HeadTrackers in your prefab).")]
    public Transform headTransform;

    [Tooltip("Ghost placeholder (GhostCube child).")]
    public GameObject ghostObject;

    [Header("Movement")]
    public float startDistance = 4f;   // meters in front of head
    public float endDistance   = 0.1f; // how close to the face
    public float duration      = 0.5f; // rush time in seconds

    [Header("Transparency")]
    [Range(0f, 1f)] public float startAlpha = 0.2f;
    [Range(0f, 1f)] public float endAlpha   = 1f;

    // Local material instance so we don't modify a shared asset
    Material _materialInstance;
    bool _isRunning;
    Coroutine _routine;

    // Local player’s instance on THIS client (static is per-process, not networked)
    public static JumpScareEffect Local { get; private set; }

    public override void Spawned()
    {
        // Register the local player on this client
        if (Object.HasInputAuthority)
            Local = this;

        if (ghostObject != null)
        {
            ghostObject.SetActive(false);

            var renderer = ghostObject.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                _materialInstance = renderer.material;
                SetAlpha(0f);
            }
        }
    }

    /// <summary>
    /// Called from UI Button (on this client) to start the effect.
    /// </summary>
    public void Trigger()
    {
        // Safety: only let the local player run its own effect
        if (!Object.HasInputAuthority) return;
        if (_isRunning) return;
        if (ghostObject == null || headTransform == null) return;

        if (_routine != null)
            StopCoroutine(_routine);

        _routine = StartCoroutine(JumpScareRoutine());
    }

    IEnumerator JumpScareRoutine()
    {
        _isRunning = true;
        ghostObject.SetActive(true);

        // World-space positions in front of the head
        Vector3 startPos = headTransform.position + headTransform.forward * startDistance;
        Vector3 endPos   = headTransform.position + headTransform.forward * endDistance;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float eased = Mathf.SmoothStep(0f, 1f, t);

            ghostObject.transform.position = Vector3.Lerp(startPos, endPos, eased);
            ghostObject.transform.LookAt(headTransform.position);

            float a = Mathf.Lerp(startAlpha, endAlpha, eased);
            SetAlpha(a);

            yield return null;
        }

        ghostObject.SetActive(false);
        SetAlpha(startAlpha);

        _isRunning = false;
        _routine = null;
    }

    void SetAlpha(float a)
    {
        if (_materialInstance == null) return;

        Color c = _materialInstance.color;
        c.a = a;
        _materialInstance.color = c;
    }
}
