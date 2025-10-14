using UnityEngine;

public class PlayerLocomotion : MonoBehaviour
{
    [Header("XR Head Reference")]
    public Transform head; // 指向 Quest 頭盔 transform
    [Header("Snap Turn Settings")]
    public float snapTurnAngle = 45f; // Snap 轉向角度
    public float snapTurnThreshold = 0.7f; // 右搖桿 X 軸超過此值才觸發
    private bool snapTurnReady = true;
    public float moveSpeed = 3f;
    public float jumpForce = 5f;
    public LayerMask groundMask;
    public float groundCheckDistance = 1.2f;
    public Transform groundCheck;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogWarning("PlayerLocomotion 需要 Rigidbody 組件");
        }
        if (groundCheck == null)
        {
            groundCheck = this.transform;
        }
    }

    void Update()
    {
        // 以頭盔面向計算移動方向（不影響角色本身 rotation）
        Transform moveRef = head ? head : transform;
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        Debug.Log($"h: {h}, v: {v}");
        Vector3 move = (moveRef.right * h + moveRef.forward * v).normalized;
        Vector3 velocity = new Vector3(move.x * moveSpeed, rb.velocity.y, move.z * moveSpeed);
        Debug.Log($"Velocity: {velocity}");
        rb.velocity = velocity;
        Debug.Log($"VelocityRB: {rb.velocity}");

        // 右手搖桿 snap turn（執行時暫時關閉 interpolation）
        if (snapTurnReady && rb != null)
        {
            if (OVRInput.GetDown(OVRInput.RawButton.RThumbstickLeft))
            {
                var prevInterp = rb.interpolation;
                rb.interpolation = RigidbodyInterpolation.None;
                rb.MoveRotation(Quaternion.AngleAxis(-snapTurnAngle, Vector3.up) * rb.rotation);
                rb.interpolation = prevInterp;
                snapTurnReady = false;
            }
            else if (OVRInput.GetDown(OVRInput.RawButton.RThumbstickRight))
            {
                var prevInterp = rb.interpolation;
                rb.interpolation = RigidbodyInterpolation.None;
                rb.MoveRotation(Quaternion.AngleAxis(snapTurnAngle, Vector3.up) * rb.rotation);
                rb.interpolation = prevInterp;
                snapTurnReady = false;
            }
        }
        // 只有在左右按鍵都沒按下才允許再次 snap
        if (!OVRInput.Get(OVRInput.RawButton.RThumbstickLeft) && !OVRInput.Get(OVRInput.RawButton.RThumbstickRight))
        {
            snapTurnReady = true;
        }

        // 跳躍（禁止空中跳躍，並確認腳下平面夠平緩）
        bool jumpInput = Input.GetKeyDown(KeyCode.Space) || Input.GetButtonDown("Jump") || OVRInput.GetDown(OVRInput.Button.SecondaryThumbstick);
        if (jumpInput)
        {
            float cheatGroundCheckDistance = groundCheckDistance;
            if (head)
            {
                cheatGroundCheckDistance = transform.InverseTransformPoint(head.position).y;
            }
            RaycastHit hit;
            float jumpCheckDistance = cheatGroundCheckDistance + 0.1f;
            if (Physics.Raycast(groundCheck.position, Vector3.down, out hit, jumpCheckDistance, groundMask))
            {
                float slope = Vector3.Dot(hit.normal, Vector3.up);
                if (slope > 0.7f)
                {
                    rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
                }
            }
        }
    }
}
