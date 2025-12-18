using UnityEngine;
using UnityEngine.UI;

public class PhaseProgressBarUI : MonoBehaviour
{
    public PhaseController phaseController; // 拖你的 PhaseController 進來
    public Slider slider;                   // 拖 Slider 進來
    public Text phaseText;                  // 顯示「Phase 1/5」
    public Text timeText;                   // 顯示剩餘時間 mm:ss

    // 可選：根據進度改顏色
    public Image fillImage;                 // Slider Fill 這顆 Image
    public Gradient colorOverPhase;         // 0~1 顏色變化（可不設）

    void Update()
    {
        if (phaseController == null || slider == null) return;
        if (!phaseController.IsRunning) return;

        // 0~1：這個階段內的進度
        float t = phaseController.CurrentPhaseNormalized;
        slider.value = t;

        // 顏色隨進度變化（可不設）
        if (fillImage != null && colorOverPhase != null)
        {
            fillImage.color = colorOverPhase.Evaluate(t);
        }

        // 階段文字：Phase X/Y
        if (phaseText != null)
        {
            int phaseIndex = Mathf.Max(0, phaseController.CurrentPhaseIndex) + 1; // 變成 1 起算
            int total = phaseController.phases.Length;
            phaseText.text = "Phase " + phaseIndex + "/" + total;
        }

        // 剩餘時間：mm:ss
        if (timeText != null)
        {
            float remain = Mathf.Max(0f, phaseController.CurrentPhaseTimeRemaining);
            int min = Mathf.FloorToInt(remain / 60f);
            int sec = Mathf.FloorToInt(remain % 60f);
            timeText.text = string.Format("{0:00}:{1:00}", min, sec);
        }
    }
}
