using UnityEngine;

public enum RequestedObjectId
{
    Candle = 0,
    Talisman = 1,
    CherryTwig = 2,
    Bottle = 3,
    BlackDoll = 4,
    YellowDoll = 5,
    RedDoll = 6,
}

public sealed class RequestableObject : MonoBehaviour
{
    [SerializeField] private RequestedObjectId objectId;
    public RequestedObjectId ObjectId => objectId;
}
