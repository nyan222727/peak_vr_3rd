using UnityEngine;
using System.Collections.Generic;

public class OVRClimbingRig : MonoBehaviour
{
    [Header("Stamina")]
    public ClimbStaminaManager staminaManager;
    [HideInInspector]
    public bool isClimbing = false; // 是否正在攀爬，供其他腳本使用

    [Header("Refs")]
    public Transform leftHand;          // 追蹤到的左手/控制器 Transform
    public Transform rightHand;         // 追蹤到的右手/控制器 Transform
    private bool lHandTrackedGrabbing = false;
    private bool rHandTrackedGrabbing = false;
    public Rigidbody rigRb;             // PlayerRig 的 Rigidbody
    public LayerMask climbableMask;     // 指向 Climbable Layer
    public float handProbeRadius = 0.09f;
    public float gripThreshold = 0.55f; // OVRInput Axis1D 觸發門檻
    public float throwBoost = 1.2f;     // 放開時把手速轉成身體速的倍率
    public float maxClimbSpeed = 6.0f;  // 夾限，避免瞬移感
    public float stickyAssist = 0.02f;  // 抓取時把 grabPoint 輕微吸回碰撞面，降低漂移

    struct HandState
    {
        public bool isGrabbing;
        public Vector3 grabPointWS;      // 固定的抓取點位置（世界空間）
        public Vector3 initialHandPosWS; // 初始抓取時的手位置（世界空間）
        public Vector3 lastHandPosWS;    // 上一幀的手位置（用於計算速度）
        public Queue<Vector3> recentVel; // 用於拋出速度
        public Collider latchCol;        // 記住抓到哪個 Collider
    }

    private HandState left, right;
    private bool wasEitherHandGrabbing = false;  // 記錄上一幀是否有任何手在抓取狀態
    private Vector3 lastVelocity = Vector3.zero; // 記錄最後一個速度，用於平滑過渡

    void Reset()
    {
        rigRb = GetComponent<Rigidbody>();
    }

    void Awake()
    {
        Init(ref left);
        Init(ref right);
        if (rigRb) rigRb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void Init(ref HandState h)
    {
        h.isGrabbing = false;
        h.grabPointWS = Vector3.zero;
        h.initialHandPosWS = Vector3.zero;
        h.lastHandPosWS = Vector3.zero;
        h.recentVel = new Queue<Vector3>(16);
        h.latchCol = null;
    }

    void Update()
    {
        // 讀取 OVRInput（Meta All-in-One SDK）
        // 支援手部追蹤時的 grip（PrimaryHandTrigger 或 HandTrigger 只要有一個達門檻即可）
        float lGrip = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, OVRInput.Controller.LTouch);
        float rGrip = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, OVRInput.Controller.RTouch);

        // 若手部追蹤判斷為抓取，直接將 grip 設為 1
        if (lHandTrackedGrabbing) lGrip = 1f;
        if (rHandTrackedGrabbing) rGrip = 1f;

