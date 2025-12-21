using Fusion;
using UnityEngine;

public class SyncMusic : NetworkBehaviour
{
    [SerializeField] private AudioSource audioSource;

    // Shared start tick for the whole session
    [Networked]
    private int StartTick { get; set; }

    private bool _started;

    private void Awake()
    {
        // Try to auto-grab AudioSource if not set in Inspector
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource != null)
            audioSource.playOnAwake = false;   // we control it manually
    }

    public override void Spawned()
    {
        Debug.Log($"[SyncMusic] Spawned. HasStateAuthority={Object.HasStateAuthority}, Runner={Runner}");

        // Only state authority decides the global start time
        if (Object.HasStateAuthority && StartTick == 0)
        {
            StartTick = Runner.Tick;
            Debug.Log($"[SyncMusic] StateAuthority sets StartTick={StartTick}");
        }

        // Late joiners: if StartTick already exists, align immediately
        if (StartTick != 0)
        {
            AlignAndPlay();
        }
    }

    public override void FixedUpdateNetwork()
    {
        // Wait until StartTick has been replicated
        if (!_started)
        {
            if (StartTick != 0)
                AlignAndPlay();

            return;
        }

        // Small drift correction while playing
        if (audioSource != null && audioSource.clip != null)
        {
            float targetTime = GetClipTimeFromTicks();
            if (Mathf.Abs(audioSource.time - targetTime) > 0.1f)
            {
                audioSource.time = targetTime;
            }
        }
    }

    private void AlignAndPlay()
    {
        if (Runner == null || audioSource == null || audioSource.clip == null || StartTick == 0)
        {
            Debug.Log($"[SyncMusic] AlignAndPlay aborted. " +
                      $"Runner={Runner}, audioSource={audioSource}, " +
                      $"clip={audioSource?.clip}, StartTick={StartTick}");
            return;
        }

        float t = GetClipTimeFromTicks();
        audioSource.time = Mathf.Clamp(t, 0f, audioSource.clip.length);

        if (!audioSource.isPlaying)
            audioSource.Play();

        _started = true;
        Debug.Log($"[SyncMusic] Playing at {audioSource.time:0.00}s " +
                  $"(Tick={Runner.Tick}, StartTick={StartTick})");
    }

    private float GetClipTimeFromTicks()
    {
        int dt = Runner.Tick - StartTick;
        float elapsed = dt * Runner.DeltaTime;      // seconds since global start
        float len = audioSource.clip.length;
        if (len <= 0f) return 0f;
        return elapsed % len;                       // loop
    }
}
