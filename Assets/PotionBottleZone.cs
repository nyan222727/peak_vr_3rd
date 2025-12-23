using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PotionBottleZone : MonoBehaviour
{
    [SerializeField] private bool verboseLogs = true;

    private void OnTriggerEnter(Collider other)
    {
        //var twig = other.GetComponentInParent<PotionTwig>();
        var twig = other.GetComponentInParent<LocalPotionTwig>();
        if (twig == null) return;

        twig.StartDip();
        if (verboseLogs) Debug.Log($"[PotionBottleZone] ENTER by {twig.name}", this);
    }

    private void OnTriggerStay(Collider other)
    {
        //var twig = other.GetComponentInParent<PotionTwig>();
        var twig = other.GetComponentInParent<LocalPotionTwig>();
        if (twig == null) return;

        // makes dip robust even if Enter was missed
        twig.StartDip();
    }

    private void OnTriggerExit(Collider other)
    {
        //var twig = other.GetComponentInParent<PotionTwig>();
        var twig = other.GetComponentInParent<LocalPotionTwig>();
        if (twig == null) return;

        twig.FinishDip();
        if (verboseLogs) Debug.Log($"[PotionBottleZone] EXIT by {twig.name}", this);
    }
}
