using System.Collections;
using UnityEngine;

public class UI_Manager : MonoBehaviour
{
    public GameObject[] UI_BT_objs;
    public GameObject Tips_objs;
    public GameObject Joy_stick_obj;
    public GameObject Bar_Time_obj;
    public GameObject Bar_Energy_obj;
    public GameObject Render_Table_image_obj;

    [Header("Menu Fade (CanvasGroup on MenuRoot)")]
    public CanvasGroup MenuCanvasGroup;   // 只控制「初始BT那一組」或 MenuRoot
    public CanvasGroup BasicCanvasGroup;   // 只控制「初始BT那一組」或 MenuRoot
    public float fadeSeconds = 1.0f;
    public float afterFadeDelay = 0.0f;

    public enum UIState { Menu, Transition, Table, End }
    public UIState state = UIState.Menu;

    bool isTransitioning = false;


    [Header("Ending UI")]
    public GameObject EndingUIRoot;          // 結束UI根物件（整包）
    public GameObject GoodTextObj;           // 好結局字物件
    public GameObject BadTextObj;            // 壞結局字物件
    public CanvasGroup EndingCanvasGroup;    // 結束UI上的 CanvasGroup（你說的 GC）
    public float endingFadeInSeconds = 1.0f;
    public float DelayFadeInSeconds = 1.0f;

    bool endingPlaying = false;


    public void SetUIActives(int n)
    {
        switch (n)
        {
            case 0: // 初始UI設置 只有BT
                foreach (var bt in UI_BT_objs) bt.SetActive(true);
                Joy_stick_obj.SetActive(false);
                Render_Table_image_obj.SetActive(false);
                Bar_Time_obj.SetActive(true);
                Bar_Energy_obj.SetActive(true);

                state = UIState.Menu;
                ApplyMenuVisual(true);   // 確保 menu 可見可點
                break;

            case 1: // 進入碟仙UI
                foreach (var bt in UI_BT_objs) bt.SetActive(false);
                Joy_stick_obj.SetActive(true);
                Render_Table_image_obj.SetActive(true);
                state = UIState.Table;
                break;
            case 2: // End
                foreach (var bt in UI_BT_objs) bt.SetActive(false);
            
                Joy_stick_obj.SetActive(false);
                Render_Table_image_obj.SetActive(false);
                Tips_objs.SetActive(false);
                Bar_Time_obj.SetActive(false);
                Bar_Energy_obj.SetActive(false);
                state = UIState.End;
                break;
        }
    }

    // ✅ 你之後按開始就呼叫這個：會淡出Menu → 切到n的UI
    public void FadeToState(int n)
    {
        if (isTransitioning) return;

        // 目前只示範：0/1 的切換（你要擴充 2/3/4 也照樣做）
        if (n == 1)
            StartCoroutine(FadeOutMenuThenSet(n));
        else
            SetUIActives(n);
    }

    // ✅ 給外部呼叫：true=好結局、false=壞結局
    public void PlayEnding(bool isGoodEnding)
    {
        if (endingPlaying) return;
        StartCoroutine(EndingFlow(isGoodEnding));
    }

    IEnumerator EndingFlow(bool isGoodEnding)
    {
        endingPlaying = true;

        // 1) 其他UI關閉（你要關哪些就集中在這裡）
        SetUIActives(2);

        if (DelayFadeInSeconds > 0f)
            yield return new WaitForSeconds(DelayFadeInSeconds);

        // 2) 結束UI打開
        EndingUIRoot.SetActive(true);

        // 3) 好或壞字物件打開（另一個關掉）
        GoodTextObj.SetActive(isGoodEnding);
        BadTextObj.SetActive(!isGoodEnding);

        CanvasGroup targetCG = isGoodEnding ? GoodTextObj.GetComponent<CanvasGroup>() : BadTextObj.GetComponent<CanvasGroup>();

 

        // 4) GC透明度 Fade In
        if (EndingCanvasGroup != null)
        {
            targetCG.alpha = 0f;
            EndingCanvasGroup.alpha = 0f;
            EndingCanvasGroup.interactable = true;
            EndingCanvasGroup.blocksRaycasts = true;

            yield return FadeCanvasGroup(EndingCanvasGroup, 0f, 1f, endingFadeInSeconds);
            yield return FadeCanvasGroup(targetCG, 0f, 1f, 1f);
        }

        endingPlaying = false;
    }


    IEnumerator FadeOutMenuThenSet(int n)
    {
        isTransitioning = true;
        state = UIState.Transition;

        // 鎖住 menu 操作，避免連點
        if (MenuCanvasGroup != null)
        {
            MenuCanvasGroup.interactable = false;
            MenuCanvasGroup.blocksRaycasts = false;
        }

        // 淡出 menu
        yield return StartCoroutine(FadeCanvasGroup(MenuCanvasGroup, MenuCanvasGroup.alpha, 0f, fadeSeconds));


        if (afterFadeDelay > 0f)
            yield return new WaitForSeconds(afterFadeDelay);

        yield return StartCoroutine(FadeCanvasGroup(BasicCanvasGroup, BasicCanvasGroup.alpha, 0f, fadeSeconds));

        // 切換到碟仙UI
        // SetUIActives(n);

        // 直接關掉 MenuRoot（可選）
        if (MenuCanvasGroup != null)
        {
            MenuCanvasGroup.gameObject.SetActive(false);
            BasicCanvasGroup.gameObject.SetActive(false);
        }
        isTransitioning = false;
    }

    IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float seconds)
    {
        if (cg == null) yield break;

        if (seconds <= 0f)
        {
            cg.alpha = to;
            yield break;
        }

        float t = 0f;
        while (t < seconds)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / seconds);
            cg.alpha = Mathf.Lerp(from, to, p);
            yield return null;
        }
        cg.alpha = to;
    }
    public void BackToMenu()
    {
        StartCoroutine(BackToMenuFlow());
    }

    IEnumerator BackToMenuFlow()
    {
        // 1️⃣ 關閉好 / 壞字
        if (GoodTextObj != null)
        {
            var cg = GoodTextObj.GetComponent<CanvasGroup>();
            if (cg) cg.alpha = 0f;
            GoodTextObj.SetActive(false);
        }

        if (BadTextObj != null)
        {
            var cg = BadTextObj.GetComponent<CanvasGroup>();
            if (cg) cg.alpha = 0f;
            BadTextObj.SetActive(false);
        }

        // 2️⃣ 關閉結局 UI
        if (EndingUIRoot != null)
            EndingUIRoot.SetActive(false);

        // 3️⃣ 重設 Menu CanvasGroup（如果你有）
        if (MenuCanvasGroup != null)
        {
            MenuCanvasGroup.gameObject.SetActive(true);
            MenuCanvasGroup.alpha = 1f;
            MenuCanvasGroup.interactable = true;
            MenuCanvasGroup.blocksRaycasts = true;
        }

        // 4️⃣ 切回 Menu UI 狀態
        SetUIActives(0);

        yield break;
    }



    void ApplyMenuVisual(bool show)
    {
        if (MenuCanvasGroup == null) return;

        MenuCanvasGroup.gameObject.SetActive(true);
        MenuCanvasGroup.alpha = show ? 1f : 0f;
        MenuCanvasGroup.interactable = show;
        MenuCanvasGroup.blocksRaycasts = show;
    }
}
