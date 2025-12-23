using UnityEngine;

public enum FootstepSurface
{
    Default,
    Wood,
    Stone,
    Carpet,
    Metal,
    Wood2,
    Paper
}

public class FootstepSurfaceTag : MonoBehaviour
{
    public FootstepSurface surface = FootstepSurface.Default;
}
