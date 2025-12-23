using UnityEngine;

public class ObjectDescription : MonoBehaviour
{
    [TextArea] public string Title;
    [TextArea(3, 12)] public string Body;
}
