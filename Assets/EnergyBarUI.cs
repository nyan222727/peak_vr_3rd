using UnityEngine;
using UnityEngine.UI;

public class EnergyBarUI : MonoBehaviour
{
    public EnergySystem energy;
    public Slider slider;              // UGUI Slider
    public Image sealOverlay;          // 半透明圖 (Image)

    // 可選：根據進度改顏色
    public Image fillImage;                 // Slider Fill 這顆 Image
    public Gradient colorOverPhase;         // 0~1 顏色變化（可不設）

    public Text energyText;

    void Start()
    {
        if (energy == null) energy = FindObjectOfType<EnergySystem>();

        slider.minValue = 0f;
        slider.maxValue = energy.maxEnergy;
        slider.value = energy.currentEnergy;

        if (sealOverlay != null) sealOverlay.gameObject.SetActive(false);


        // 初始化顯示
        UpdateText(energy.currentEnergy, energy.maxEnergy);

        // 訂閱能量變化事件
        energy.OnEnergyChanged += UpdateText;
        energy.OnEnergyChanged += HandleEnergyChanged;
        energy.OnSkillLockChanged += HandleSkillLockChanged;
    }

    void Update()
    {
        float t = energy.currentEnergy / energy.maxEnergy;
        // 顏色隨進度變化（可不設）
        if (fillImage != null && colorOverPhase != null)
        {
            fillImage.color = colorOverPhase.Evaluate(t);
        }
    }

    void OnDestroy()
    {
        if (energy == null) return;
        energy.OnEnergyChanged -= HandleEnergyChanged;
        energy.OnSkillLockChanged -= HandleSkillLockChanged;
    }

    void HandleEnergyChanged(float current, float max)
    {
        slider.maxValue = max;
        slider.value = current;
    }

    void HandleSkillLockChanged(bool locked)
    {
        if (sealOverlay != null)
            sealOverlay.gameObject.SetActive(locked);
    }

    void UpdateText(float current, float max)
    {
        // 整數顯示，看起來比較乾淨
        energyText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
    }
}