        // 手邏輯：進出抓取狀態
        HandleHand(ref left, leftHand, lGrip, true);
        HandleHand(ref right, rightHand, rGrip, false);

    }

    void FixedUpdate()
    {
        // 檢查是否有任何手在抓取
        bool isGrabbing = left.isGrabbing || right.isGrabbing;
        bool justStartedGrabbing = isGrabbing && !wasEitherHandGrabbing;
        bool justStoppedGrabbing = !isGrabbing && wasEitherHandGrabbing;

        // 更新 isClimbing 狀態
        isClimbing = isGrabbing;

        // 通知 StaminaManager 攀爬狀態，消耗/恢復體力
        if (staminaManager)
        {
            staminaManager.SetClimbing(isClimbing);
        }

        // 計算單手位移（現在只會有一隻手抓取）
        Vector3 totalOffset = Vector3.zero;

        // 由於強制單手模式，這裡只會有一個條件為真
        if (left.isGrabbing) totalOffset = ComputeClimbOffset(ref left, leftHand);
        else if (right.isGrabbing) totalOffset = ComputeClimbOffset(ref right, rightHand);

        // 如果有手在抓取狀態
        if (isGrabbing)
        {
            // 限速 & 套用到剛體
            if (totalOffset.sqrMagnitude > 0f)
            {
                // 計算新速度
                Vector3 vel = totalOffset / Time.fixedDeltaTime;
                if (vel.magnitude > maxClimbSpeed) vel = vel.normalized * maxClimbSpeed;

                // 如果剛剛開始抓取，平滑過渡到新速度
                if (justStartedGrabbing)
                {
                    vel = Vector3.Lerp(lastVelocity, vel, 0.5f);
                }

                // 直接設定速度，保留重力方向的分量（更自然）
                Vector3 newVel = vel;
                newVel.y = Mathf.Max(newVel.y, rigRb.velocity.y); // 允許往上拉、保持下墜重力
                rigRb.velocity = newVel;

                // 記錄最後速度，用於平滑過渡
                lastVelocity = newVel;
            }
        }

        // 更新抓取狀態追蹤
        wasEitherHandGrabbing = isGrabbing;

        // 記錄手速，用於拋出
        AccumulateRecentVel(ref left, leftHand);
        AccumulateRecentVel(ref right, rightHand);
    }

    void HandleHand(ref HandState h, Transform hand, float grip, bool isLeft)
    {
        bool wantGrab = grip >= gripThreshold;
        // 攀爬前詢問體力
        if (wantGrab && staminaManager && staminaManager.GetStaminaPercent() <= 0f)
        {
            wantGrab = false; // 沒體力不能攀爬
        }
        bool otherHandGrabbing = isLeft ? right.isGrabbing : left.isGrabbing;
        Transform otherHandTransform = isLeft ? rightHand : leftHand;
        bool otherHandIsLeft = !isLeft;

        if (!h.isGrabbing && wantGrab)
        {
            // 嘗試在手附近找 Climbable - 只在手確實碰到可攀爬物體時才抓取
            if (TryFindClimbable(hand.position, out var hit, out var col))
            {
                // 強制模式：如果另一隻手已經在抓取，強制放開它
                if (otherHandGrabbing)
                {
                    // 保留當前速度狀態，不應用拋出力
                    lastVelocity = rigRb.velocity;

                    // 釋放另一隻手的抓取
                    if (isLeft)
                    {
                        right.isGrabbing = false;
                        right.latchCol = null;
                    }
                    else
                    {
                        left.isGrabbing = false;
                        left.latchCol = null;
                    }

                    // 可選的觸覺反饋，表示另一隻手被強制放開
                    OVRInput.SetControllerVibration(0.1f, 0.3f, otherHandIsLeft ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch);
                }
                else
                {
                    // 第一隻手抓取，可以安全地重置速度
                    rigRb.velocity = Vector3.zero;
                }

                // 設置新手的抓取狀態
                h.isGrabbing = true;
                h.latchCol = col;
                // 固定抓取點在碰撞位置
                h.grabPointWS = hit.point;
                h.initialHandPosWS = hand.position; // 記錄初始抓取時的手位置
                h.lastHandPosWS = hand.position;

                // 微調抓點貼面（降低手的抖動漂移）
                h.grabPointWS += hit.normal * stickyAssist;

                // 簡單觸覺回饋
                OVRInput.SetControllerVibration(0.2f, 0.7f, isLeft ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch);
                Invoke(nameof(StopHaptics), 0.06f);
            }
        }
        // 強制鬆手：體力不足時
        else if (h.isGrabbing && (!wantGrab || (staminaManager && staminaManager.GetStaminaPercent() <= 0f)))
        {
            // 放手：單手模式下直接釋放並應用拋出力
            Vector3 throwVel = AverageRecentVel(h);

            // 防止異常高速 - 特別是垂直方向
            float maxThrowSpeed = 3.0f; // 最大拋射速度
            if (throwVel.magnitude > maxThrowSpeed)
            {
                throwVel = throwVel.normalized * maxThrowSpeed;
            }

            // 特別限制垂直分量以防止飛天
            float maxVerticalThrow = 1.0f; // 最大垂直拋射
            if (throwVel.y > maxVerticalThrow)
            {
                throwVel.y = maxVerticalThrow;
            }

            // 只有在手速不為零時才套用拋射力
            if (throwVel.sqrMagnitude > 0.01f)
            {
                rigRb.velocity += throwVel * throwBoost;
            }

            h.isGrabbing = false;
            h.latchCol = null;

            // 記錄放開後的速度，用於下次抓取時的平滑過渡
            lastVelocity = rigRb.velocity;
        }
    }

    Vector3 ComputeClimbOffset(ref HandState h, Transform hand)
    {
        // 計算當前手相對於初始抓取位置的位移
        Vector3 currentToInitialOffset = h.initialHandPosWS - hand.position;

        // 記錄上一幀手位置（用於計算速度）
        Vector3 handDelta = hand.position - h.lastHandPosWS;
        h.lastHandPosWS = hand.position;

        // 檢查抓取點是否仍然有效
        if (h.latchCol != null)
        {
            // 可以添加額外的檢查，例如手是否已經移動太遠等
            float distToGrab = Vector3.Distance(hand.position, h.grabPointWS);

            // 如果手離抓取點太遠，可以選擇自動釋放或限制最大距離
            if (distToGrab > 0.5f) // 可調整的閾值
            {
                // 在這個實現中，我們不自動釋放，但會限制偏移量防止過度拉伸
                float maxOffset = 0.4f; // 可調整的最大偏移
                if (currentToInitialOffset.magnitude > maxOffset)
                {
                    currentToInitialOffset = currentToInitialOffset.normalized * maxOffset;
                }
            }

            // 測試可視化（調試用）
            // Debug.DrawLine(hand.position, h.grabPointWS, Color.red);
            // Debug.DrawLine(hand.position, h.initialHandPosWS, Color.blue);
        }

        // 使用初始抓取位置和當前手位置之間的偏移作為移動依據
        // 這樣可以確保手和抓取點之間的相對關係被正確維護
        return currentToInitialOffset;
    }

    bool TryFindClimbable(Vector3 center, out RaycastHit hit, out Collider col)
    {
        // 在手附近做球體檢測，找最近的 Climbable 面
        // 確保手真正碰到了可攀爬物體
        Collider[] cols = Physics.OverlapSphere(center, handProbeRadius, climbableMask, QueryTriggerInteraction.Collide);
        float bestDist = float.PositiveInfinity;
        col = null;
        hit = default;

        // 只有當真正碰到可攀爬物體時才返回 true
        if (cols.Length == 0)
        {
            return false;
        }

        foreach (var c in cols)
        {
            Vector3 closest = c.ClosestPoint(center);
            float d = (closest - center).sqrMagnitude;

            // 只考慮真正接觸到的物體（距離非常接近）
            if (d < bestDist && d < handProbeRadius * handProbeRadius * 0.5f)
            { // 使用更嚴格的距離判斷
                // 用 Raycast 拿到法線與精確點
                Vector3 dir = (closest - center);
                float len = Mathf.Max(0.01f, Mathf.Sqrt(d));
                if (Physics.Raycast(center, dir.normalized, out var h, len + 0.02f, climbableMask, QueryTriggerInteraction.Collide))
                {
                    bestDist = d;
                    hit = h;
                    col = c;
                }
                else
                {
                    // 沒打到就直接用 ClosestPoint
                    bestDist = d;
                    hit = new RaycastHit { point = closest, normal = (center - closest).normalized };
                    col = c;
                }
            }
        }
        return col != null;
    }

    void AccumulateRecentVel(ref HandState h, Transform hand)
    {
        // 計算當前速度
        Vector3 v = (hand.position - h.lastHandPosWS) / Mathf.Max(Time.fixedDeltaTime, 1e-4f);

        // 過濾異常速度值 - 設置合理的最大速度閾值
        float maxValidSpeed = 10.0f;
        if (v.magnitude > maxValidSpeed)
        {
            v = v.normalized * maxValidSpeed;
        }

        // 存儲速度歷史
        if (h.recentVel.Count > 14) h.recentVel.Dequeue();
        h.recentVel.Enqueue(v);
    }

    Vector3 AverageRecentVel(HandState h)
    {
        if (h.recentVel.Count == 0) return Vector3.zero;
        Vector3 sum = Vector3.zero;
        foreach (var v in h.recentVel) sum += v;
        return sum / h.recentVel.Count;
    }

    void StopHaptics()
    {
        OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.LTouch);
        OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.RTouch);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        if (leftHand) Gizmos.DrawWireSphere(leftHand.position, handProbeRadius);
        if (rightHand) Gizmos.DrawWireSphere(rightHand.position, handProbeRadius);
    }

    public void SetLeftHandTrackedGrab(bool grabbing)
    {
        lHandTrackedGrabbing = grabbing;
    }
    public void SetRightHandTrackedGrab(bool grabbing)
    {
        rHandTrackedGrabbing = grabbing;
    }
}


