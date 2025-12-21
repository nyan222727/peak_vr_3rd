using System.Collections;
using UnityEngine;
using Fusion;

/// <summary>
/// Per-object ghost interference controller.
/// For now, wired to NetworkCandle and only does "shake" effects.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class GhostHauntTarget : NetworkBehaviour
{
    public enum InterferencePattern
    {
        SmallShake = 0,
        BigShake   = 1,
    }

    [Header("References")]
    [SerializeField] private NetworkCandle _candle;      // assign in Inspector
    [SerializeField] private Transform _visualRoot;      // optional; defaults to this.transform

    [Header("Shake settings")]
    public float shakeDuration = 0.6f;

    public float smallPosAmplitude = 0.01f;
    public float smallRotAmplitude = 5f;

    public float bigPosAmplitude   = 0.03f;
    public float bigRotAmplitude   = 15f;

    Coroutine _runningRoutine;

    Transform VisualRoot => _visualRoot != null ? _visualRoot : transform;

    /// <summary>
    /// True if this object is currently held by someone.
    /// (For now we just proxy NetworkCandle's IsHeld.)
    /// </summary>
    public bool IsHeld =>
        _candle != null && _candle.IsHeldByAnyone;

    // --- RPC entry point --------------------------------------------------

    /// <summary>
    /// Called by the GhostInterferenceManager.
    /// RPC goes to StateAuthority, which actually runs the shake.
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_TriggerInterference(InterferencePattern pattern)
    {
        if (!IsHeld)
        {
            // Optional: early out if not being held.
            return;
        }

        if (_runningRoutine != null)
            StopCoroutine(_runningRoutine);

        _runningRoutine = StartCoroutine(ShakeRoutine(pattern));
    }

    // --- Local shake implementation (runs only on StateAuthority) ---------

    private IEnumerator ShakeRoutine(InterferencePattern pattern)
    {
        var root = VisualRoot;

        Vector3 basePos = root.localPosition;
        Quaternion baseRot = root.localRotation;

        float duration = shakeDuration;
        float t = 0f;

        // Pick amplitudes based on pattern
        float posAmp = (pattern == InterferencePattern.SmallShake)
            ? smallPosAmplitude
            : bigPosAmplitude;

        float rotAmp = (pattern == InterferencePattern.SmallShake)
            ? smallRotAmplitude
            : bigRotAmplitude;

        // Slight randomization per call so it doesn't feel identical
        float posJitter = Random.Range(0.7f, 1.3f);
        float rotJitter = Random.Range(0.7f, 1.3f);

        posAmp *= posJitter;
        rotAmp *= rotJitter;

        while (t < duration)
        {
            t += Time.deltaTime;
            float normalized = Mathf.Clamp01(t / duration);

            // Ease-out so it calms down at the end
            float strength = 1f - normalized;
            strength = strength * strength; // quadratic falloff

            // Position jitter (small offset in local space)
            Vector3 posOffset = Random.insideUnitSphere * posAmp * strength;

            // Rotation jitter around random local axis
            Vector3 rotAxis = Random.onUnitSphere;
            float rotAngle = rotAmp * strength;
            Quaternion rotOffset = Quaternion.AngleAxis(rotAngle, rotAxis);

            root.localPosition = basePos + posOffset;
            root.localRotation = baseRot * rotOffset;

            yield return null;
        }

        // Restore original transform
        root.localPosition = basePos;
        root.localRotation = baseRot;

        _runningRoutine = null;
    }
}
