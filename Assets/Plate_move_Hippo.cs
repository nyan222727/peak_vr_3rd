using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Plate_move_Hippo : MonoBehaviour
{
    public FixedJoystick FJ_main;

    [Header("Move")]
    public float moveSpeed = 3.5f;   // 移動速度 (m/s)
    public bool cameraRelative = true; // 搖桿方向是否跟隨相機方向
    public float rotateSpeed = 10f;  // 轉向平滑

    float groundY;
    Rigidbody rb;


    // Start is called before the first frame update
    void Start()
    {
        rb = transform.GetComponent<Rigidbody>();
        groundY = transform.position.y;

    }

    // Update is called once per frame
    void Update()
    {
        float h = FJ_main.Horizontal; // -1~1
        float v = FJ_main.Vertical;   // -1~1

        Vector3 input = new Vector3(v, 0f, -h);
        Vector3 moveDir = input.sqrMagnitude > 1e-4f ? input.normalized : Vector3.zero;
        Vector3 targetPos = rb.position + moveDir * moveSpeed * Time.fixedDeltaTime;
        targetPos.y = groundY;
        rb.MovePosition(targetPos);

    }
}
