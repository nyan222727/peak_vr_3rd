using System;
using UnityEngine;
using UnityEngine.UI;

public class PhaseController : MonoBehaviour
{
    [Serializable]
    public class Phase
    {
        public string name = "Phase";
        public float duration = 120f; // 每階段秒數（預設 2 分鐘）
    }

    [Header("階段設定")]
    public Phase[] phases = new Phase[5];   // Inspector 裡可調整

    [Header("時間控制")]
    public float timeScale = 1f;            // 1 = 正常倒數, 2 = 兩倍速, -1 = 倒退

    public int CurrentPhaseIndex { get; private set; } = -1;  // -1 = 尚未開始
    public float CurrentPhaseTimeRemaining { get; private set; }  // 這階段剩餘時間
    public float CurrentPhaseDuration =>
        (CurrentPhaseIndex >= 0 && CurrentPhaseIndex < phases.Length) ?
        phases[CurrentPhaseIndex].duration : 0f;

    public float CurrentPhaseNormalized =>
        CurrentPhaseDuration > 0 ?
        Mathf.Clamp01(1f - CurrentPhaseTimeRemaining / CurrentPhaseDuration) : 0f;
    // 0 = 剛開始, 1 = 尾端

    public bool IsRunning { get; private set; } = false;

    // === 對外事件（其他系統可以訂閱） ===
    public event Action<int> OnPhaseStarted;         // 傳目前 phase index
    public event Action<int> OnPhaseEnded;           // 前一個 phase index
    public event Action OnAllPhasesCompleted;

    private void Start()
    {
        // 初始化 5 個階段都 120 秒
        if (phases == null || phases.Length == 0)
        {
            phases = new Phase[5];
        }

        for (int i = 0; i < phases.Length; i++)
        {
            if (phases[i] == null) phases[i] = new Phase();
            if (phases[i].duration <= 0f) phases[i].duration = 120f;
            if (string.IsNullOrEmpty(phases[i].name)) phases[i].name = $"Phase {i + 1}";
        }

        StartPhases();  // 你也可以改成外部手動呼叫
    }

    private void Update()
    {
        if (!IsRunning || CurrentPhaseIndex < 0) return;

        // 根據 timeScale 更新「虛擬時間」
        float delta = Time.deltaTime * timeScale;
        CurrentPhaseTimeRemaining -= delta;

        // 正向倒數：時間 <= 0 進下一階段
        if (timeScale > 0 && CurrentPhaseTimeRemaining <= 0f)
        {
            EndCurrentPhase();
            GoToNextPhase();
        }
        // 倒退：時間 >= 原本長度 → 回上一階段
        else if (timeScale < 0 && CurrentPhaseTimeRemaining >= CurrentPhaseDuration)
        {
            EndCurrentPhase();
            GoToPreviousPhase();
        }

        // 這邊可以加「依剩餘時間做事」的判斷
        // 例如：最後一階段最後一分鐘
        // if (CurrentPhaseIndex == phases.Length - 1 && CurrentPhaseTimeRemaining <= 60f)
        // {
        //     // 開最大污染、音樂加速等等
        // }
    }

    // ====== 公開 API ======

    public void StartPhases()
    {
        if (phases == null || phases.Length == 0)
        {
            Debug.LogError("PhaseController：沒有設定任何階段");
            return;
        }

        IsRunning = true;
        SetPhase(0);
    }

    public void Pause() => IsRunning = false;
    public void Resume() => IsRunning = true;

    public void SetTimeScale(float newScale)
    {
        timeScale = newScale;
    }

    public void AddTime(float seconds)
    {
        CurrentPhaseTimeRemaining += seconds;
    }

    public void JumpToPhase(int index, float? timeRemaining = null)
    {
        if (index < 0 || index >= phases.Length)
        {
            Debug.LogWarning("PhaseController：JumpToPhase index 超出範圍");
            return;
        }

        SetPhase(index);

        if (timeRemaining.HasValue)
        {
            CurrentPhaseTimeRemaining = Mathf.Clamp(
                timeRemaining.Value, 0f, CurrentPhaseDuration);
        }
    }

    // ====== 內部處理 ======

    private void SetPhase(int index)
    {
        CurrentPhaseIndex = index;
        CurrentPhaseTimeRemaining = phases[index].duration;

        OnPhaseStarted?.Invoke(CurrentPhaseIndex);
        Debug.Log($"[PhaseController] Start phase {index + 1}: {phases[index].name}");
    }

    private void EndCurrentPhase()
    {
        if (CurrentPhaseIndex >= 0)
            OnPhaseEnded?.Invoke(CurrentPhaseIndex);
    }

    private void GoToNextPhase()
    {
        int next = CurrentPhaseIndex + 1;
        if (next >= phases.Length)
        {
            Debug.Log("[PhaseController] All phases completed.");
            IsRunning = false;
            OnAllPhasesCompleted?.Invoke();
        }
        else
        {
            SetPhase(next);
        }
    }

    private void GoToPreviousPhase()
    {
        int prev = CurrentPhaseIndex - 1;
        if (prev < 0)
        {
            // 回到第一階段開頭
            SetPhase(0);
        }
        else
        {
            // 直接回上一階段尾端
            CurrentPhaseIndex = prev;
            CurrentPhaseTimeRemaining = phases[prev].duration;
            OnPhaseStarted?.Invoke(CurrentPhaseIndex);
        }
    }
}
