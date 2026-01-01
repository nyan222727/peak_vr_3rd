using UnityEngine;

public class SimpleIncenseBurn : MonoBehaviour
{
    [Header("燃燒速度 (越小燒越慢)")]
    public float burnSpeed = 0.05f;

    [Header("最低點 (燒到這裡停止)")]
    public float bottomLimit = -0.9f; // 根據你的香長度調整

    void Update()
    {
        // 如果現在的高度還沒到底部
        if (transform.localPosition.y > bottomLimit)
        {
            // 就持續慢慢往下移動 (沿著 Y 軸)
            transform.Translate(Vector3.down * burnSpeed * Time.deltaTime);
        }
    }
}