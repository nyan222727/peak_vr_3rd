using Fusion;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class FireEffectNetwork : NetworkBehaviour
{
    [SerializeField] private ParticleSystem particleSystem;
    [SerializeField] private AudioSource audioSource;

    public override void Spawned()
    {
        // Auto-cache if not assigned
        if (particleSystem == null)
            particleSystem = GetComponentInChildren<ParticleSystem>(true);

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        // Play VFX
        if (particleSystem != null)
            particleSystem.Play();

        // Play SFX
        if (audioSource != null)
            audioSource.Play();
    }
}

