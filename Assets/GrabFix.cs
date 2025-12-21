using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion; // remove this line + authority check if you don't want Fusion dependency

public class GrabFix : MonoBehaviour
{
    [Header("What to rotate")]
    [SerializeField] private Transform target; // usually leave empty = this.transform

    [Header("Fixed rotation reference (set this in editor)")]
    [SerializeField] private Transform rotationReference; 
    // Create a child "UprightRef" and rotate it to the exact upright you want, then drag here.

    [Header("Networking")]
    [SerializeField] private bool authorityOnly = true;

    private bool _held;
    private NetworkObject _netObj;

    private void Awake()
    {
        if (target == null) target = transform;
        _netObj = GetComponentInParent<NetworkObject>();
    }

    // Hook these two from events
    public void OnGrabbed()
    {
        _held = true;
        ApplyFixedRotation();
    }

    public void OnReleased()
    {
        _held = false;
    }

    private void LateUpdate()
    {
        if (!_held) return;

        if (authorityOnly && _netObj != null && !_netObj.HasStateAuthority)
            return;

        ApplyFixedRotation();
    }

    private void ApplyFixedRotation()
    {
        if (rotationReference == null) return;
        target.rotation = rotationReference.rotation;
    }
}
