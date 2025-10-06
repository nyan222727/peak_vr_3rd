using UnityEngine;

public class LandingInjuryManager : MonoBehaviour
{
    [Header("Post Processing")]
    public UnityEngine.Rendering.Volume vignetteVolume;
    private UnityEngine.Rendering.Universal.Vignette vignette;
    public float vignetteMaxIntensity = 0.6f;
    public float vignetteFadeSpeed = 0.3f;

    [Header("References")]
    public PlayerLocomotion locomotion; // 從 PlayerLocomotion 擷取落地狀態
    public ClimbStaminaManager staminaManager; // 受傷管理
    public AudioClip injurySound;
    public float injuryThreshold = 8f; // 超過此速度才受傷
    public float injuryAmount = 20f;   // 受傷值
    [SerializeField]
    private AudioSource audioSource;
    private bool wasGrounded = true;
    private float lastYVelocity = 0f;
    public bool isGrounded = true;

    void Start()
    {
        if (vignetteVolume)
        {
            vignetteVolume.profile.TryGet(out vignette);
        }
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    void Update()
    {
        // 根據受傷值調整 Vignette 效果
        UpdateVignetteEffect();
        if (locomotion == null || staminaManager == null) return;
        // 取得目前是否在地面
        isGrounded = CheckGrounded();
        float currentYVelocity = locomotion.GetComponent<Rigidbody>().velocity.y;

        // 檢查落地
        if (!wasGrounded && isGrounded)
        {
            float fallSpeed = Mathf.Abs(lastYVelocity);
            if (fallSpeed > injuryThreshold)
            {
                // 造成受傷
                staminaManager.AddInjury(injuryAmount);
                // 播放受傷音效
                if (injurySound)
                {
                    audioSource.PlayOneShot(injurySound);
                }
            }
        }
        // 根據受傷值調整 Vignette 效果，受傷未滿最大體力時自動淡出
        void UpdateVignetteEffect()
        {
            if (vignette == null || staminaManager == null) return;
            float injuryValue = staminaManager.injurySlider ? staminaManager.injurySlider.value : 0f;
            float maxStamina = staminaManager.maxStamina;
            float targetIntensity = vignetteMaxIntensity * Mathf.Clamp01(injuryValue / maxStamina);

            // 若受傷未佔滿體力條，則慢慢淡出
            if (injuryValue < maxStamina)
            {
                vignette.intensity.value = Mathf.MoveTowards(vignette.intensity.value, 0f, vignetteFadeSpeed * Time.deltaTime);
            }
            else
            {
                vignette.intensity.value = targetIntensity;
            }
            // 若受傷值大於 0，則根據比例顯示
            if (injuryValue > 0f && injuryValue < maxStamina)
            {
                float showIntensity = Mathf.Max(vignette.intensity.value, targetIntensity);
                vignette.intensity.value = showIntensity;
            }
        }
        wasGrounded = isGrounded;
        lastYVelocity = currentYVelocity;
    }

    // 從 PlayerLocomotion 擷取地面判斷
    bool CheckGrounded()
    {
        // 你可以在 PlayerLocomotion 加一個 public bool isGrounded
        // 這裡假設有此欄位，否則可用 Raycast 判斷
        return locomotion.groundCheck ? Physics.Raycast(locomotion.groundCheck.position, Vector3.down, locomotion.groundCheckDistance + 0.1f, locomotion.groundMask) : false;
    }
}
