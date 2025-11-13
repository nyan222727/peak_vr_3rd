using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class NetworkRig : NetworkBehaviour
{
    public bool IsLocalNetworkRig => Object.HasStateAuthority;

    [Header("RigComponents")]
    [SerializeField]
    private NetworkTransform playerTransform;

    [SerializeField]
    private NetworkTransform headTransform;

    [SerializeField]
    private NetworkTransform leftHandTransform;

    [SerializeField]
    private NetworkTransform rightHandTransform;

    [Header("Hand Bone Roots")]
    public Transform leftHandRoot;
    public Transform rightHandRoot;

    public List<Transform> leftBones = new();
    public List<Transform> rightBones = new();

    [Networked, Capacity(64)] 
    public NetworkArray<BoneState> LeftBones { get; }

    [Networked, Capacity(64)] 
    public NetworkArray<BoneState> RightBones { get; }

    HardwareRig hardwareRig;

    public override void Spawned()
    {
        base.Spawned();

        if (IsLocalNetworkRig)
        {
            hardwareRig = FindObjectOfType<HardwareRig>();
            if (hardwareRig == null)
                Debug.LogError("Missing HardwareRig in the scene");
        }
        // else it means that this is a client

        CollectBonesRecursive(leftHandRoot, leftBones);
        CollectBonesRecursive(rightHandRoot, rightBones);
    }

    void CollectBonesRecursive(Transform root, List<Transform> list)
    {
        if (root == null) return;
        if(root.gameObject.name != "XRHand_Wrist") list.Add(root);
        foreach (Transform child in root)
            CollectBonesRecursive(child, list);
    }

    //if you dont have networktransform you need to manually update the position/rotation
    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();

        if (GetInput<RigState>(out var input))
        {
            playerTransform.transform.SetPositionAndRotation(input.PlayerPosition, input.PlayerRotation);

            headTransform.transform.SetPositionAndRotation(input.HeadsetPosition, input.HeadsetRotation);

            leftHandTransform.transform.SetPositionAndRotation(input.LeftHandPosition, input.LeftHandRotation);

            rightHandTransform.transform.SetPositionAndRotation(input.RightHandPosition, input.RightHandRotation);
        }
    }

    public void UpdateLocalHandBones(List<Transform> left, List<Transform> right)
    {
        int count = Mathf.Min(left.Count, LeftBones.Length);
        for (int i = 0; i < count; i++)
        {
            LeftBones.Set(i, new BoneState
            {
                Position = left[i].localPosition,
                Rotation = left[i].localRotation
            });
        }

        count = Mathf.Min(right.Count, RightBones.Length);
        for (int i = 0; i < count; i++)
        {
            RightBones.Set(i, new BoneState
            {
                Position = right[i].localPosition,
                Rotation = right[i].localRotation
            });
        }
    }

    public override void Render()
    {
        base.Render();
        if (IsLocalNetworkRig)
        {
            playerTransform.transform.SetPositionAndRotation(hardwareRig.playerTransform.position, hardwareRig.playerTransform.rotation);

            headTransform.transform.SetPositionAndRotation(hardwareRig.headTransform.position, hardwareRig.headTransform.rotation);

            leftHandTransform.transform.SetPositionAndRotation(hardwareRig.leftHandTransform.position, hardwareRig.leftHandTransform.rotation);

            rightHandTransform.transform.SetPositionAndRotation(hardwareRig.rightHandTransform.position, hardwareRig.rightHandTransform.rotation);

            for (int i = 0; i < hardwareRig.leftBones.Count; i++)
            {
                leftBones[i].localPosition = hardwareRig.leftBones[i].localPosition;
                leftBones[i].localRotation = hardwareRig.leftBones[i].localRotation;
            }

            for (int i = 0; i < hardwareRig.rightBones.Count; i++)
            {
                rightBones[i].localPosition = hardwareRig.rightBones[i].localPosition;
                rightBones[i].localRotation = hardwareRig.rightBones[i].localRotation;
            }
        }
        else
        {
            int count = Mathf.Min(leftBones.Count, LeftBones.Length);
            for (int i = 0; i < count; i++)
            {
                BoneState b = LeftBones.Get(i);
                leftBones[i].localPosition = b.Position;
                leftBones[i].localRotation = b.Rotation;
            }

            count = Mathf.Min(rightBones.Count, RightBones.Length);
            for (int i = 0; i < count; i++)
            {
                BoneState b = RightBones.Get(i);
                rightBones[i].localPosition = b.Position;
                rightBones[i].localRotation = b.Rotation;
            }
        }
    }
}