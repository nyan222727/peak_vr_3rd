using UnityEngine;

public class CameraSpringUp : MonoBehaviour
{

    private OVRClimbingRig climbingRig;
    [Header("Capsule Collider Settings")]
    public CapsuleCollider capsuleCollider;
    public Transform head; // 指向玩家頭盔
    public float radius = 0.25f;
    public float minHeight = 1.0f;
    public float maxHeight = 2.2f;
    public float skinOffset = 0.05f;
    public float climbCapsuleHeight = 0.5f;
    public float capsuleHeightSmoothTime = 0.2f;
    private float capsuleHeightVelocity = 0f;
    private float normalCapsuleHeight;
    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogWarning("CameraSpringUp 需要 Rigidbody 組件");
        }
        if (capsuleCollider == null)
        {
            capsuleCollider = GetComponent<CapsuleCollider>();
        }
        if (capsuleCollider != null)
        {
            capsuleCollider.direction = 1; // y 軸
            capsuleCollider.radius = radius;
            normalCapsuleHeight = capsuleCollider.height;
        }
        climbingRig = FindObjectOfType<OVRClimbingRig>();
    }

    void LateUpdate()
    {
        if (!capsuleCollider) return;
        bool climbing = climbingRig != null && climbingRig.isClimbing;

        float targetHeight;
        Vector3 headLocal = head ? transform.InverseTransformPoint(head.position) : Vector3.zero;
        if (climbing)
        {
            // 攀爬時縮短 capsule，頂端不變（底部往上）
            targetHeight = climbCapsuleHeight;
        }
        else if (head)
        {
            targetHeight = Mathf.Clamp(headLocal.y, minHeight, maxHeight);
        }
        else
        {
            targetHeight = normalCapsuleHeight;
        }

        float prevHeight = capsuleCollider.height;
        float newHeight = Mathf.SmoothDamp(prevHeight, targetHeight, ref capsuleHeightVelocity, capsuleHeightSmoothTime);
        capsuleCollider.height = newHeight;

        // 頂端不變，底部上升
        Vector3 center = capsuleCollider.center;
        if (head)
        {
            float topY = headLocal.y + skinOffset;
            if (climbing)
            {
                // 攀爬時縮短，頂端不變
                center.y = topY - newHeight / 2f;
            }
            else
            {
                // 伸長時底部往下，頂端不動
                center.y = topY - newHeight / 2f;
            }
        }

        // x,z 位置跟隨頭盔
        if (head)
        {
            center.x = headLocal.x;
            center.z = headLocal.z;
        }
        capsuleCollider.center = center;
        capsuleCollider.radius = radius;

    }
}
