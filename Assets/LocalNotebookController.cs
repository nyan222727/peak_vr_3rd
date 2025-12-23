using System.Collections;
using UnityEngine;
using UnityEngine.VFX;

/// <summary>
/// Local version of NotebookController (no Fusion).
/// Controls the burning sequence of the Talisman notebook.
/// Attach to the Talisman root. Requires colliders + Rigidbody to detect collisions.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class LocalNotebookController : MonoBehaviour
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
    public string burnIntensityProperty    = "BurnIntensity";
    public string fireSparkIntensityProp   = "FireSparkIntensity";
    public string burnDissolveProperty     = "DissolveAmount";

    [Header("Smoke graph property names")]
    public string smokeIntensityProperty   = "SmokeIntensity"; // on 'Smoke' VFX

    [Header("Tunable timings (seconds)")]
    public float delaySmoke3           = 1.5f;
    public float delaySmoke5After3     = 0.5f;
    public float burnSparkHoldTime     = 2.0f;   // BurnIntensity = 0, FireSpark = default
    public float burnIntensityRampTime = 3.2f;   // 0 -> 8
    public float burnHoldAtMax         = 1f;     // hold at 8
    public float postSmoke2_5Hold      = 1.5f;   // after enabling Smoke5+Smoke2
    public float smokeIntensityRampTime= 1.0f;   // SmokeInt -> 100
    public float dissolveTo02Time      = 1.0f;   // DissolveAmount -> 0.2
    public float waitAfter02           = 1.0f;   // step 5 pre-unique move
    public float dissolveTo05Time      = 0.5f;   // DissolveAmount -> 0.5
    public float waitAfter05           = 0.1f;
    public float finalDissolveTime     = 3f;     // 0.5 -> 1

    [Header("Burn intensities")]
    public float burnIntensityMax      = 8f;

    [Header("Cleanup")]
    public float destroyDelayAfterBurn = 1.0f;   // how long after burn end before Destroy()

    [Header("Audio (optional)")]
    public AudioClip burnStartSfx;
    [Range(0f, 1f)] public float burnStartSfxVolume = 1f;

    // Optional: assign a specific AudioSource (can be on a child). If null, it will auto-find/create one.
    public AudioSource burnStartSfxSource;

    // If we auto-create an AudioSource, make it 3D so it comes from the notebook.
    public bool burnStartSfxSpatial3D = true;

    [Header("Debug")]
    [SerializeField] private bool autoTestBurnOnStart = false;

    [SerializeField] public GameObject startGame;

    private bool _sequenceStarted;
    private bool _sequenceFinished;

    private void Start()
    {
        if (autoTestBurnOnStart)
            StartBurnSequence();
    }

    // Use collision (not trigger) so we don't have to change colliders to triggers.
    private void OnCollisionEnter(Collision collision)
    {
        if (_sequenceStarted || _sequenceFinished)
            return;

        // Local candle path (preferred for local mode)
        var localCandle = collision.collider.GetComponentInParent<LocalCandle>();
        if (localCandle != null)
        {
            var candleRoot = collision.collider.GetComponentInParent<Transform>();
            if (candleRoot != null && candleRoot.CompareTag("Candle") && localCandle.IsLit)
            {
                StartBurnSequence();
                return;
            }
        }

        // Optional compatibility: if a NetworkCandle exists in project, detect it WITHOUT a hard reference.
        // This avoids requiring Fusion in this local script.
        var mb = collision.collider.GetComponentInParent<MonoBehaviour>();
        if (mb != null && mb.GetType().Name == "NetworkCandle")
        {
            var isLitProp = mb.GetType().GetProperty("IsLit");
            if (isLitProp != null && isLitProp.PropertyType == typeof(bool))
            {
                bool isLit = (bool)isLitProp.GetValue(mb, null);
                if (isLit)
                {
                    StartBurnSequence();
                    return;
                }
            }
        }
    }

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

        // Play burn start SFX (local)
        if (burnStartSfx != null)
        {
            var src = burnStartSfxSource;

            if (src == null)
            {
                src = GetComponent<AudioSource>();
                if (src == null)
                    src = gameObject.AddComponent<AudioSource>();

                burnStartSfxSource = src; // cache it
                src.playOnAwake = false;
                if (burnStartSfxSpatial3D) src.spatialBlend = 1f; // 3D
            }

            src.PlayOneShot(burnStartSfx, burnStartSfxVolume);
        }

        yield return new WaitForSeconds(delaySmoke3);

        if (smoke3 != null)
            smoke3.Play();

        yield return new WaitForSeconds(delaySmoke5After3);

        if (smoke5 != null)
            smoke5.Play();

        // Step 2: Burn starts with BurnIntensity = 0
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

        if (smoke5 != null) smoke5.Play();
        if (smoke2 != null) smoke2.Play();

        yield return new WaitForSeconds(postSmoke2_5Hold);

        // Step 4: ramp SmokeIntensity + Dissolve
        float startSmokeIntensity = 1f;
        if (smoke != null && smoke.HasFloat(smokeIntensityProperty))
            startSmokeIntensity = smoke.GetFloat(smokeIntensityProperty);

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

        // Step 5: wait, stop other smokes, dissolve to 0.5
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

            if (t5 > (0.5f * dissolveTo05Time))
            {
                if (smoke != null) smoke.Stop();
                if (notebookRenderer != null)
                    notebookRenderer.enabled = false;
            }

            yield return null;
        }

        if (burn != null)
            burn.SetFloat(burnDissolveProperty, targetDissolve05);

        // Step 6: final dissolve to 1
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

        if (burn != null)
            burn.Stop();

        _sequenceFinished = true;

        // === Trigger global GhostLock (same as your original) ===
        if (GhostGlobalState.Instance != null)
        {
            GhostGlobalState.Instance.GhostLockActivation(30f);
        }

        if (destroyDelayAfterBurn > 0f)
            yield return new WaitForSeconds(destroyDelayAfterBurn);

        // Start game flag (same as original)
        if (startGame != null)
        {
            var controller = startGame.GetComponent<StartGameController>();
            if (controller != null)
                controller.StartGame = true;
        }

        // Local cleanup (replace Runner.Despawn)
        Destroy(gameObject);
    }
}
