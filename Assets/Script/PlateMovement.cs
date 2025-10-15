using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlateMovement : MonoBehaviour
{
    public float moveSpeed = 2.0f;   // 移動速度
    public float moveRange = 1.5f;   // 限制碟子移動範圍
    private Vector3 startPos;

    // Start is called before the first frame update
    void Start()
    {
        startPos = transform.position; // 記錄初始位置
    }

    // Update is called once per frame
    void Update()
    {
        float moveX = Input.GetAxis("Horizontal"); // A/D 或 左右鍵
        float moveZ = Input.GetAxis("Vertical");   // W/S 或 上下鍵

        Vector3 move = new Vector3(moveX, 0f, moveZ) * moveSpeed * Time.deltaTime;

        // 計算新位置
        Vector3 newPos = transform.position + move;

        // 限制碟子移動範圍（避免超出桌面）
        Vector3 offset = newPos - startPos;
        if (offset.magnitude > moveRange)
        {
            newPos = startPos + offset.normalized * moveRange;
        }

        transform.position = newPos;
    }
}
