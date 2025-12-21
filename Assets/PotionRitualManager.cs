using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Fusion;
using Fusion.Sockets;
using System.Threading.Tasks;

public class PotionRitualManager : NetworkBehaviour
{
    public static PotionRitualManager Instance { get; private set; }

    //[Header("Debug")]
    [Networked] public bool Region0Done { get; set; }
    [Networked] public bool Region1Done { get; set; }
    [Networked] public bool Region2Done { get; set; }

    // This is the global flag you wanted (public and networked)
    [Networked] public bool AllRegionsCompleted { get; set; }

    [SerializeField] private bool verboseLogs = true;
    private void Log(string msg) { if (verboseLogs) Debug.Log($"[PotionRitualManager] {msg}", this); }


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Called from PotionTwig when a valid splat happens in a region.
    /// </summary>
    public void RegisterSplat(int regionIndex)
    {
        Log($"RegisterSplat({regionIndex}) called. Authority={Object.HasStateAuthority}");
        if (!Object.HasStateAuthority)
            return; // only authority updates shared state

        switch (regionIndex)
        {
            case 0:
                Region0Done = true;
                break;
            case 1:
                Region1Done = true;
                break;
            case 2:
                Region2Done = true;
                break;
            default:
                return;
        }
        Log($"State now: R0={Region0Done} R1={Region1Done} R2={Region2Done}");


        // If all three regions were splatted at least once, raise global flag
        if (!AllRegionsCompleted && Region0Done && Region1Done && Region2Done)
        {
            AllRegionsCompleted = true;
            Log("AllRegionsCompleted = TRUE");
            // later you can trigger sound / VFX or notify other systems here
        }
    }
}
