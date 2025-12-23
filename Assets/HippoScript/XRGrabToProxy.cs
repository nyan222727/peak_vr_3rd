
using UnityEngine;
using Fusion;
using Oculus.Interaction;

public class XRGrabToProxy : MonoBehaviour
{
    [Header("Oculus Interaction")]
    [SerializeField] private Grabbable grabbable;

    [Header("Fusion")]
    [SerializeField] private NetworkRunner runner;
    [SerializeField] private NetworkItemProxy_ServerAuth proxy;

    [Header("Pose Source (optional)")]
    [Tooltip("不指定就用 Grabbable.Transform / grabbable.transform")]
    [SerializeField] private Transform poseSource;

    [Header("Send Rate")]
    [Range(10, 60)]
    [SerializeField] private float sendRateHz = 20f;

    private bool _isHolding;
    private float _timer;

    private Transform PoseSourceResolved =>
        poseSource != null ? poseSource :
        (grabbable != null && grabbable.Transform != null ? grabbable.Transform : transform);

    private void Reset()
    {
        grabbable = GetComponent<Grabbable>();
    }

    private void OnEnable()
    {
        if (grabbable == null) grabbable = GetComponent<Grabbable>();
        if (grabbable != null)
        {
            grabbable.WhenPointerEventRaised += OnPointerEventRaised;
        }
    }

    private void OnDisable()
    {
        if (grabbable != null)
        {
            grabbable.WhenPointerEventRaised -= OnPointerEventRaised;
        }
    }

    private void OnPointerEventRaised(PointerEvent evt)
    {
        // Oculus Interaction：Select = 開始抓、Unselect = 放開、Move = 抓取中更新
        switch (evt.Type)
        {
            case PointerEventType.Select:
                _isHolding = true;
                _timer = 0f;
                if (runner != null && proxy != null)
                    proxy.RPC_RequestGrab(runner.LocalPlayer);
                break;

            case PointerEventType.Unselect:
            case PointerEventType.Cancel:
                _isHolding = false;
                if (runner != null && proxy != null)
                    proxy.RPC_ReleaseGrab(runner.LocalPlayer);
                break;

            case PointerEventType.Move:
                // Move 會很頻繁：節流送 pose 給 Host
                if (!_isHolding || runner == null || proxy == null) return;

                _timer += Time.deltaTime;
                float interval = 1f / Mathf.Max(1f, sendRateHz);
                if (_timer < interval) return;
                _timer = 0f;

                var t = PoseSourceResolved;
                proxy.RPC_SendPose(runner.LocalPlayer, t.position, t.rotation);
                break;
        }
    }
}
