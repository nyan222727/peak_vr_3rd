using System.Collections;
using Fusion;
using UnityEngine;
using UnityEngine.VFX;

/// <summary>
/// Controls the burning sequence of the Talisman notebook.
/// Attach to the Talisman root. Requires colliders + Rigidbody to detect collisions.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class NotebookController : NetworkBehaviour
{
    [Header("VFX References")]
    public VisualEffect burn;     // Burn VFX
    public VisualEffect smoke;    // Smoke (center)
    public VisualEffect smoke2;
    public VisualEffect smoke3;
    public VisualEffect smoke4;
    public VisualEffect smoke5;

    [Header("Notebook visuals")]
    public MeshRenderer notebookRenderer; // the visible mesh of the talisman

    [Header("Burn graph property names")]
    public string burnIntensityProperty   = "BurnIntensity";
    public string fireSparkIntensityProp  = "FireSparkIntensity";
    public string burnDissolveProperty    = "DissolveAmount";

    [Header("Smoke graph property names")]
    public string smokeIntensityProperty  = "SmokeIntensity"; // on 'Smoke' VFX

    [Header("Tunable timings (seconds)")]
    public float delaySmoke3          = 1.5f;
    public float delaySmoke5After3    = 0.5f;
    public float burnSparkHoldTime    = 2.0f;   // BurnIntensity = 0, FireSpark = default
    public float burnIntensityRampTime= 3.2f;   // 0 -> 8
    public float burnHoldAtMax        = 1f;   // hold at 8
    public float postSmoke2_5Hold     = 1.5f;   // after enabling Smoke5+Smoke2
    public float smokeIntensityRampTime = 1.0f; // SmokeInt -> 100
    public float dissolveTo02Time     = 1.0f;   // DissolveAmount -> 0.2
    public float waitAfter02          = 1.0f;   // step 5 pre-unique move
    public float dissolveTo05Time     = 0.5f;   // DissolveAmount -> 0.5
    public float waitAfter05          = 0.1f;
    //public float stopOtherSmokesDelay = 0.1f;
    public float finalDissolveTime    = 3f;   // 0.5 -> 1

    [Header("Burn intensities")]
    public float burnIntensityMax     = 8f;

    [Header("Cleanup")]
    public float destroyDelayAfterBurn = 1.0f;   // how long after burn end before despawn

    [Header("Debug")]
    [SerializeField] private bool autoTestBurnOnStart = false;

    [SerializeField] public GameObject startGame;


    private bool _sequenceStarted;
    private bool _sequenceFinished;

    private void Start()
    {
        if (autoTestBurnOnStart)
        {
            StartBurnSequence();
        }
    }

    #region Collision trigger with lit candle

    // Use collision (not trigger) so we don't have to change colliders to triggers.
    private void OnCollisionEnter(Collision collision)
    {
        if (_sequenceStarted || _sequenceFinished)
            return;

        LocalCandle localCandle = collision.collider.GetComponentInParent<LocalCandle>();
        if(localCandle!=null)
        {
            var candleRoot = collision.collider.GetComponentInParent<Transform>();
            if(candleRoot.CompareTag("Candle") && localCandle.IsLit)
                StartBurnSequence();
        }
        
        // Look for a NetworkCandle in the colliding object or its parents
        NetworkCandle candle = collision.collider.GetComponentInParent<NetworkCandle>();
        if (candle == null)
            return;

        if (!candle.IsLit)
            return;

        // Candle is lit, Talisman touched it: begin burning
        StartBurnSequence();
    }

    #endregion

    /// <summary>
    /// Can also be called manually (button / debug).
    /// </summary>
    public void StartBurnSequence()
    {
        if (_sequenceStarted || _sequenceFinished)
            return;

        _sequenceStarted = true;
        StartCoroutine(BurnSequenceCoroutine());
    }

    private IEnumerator BurnSequenceCoroutine()
    {
        // Safety: ensure renderer + VFX references exist
        if (notebookRenderer == null)
            notebookRenderer = GetComponentInChildren<MeshRenderer>();

        yield return new WaitForSeconds(0.5f);

        // Step 1: play Smoke, then Smoke3, then Smoke5
        if (smoke != null)
            smoke.Play();

        yield return new WaitForSeconds(delaySmoke3);

        if (smoke3 != null)
            smoke3.Play();

        yield return new WaitForSeconds(delaySmoke5After3);

        if (smoke5 != null)
            smoke5.Play();

        // Step 2: Burn starts with BurnIntensity = 0, FireSparkIntensity default
        if (burn != null)
        {
            burn.SetFloat(burnIntensityProperty, 0f);
            burn.Play();
            
        }

        // Hold sparks only
        yield return new WaitForSeconds(burnSparkHoldTime);

        // Ramp BurnIntensity 0 -> burnIntensityMax over burnIntensityRampTime
        if (burn != null && burnIntensityRampTime > 0f)
        {
            float t = 0f;
            while (t < burnIntensityRampTime)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / burnIntensityRampTime);
                float value = Mathf.Lerp(0f, burnIntensityMax, k);
                burn.SetFloat(burnIntensityProperty, value);
                yield return null;
            }
            burn.SetFloat(burnIntensityProperty, burnIntensityMax);
        }

        // Step 3: Hold at max, then enable Smoke5 + Smoke2, then wait again
        yield return new WaitForSeconds(burnHoldAtMax);

        if (smoke5 != null) smoke5.Play(); // harmless even if already playing
        if (smoke2 != null) smoke2.Play();

        yield return new WaitForSeconds(postSmoke2_5Hold);

        // Step 4: Over 1s: SmokeIntensity -> 100, DissolveAmount -> 0.2
        float startSmokeIntensity = 1f;
        if (smoke != null)
        {
            if (smoke.HasFloat(smokeIntensityProperty))
                startSmokeIntensity = smoke.GetFloat(smokeIntensityProperty);
        }

        float startDissolve = 0f;
        if (burn != null && burn.HasFloat(burnDissolveProperty))
            startDissolve = burn.GetFloat(burnDissolveProperty);

        float targetSmokeIntensity = 50f;
        float targetDissolve02 = 0.25f;

        float t4 = 0f;
        while (t4 < smokeIntensityRampTime)
        {
            t4 += Time.deltaTime;
            float k = Mathf.Clamp01(t4 / smokeIntensityRampTime);

            float s = Mathf.Lerp(startSmokeIntensity, targetSmokeIntensity, k);
            float d = Mathf.Lerp(startDissolve, targetDissolve02, k);

            if (smoke != null)
                smoke.SetFloat(smokeIntensityProperty, s);

            if (burn != null)
                burn.SetFloat(burnDissolveProperty, d);

            yield return null;
        }

        if (burn != null)
            burn.SetFloat(burnDissolveProperty, targetDissolve02);

        // Step 5 (first part): wait, then DissolveAmount -> 0.5
        yield return new WaitForSeconds(waitAfter02);

        if (smoke2 != null) smoke2.Stop();
        if (smoke3 != null) smoke3.Stop();
        if (smoke4 != null) smoke4.Stop();
        if (smoke5 != null) smoke5.Stop();

        float targetDissolve05 = 0.5f;
        float t5 = 0f;
        float startD05 = targetDissolve02;

        while (t5 < dissolveTo05Time)
        {
            t5 += Time.deltaTime;
            float k = Mathf.Clamp01(t5 / dissolveTo05Time);
            float d = Mathf.Lerp(startD05, targetDissolve05, k);

            if (burn != null)
                burn.SetFloat(burnDissolveProperty, d);
            if(t5>(0.5f*dissolveTo05Time))
            {
                if (smoke != null) smoke.Stop();
                if (notebookRenderer != null)
                    notebookRenderer.enabled = false;
            }

            yield return null;
        }

        if (burn != null)
            burn.SetFloat(burnDissolveProperty, targetDissolve05);

        

        // Wait 0.3s, then stop Smoke2..5
        //yield return new WaitForSeconds(waitAfter05);

        //if (smoke2 != null) smoke2.Stop();
        //if (smoke3 != null) smoke3.Stop();
        //if (smoke4 != null) smoke4.Stop();
        //if (smoke5 != null) smoke5.Stop();

        // After 0.1s, stop main Smoke and disable mesh renderer (fake dissolve)
        //yield return new WaitForSeconds(stopOtherSmokesDelay);

        //if (smoke != null) smoke.Stop();
        //if (notebookRenderer != null)
            //notebookRenderer.enabled = false;

        
        // Step 6: over 0.5s, DissolveAmount -> 1, then finish
        float t6 = 0f;
        float startDissolveFinal = targetDissolve05;
        float targetDissolve1 = 1f;

        while (t6 < finalDissolveTime)
        {
            t6 += Time.deltaTime;
            float k = Mathf.Clamp01(t6 / finalDissolveTime);
            float d = Mathf.Lerp(startDissolveFinal, targetDissolve1, k);

            if (burn != null)
                burn.SetFloat(burnDissolveProperty, d);

            yield return null;
        }

        if (burn != null)
            burn.SetFloat(burnDissolveProperty, targetDissolve1);

        // Optionally stop burn VFX completely or leave a tiny ember:
        if (burn != null)
            burn.Stop();

        _sequenceFinished = true;

        // === Trigger global GhostLock ===
        if (GhostGlobalState.Instance != null)
        {
            //GhostGlobalState.Instance.SetGhostLock(true);
            GhostGlobalState.Instance.GhostLockActivation(30f);
        }

        // Wait a bit, then destroy / despawn the Talisman over the network
        if (destroyDelayAfterBurn > 0f)
            yield return new WaitForSeconds(destroyDelayAfterBurn);
        
        var controller = startGame.GetComponent<StartGameController>();
        if (controller != null)
            controller.StartGame = true;

        // Only the state authority should despawn the NetworkObject
        if (Object != null && Object.HasStateAuthority)
        {
            // Despawn the networked Talisman so all clients see it disappear
            Runner.Despawn(Object);
        }

    }
}
