using UnityEngine;
using UnityEngine.VFX;

public class VfxPlayer : MonoBehaviour
{
    public static VfxPlayer Instance { get; private set; }

    [SerializeField] private VisualEffect vfx;

    private void Awake()
    {
        Instance = this;

        if (vfx == null)
            vfx = GetComponent<VisualEffect>();
    }

    public void PlayBurst()
    {
        if (vfx == null) return;

        // Re-trigger one-shot burst reliably
        vfx.Stop();
        vfx.Reinit();
        vfx.Play();
    }
}

