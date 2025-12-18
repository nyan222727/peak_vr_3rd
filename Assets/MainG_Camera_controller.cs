using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainG_Camera_controller : MonoBehaviour
{
    [Header("目標 & 初始位置")]
    public Transform target;
    public Joystick joystick;            // 🔹 拖你的虛擬搖桿進來

    public float distance = 5f;          // 與目標的水平距離
    public float height = 2f;            // 初始高度（相對目標）
    public float minHeight = 0.5f;       // 最低高度
    public float maxHeight = 5f;         // 最高高度
    public float lookHeightOffset = 1.5f;// 視線對準目標的高度偏移（看頭部）

    [Header("旋轉 & 高度調整")]
    public float rotateSpeed = 90f;      // 水平旋轉速度（度/秒）
    public float heightAdjustSpeed = 2f; // 高度調整速度

    [Header("平滑參數")]
    public float moveSmoothTime = 0.15f; // 位置平滑
    public float rotateSmoothSpeed = 10f;// 旋轉平滑

    private float currentAngle = 0f;
    private float targetHeight;
    private Vector3 posVelocity = Vector3.zero;

    // 重置用
    private float initialAngle;
    private float initialHeight;

    void Start()
    {
        if (target == null)
        {
            Debug.LogWarning("SmoothOrbitCamera：請指定 target");
            enabled = false;
            return;
        }

        // 用目前相機位置反推初始角度 & 高度
        Vector3 fromTarget = transform.position - target.position;
        Vector3 flat = new Vector3(fromTarget.x, 0f, fromTarget.z);

        if (flat.sqrMagnitude < 0.0001f)
            currentAngle = 0f;
        else
            currentAngle = Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg;

        targetHeight = transform.position.y - target.position.y;

        initialAngle = currentAngle;
        initialHeight = targetHeight;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // 1️⃣ 水平輸入：鍵盤 + 搖桿
        float keyboardH = Input.GetAxisRaw("Horizontal");      // ← → / A D
        float joystickH = (joystick != null) ? joystick.Horizontal : 0f; // -1 ~ 1

        float h = Mathf.Clamp(keyboardH + -joystickH, -1f, 1f);
        currentAngle += h * rotateSpeed * Time.deltaTime;

        // 2️⃣ 垂直輸入（高度）：鍵盤 + 搖桿
        float keyboardV = 0f;
        if (Input.GetKey(KeyCode.UpArrow)) keyboardV = 1f;
        else if (Input.GetKey(KeyCode.DownArrow)) keyboardV = -1f;

        float joystickV = (joystick != null) ? joystick.Vertical : 0f; // -1 ~ 1

        float v = Mathf.Clamp(keyboardV + joystickV, -1f, 1f);

        // 小範圍抖動直接當 0，避免搖桿沒回正就晃
        if (Mathf.Abs(joystickV) < 0.1f && keyboardV == 0f)
            v = 0f;

        targetHeight += v * heightAdjustSpeed * Time.deltaTime;
        targetHeight = Mathf.Clamp(targetHeight, minHeight, maxHeight);

        // 3️⃣ 計算理想位置（繞圈）
        float rad = currentAngle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad)) * distance;

        Vector3 targetPos = target.position;
        Vector3 wantedPos = targetPos + offset;
        wantedPos.y = targetPos.y + targetHeight;

        // 4️⃣ 平滑移動
        transform.position = Vector3.SmoothDamp(
            transform.position,
            wantedPos,
            ref posVelocity,
            moveSmoothTime
        );

        // 5️⃣ 平滑看向目標
        Vector3 lookPoint = targetPos + Vector3.up * lookHeightOffset;
        Vector3 dir = lookPoint - transform.position;

        if (dir.sqrMagnitude > 0.0001f)
        {
            Quaternion wantedRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                wantedRot,
                rotateSmoothSpeed * Time.deltaTime
            );
        }
    }

    // 🔄 重置到初始視角（給 UI Button 或其他地方呼叫）
    public void ResetCamera()
    {
        currentAngle = initialAngle;
        targetHeight = initialHeight;
        posVelocity = Vector3.zero;

        Vector3 targetPos = target.position;

        float rad = currentAngle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad)) * distance;

        Vector3 camPos = targetPos + offset;
        camPos.y = targetPos.y + targetHeight;
        transform.position = camPos;

        Vector3 lookPoint = targetPos + Vector3.up * lookHeightOffset;
        Vector3 dir = lookPoint - transform.position;

        if (dir.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
    }
}
