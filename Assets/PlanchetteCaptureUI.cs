using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlanchetteCaptureUI : MonoBehaviour
{
    [System.Serializable]
    public class RoundData
    {
        public Transform lookAtTarget;      // 這輪要拍的字的位置(可選)
        public float countdownSeconds = 3f;
    }

    [Header("Rounds (Size=4)")]
    public RoundData[] rounds = new RoundData[4];

    [Header("Crop Settings (0~1)")]
    public float cropCenterX = 0.5f;   // 中心 X（0~1）
    public float cropCenterY = 0.5f;   // 中心 Y（0~1）
    public float cropWidth = 0.3f;     // 寬度比例（例如 30%）
    public float cropHeight = 0.3f;    // 高度比例

    [Header("UI - Countdown")]
    public Text countdownText;             // 舊版 UGUI Text
    public GameObject countdownRoot;       // 倒數容器(可不填)

    [Header("UI - 4 Slots")]
    public RawImage[] slots = new RawImage[4];  // 四格 RawImage
    public Texture placeholderTexture;          // 一開始顯示的空圖

    [Header("Capture")]
    public Camera hintCamera;
    public RenderTexture hintRT;

    [Header("Flow Control")]
    public bool waitForConfirmEachRound = true;
    private bool confirmed;

    Coroutine flowRoutine;

    // 開始四輪流程
    public void StartFlow()
    {
        if (flowRoutine != null) StopCoroutine(flowRoutine);
        flowRoutine = StartCoroutine(Flow());
    }

    // 每輪字擺好後呼叫（可綁按鈕/VR互動）
    public void ConfirmReady()
    {
        confirmed = true;
    }

    // 一開始把四格清空
    public void ResetSlots()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null) continue;
            slots[i].texture = placeholderTexture; // 沒有 placeholder 也可以設 null
            slots[i].color = new Color(1, 1, 1, 0.5f); // 若你想用透明方式清空也行
        }
        confirmed = false;
        flowRoutine = null;

    }

    IEnumerator Flow()
    {
        ResetSlots();

        if (countdownRoot != null) countdownRoot.SetActive(false);

        for (int i = 0; i < rounds.Length && i < 4; i++)
        {
            confirmed = false;

            // 對準該輪目標（讓字更清楚）
            if (hintCamera != null && rounds[i].lookAtTarget != null)
                hintCamera.transform.LookAt(rounds[i].lookAtTarget.position);

            // 等你說「字擺好」
            if (waitForConfirmEachRound)
            {
                while (!confirmed) yield return null;
            }

            // 倒數
            yield return StartCoroutine(Countdown(rounds[i].countdownSeconds));
            yield return new WaitForEndOfFrame();

            // 截圖
            Texture2D fullShot = CaptureFromCamera(hintCamera, hintRT);

            Texture2D cropped = CropTexture(fullShot, cropCenterX, cropCenterY, cropWidth, cropHeight);

            // 填入第 i 格
            if (slots[i] != null && cropped != null)
            {
                slots[i].texture = cropped;
                slots[i].color = new Color(1, 1, 1, 1);
                // 如果你想固定填滿格子，通常不需要 SetNativeSize
                // slots[i].SetNativeSize();
            }
        }

        flowRoutine = null;
    }

    IEnumerator Countdown(float seconds)
    {
        if (countdownRoot != null) countdownRoot.SetActive(true);

        float t = seconds;
        while (t > 0f)
        {
            if (countdownText != null)
                countdownText.text = Mathf.CeilToInt(t).ToString();
            yield return null;
            t -= Time.deltaTime;
        }

        if (countdownText != null) countdownText.text = "0";

        if (countdownRoot != null) countdownRoot.SetActive(false);
    }

    Texture2D CaptureFromCamera(Camera cam, RenderTexture rt)
    {
        if (cam == null || rt == null)
        {
            Debug.LogError("HintCamera 或 RenderTexture 沒有設定！");
            return null;
        }

        RenderTexture prevActive = RenderTexture.active;
        RenderTexture prevTarget = cam.targetTexture;

        cam.targetTexture = rt;
        RenderTexture.active = rt;

        cam.Render();

        Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();

        cam.targetTexture = prevTarget;
        RenderTexture.active = prevActive;

        return tex;
    }

    Texture2D CropTexture(
    Texture2D source,
    float centerXPercent,
    float centerYPercent,
    float widthPercent,
    float heightPercent)
    {
        int w = source.width;
        int h = source.height;

        int cropW = Mathf.RoundToInt(w * widthPercent);
        int cropH = Mathf.RoundToInt(h * heightPercent);

        int centerX = Mathf.RoundToInt(w * centerXPercent);
        int centerY = Mathf.RoundToInt(h * centerYPercent);

        int startX = Mathf.Clamp(centerX - cropW / 2, 0, w - cropW);
        int startY = Mathf.Clamp(centerY - cropH / 2, 0, h - cropH);

        Color[] pixels = source.GetPixels(startX, startY, cropW, cropH);

        Texture2D cropped = new Texture2D(cropW, cropH, source.format, false);
        cropped.SetPixels(pixels);
        cropped.Apply();

        return cropped;
    }

}
