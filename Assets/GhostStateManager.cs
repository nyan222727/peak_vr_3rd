using Fusion;
using UnityEngine;
using System.Collections;

/// <summary>
/// Global networked flags for the horror logic.
/// Single instance in the scene; access via GhostGlobalState.Instance.
/// </summary>
[DisallowMultipleComponent]
public class GhostGlobalState : NetworkBehaviour
{
    public static GhostGlobalState Instance { get; private set; }

    [Networked] public NetworkBool GhostLock { get; private set; }

    private Coroutine ghostLockRoutine;

    public void GhostLockActivation(float duration)
    {
        // Only host / state authority should drive the timer
        if (!Object.HasStateAuthority)
        {
            Object.RequestStateAuthority();
        // we still continue; SetGhostLock() handles authority too
        }

        // Restart timer if already running
        if (ghostLockRoutine != null)
        {
            StopCoroutine(ghostLockRoutine);
        }

        ghostLockRoutine = StartCoroutine(GhostLockRoutine(duration));
    }

    private IEnumerator GhostLockRoutine(float duration)
    {
        // false -> true
        SetGhostLock(true);

        yield return new WaitForSeconds(duration);

        // back to false
        SetGhostLock(false);
        ghostLockRoutine = null;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public override void Spawned()
    {
        // Ensure starting value is false on host
        if (Object.HasStateAuthority)
        {
            GhostLock = false;
        }
    }

    /// <summary>
    /// Host / StateAuthority sets GhostLock; request authority if needed.
    /// </summary>
    public void SetGhostLock(bool value)
    {
        if (!Object.HasStateAuthority)
        {
            Object.RequestStateAuthority();
        }

        GhostLock = value;
    }

    // Convenience read-only static for other scripts
    public static bool IsGhostLocked => Instance != null && Instance.GhostLock;
}
