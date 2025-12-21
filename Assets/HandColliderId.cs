using Fusion;
using UnityEngine;

public class HandColliderId : MonoBehaviour
{
    public HandLock.HandSide side;

    [Tooltip("Optional. If empty, auto-finds in parent.")]
    public HandLock stickLock;

    private NetworkObject _playerNetworkObject;

    private void Awake()
    {
        if (!stickLock)
            stickLock = GetComponentInParent<HandLock>();

        _playerNetworkObject = GetComponentInParent<NetworkObject>();
    }

    // In Fusion, local player's object typically has Input Authority
    public bool IsLocalPlayerHand =>
        _playerNetworkObject == null || _playerNetworkObject.HasInputAuthority;
}
