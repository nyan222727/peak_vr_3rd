using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ClimbStaminaManager : MonoBehaviour
{
    [Header("Slider References")]
    public Slider staminaSlider; // 主體力條（顯示可用最大體力）
    public Slider injurySlider;  // 受傷條（影響最大可用體力）
    public Slider currentStaminaSlider; // 顯示目前體力值

    [Header("Stamina Settings")]
    public float maxStamina = 100f;
    public float staminaDecreaseRate = 20f; // 攀爬時每秒消耗
    public float staminaRecoverRate = 15f;  // 恢復時每秒回復
    public float recoverDelay = 2f;         // 離開攀爬多久後才開始回復

    private float currentStamina;
    private float lastClimbTime;
    private bool isClimbing;

    void Start()
    {
        currentStamina = maxStamina;
        if (staminaSlider) staminaSlider.maxValue = maxStamina;
        if (injurySlider) injurySlider.maxValue = maxStamina;
        if (currentStaminaSlider) currentStaminaSlider.maxValue = maxStamina;
        UpdateSliders();
    }

    void Update()
    {
        float injuryValue = injurySlider ? injurySlider.value : 0f;
        float usableMaxStamina = maxStamina - injuryValue;
        usableMaxStamina = Mathf.Max(0f, usableMaxStamina);

        if (isClimbing)
        {
            currentStamina -= staminaDecreaseRate * Time.deltaTime;
            lastClimbTime = Time.time;
        }
        else if (Time.time - lastClimbTime > recoverDelay)
        {
            currentStamina += staminaRecoverRate * Time.deltaTime;
        }

        currentStamina = Mathf.Clamp(currentStamina, 0f, usableMaxStamina);
        UpdateSliders();
    }

    public void SetClimbing(bool climbing)
    {
        isClimbing = climbing;
        if (climbing) lastClimbTime = Time.time;
    }

    // 增加受傷（累加）
    public void AddInjury(float damage)
    {
        if (injurySlider)
        {
            float newValue = injurySlider.value + damage;
            injurySlider.value = Mathf.Clamp(newValue, 0f, maxStamina);
        }
    }

    // 治療（減少受傷）
    public void HealInjury(float healAmount)
    {
        if (injurySlider)
        {
            float newValue = injurySlider.value - healAmount;
            injurySlider.value = Mathf.Clamp(newValue, 0f, maxStamina);
        }
    }

    public void SetStamina(float value)
    {
        currentStamina = Mathf.Clamp(value, 0f, maxStamina);
        UpdateSliders();
    }

    private void UpdateSliders()
    {
        if (staminaSlider)
        {
            float injuryValue = injurySlider ? injurySlider.value : 0f;
            float usableMaxStamina = maxStamina - injuryValue;
            staminaSlider.value = usableMaxStamina;
        }
        if (currentStaminaSlider) currentStaminaSlider.value = currentStamina;
    }

    public float GetStaminaPercent()
    {
        float injuryValue = injurySlider ? injurySlider.value : 0f;
        float usableMaxStamina = maxStamina - injuryValue;
        if (usableMaxStamina <= 0f) return 0f;
        return currentStamina / usableMaxStamina;
    }
}
