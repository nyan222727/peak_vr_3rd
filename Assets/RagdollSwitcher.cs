using System.Collections;
using UnityEngine;

public class RagdollSwitcher : MonoBehaviour
{
    [Header("基本")]
    public Animator animator;               // 角色 Animator
    public Rigidbody mainRigidbody;         // 若有主剛體（沒有可留空）

    [Header("吊掛設定")]
    public ConfigurableJoint hangJoint;     // 掛在 Chest/Spine 上的 Joint
    public Rigidbody hangAnchor;            // 場景裡上方的吊點（有 isKinematic 的 Rigidbody）
    public float hoistHeight = 2f;          // 吊起來往上拉多少
    public float hoistDuration = 1f;        // 往上拉需要多久
    public float fallDelay = 0.3f;          // 先倒在地上幾秒再被吊起

    private Rigidbody[] ragdollBodies;
    private Collider[] ragdollColliders;

    private bool isRagdoll = false;
    private bool isHanging = false;

    void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();

        ragdollBodies = GetComponentsInChildren<Rigidbody>();
        ragdollColliders = GetComponentsInChildren<Collider>();

        // 起始：正常站立狀態
        SetRagdoll(false);

        // 確保一開始沒有連到吊點
        if (hangJoint != null)
            hangJoint.connectedBody = null;
    }

    // 測試：按 H 一鍵觸發
    void Update()
    {
      
    }

    /// <summary>
    /// 一鍵：變 ragdoll → 過一小段時間 → 被吊起來
    /// </summary>
    public void TriggerHang()
    {
        if (isHanging) return;
        StartCoroutine(HangSequence());
    }

    private IEnumerator HangSequence()
    {
        isHanging = true;

        // 1️⃣ 先變 ragdoll 倒地
        SetRagdoll(true);

        // 等一下，讓他先軟掉
        if (fallDelay > 0f)
            yield return new WaitForSeconds(fallDelay);

        // 2️⃣ 接上繩子（Joint 連到吊點）
        if (hangJoint != null && hangAnchor != null)
        {
            hangJoint.connectedBody = hangAnchor;
        }

        // 3️⃣ 往上拉：移動吊點，角色會被一起拉起來
        if (hangAnchor != null && hoistDuration > 0f)
        {
            Vector3 startPos = hangAnchor.transform.position;
            Vector3 endPos = startPos + Vector3.up * hoistHeight;

            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / hoistDuration;
                // 因為是 kinematic 剛體，用 MovePosition / 直接改 transform 都可以
                hangAnchor.MovePosition(Vector3.Lerp(startPos, endPos, t));
                yield return null;
            }
        }

        // 之後就保持吊著軟軟的狀態
    }

    /// <summary>
    /// 切換 ragdoll 開關
    /// </summary>
    private void SetRagdoll(bool enable)
    {
        isRagdoll = enable;

        // Animator、移動控制關掉
        if (animator != null)
            animator.enabled = !enable;

        var controller = GetComponent<CharacterController>();
        if (controller != null)
            controller.enabled = !enable;

        if (mainRigidbody != null)
            mainRigidbody.isKinematic = enable;

        foreach (var rb in ragdollBodies)
        {
            //if (rb == mainRigidbody) continue;
            rb.isKinematic = !enable;
        }

        foreach (var col in ragdollColliders)
        {
            if (!col.isTrigger)
                col.enabled = enable;
        }
    }
}
