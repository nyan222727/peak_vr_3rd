using System;
using System.Collections;
using UnityEngine;

public class EnergySystem : MonoBehaviour
{
    [Header("Energy")]
    public float maxEnergy = 100f;
    public float currentEnergy = 100f;

    [Tooltip("每秒恢復 maxEnergy 的百分比。例如 0.05 = 5%/sec")]
    public float regenPercentPerSecond = 0.05f;

    public bool skillLocked { get; private set; } = false;   // 封印時：不能放技能
    public bool regenLocked { get; private set; } = false;   // 封印時：不回能量

    public event Action<float, float> OnEnergyChanged;        // (current, max)
    public event Action<bool> OnSkillLockChanged;             // skillLocked

    Coroutine sealRoutine;

    void Awake()
    {
        currentEnergy = Mathf.Clamp(currentEnergy, 0, maxEnergy);
        NotifyEnergy();
    }

    void Update()
    {
        // 封印時不回能量
        if (regenLocked) return;

        // 每秒回 5%（maxEnergy * 0.05 = 5/秒）
        if (currentEnergy < maxEnergy)
        {
            float regenPerSecond = maxEnergy * regenPercentPerSecond;
            currentEnergy = Mathf.Min(maxEnergy, currentEnergy + regenPerSecond * Time.deltaTime);
            NotifyEnergy();
        }
    }

    public bool CanUseSkill(float cost)
    {
        if (skillLocked) return false;
        return currentEnergy >= cost;
    }

    public bool TryConsume(float cost)
    {
        if (!CanUseSkill(cost)) return false;

        currentEnergy = Mathf.Max(0, currentEnergy - cost);
        NotifyEnergy();
        return true;
    }

    public void SealForSeconds(float seconds)
    {
        if (sealRoutine != null) StopCoroutine(sealRoutine);
        sealRoutine = StartCoroutine(SealRoutine(seconds));
    }

    IEnumerator SealRoutine(float seconds)
    {
        SetLocked(true);
        yield return new WaitForSeconds(seconds);
        SetLocked(false);
        sealRoutine = null;
    }

    void SetLocked(bool locked)
    {
        skillLocked = locked;
        regenLocked = locked;
        OnSkillLockChanged?.Invoke(skillLocked);
    }

    void NotifyEnergy()
    {
        OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
    }
}
