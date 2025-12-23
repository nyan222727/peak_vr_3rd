using UnityEngine;

public class XRInteractableTag : MonoBehaviour
{
    [Tooltip("全局唯一ID，建議手動填或用工具生成")]
    public string ItemId;

    [Tooltip("手機端要生成的視覺Prefab key（或索引）")]
    public string VisualKey;
}
