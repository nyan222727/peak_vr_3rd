using Fusion;
using UnityEngine;

public struct BoneState : INetworkStruct
{
    public Vector3 Position;
    public Quaternion Rotation;
}