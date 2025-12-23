using UnityEngine;

[CreateAssetMenu(menuName = "Audio/Footstep Library")]
public class FootstepLibrary : ScriptableObject
{
    [System.Serializable]
    public class SurfaceClips
    {
        public FootstepSurface surface = FootstepSurface.Default;
        public AudioClip[] clips;

        [Header("Per-surface tuning")]
        [Range(0f, 2f)] public float volume = 1f;
        public float pitchMin = 0.95f;
        public float pitchMax = 1.05f;
    }

    public SurfaceClips[] surfaces;

    public bool TryGet(FootstepSurface s, out SurfaceClips result)
    {
        if (surfaces != null)
        {
            for (int i = 0; i < surfaces.Length; i++)
            {
                if (surfaces[i] != null && surfaces[i].surface == s)
                {
                    result = surfaces[i];
                    return true;
                }
            }
        }
        result = null;
        return false;
    }
}
