using UnityEngine;
using System;

public class DestroyTrace : MonoBehaviour
{
    void OnDisable()
    {
        Debug.LogError($"[DestroyTrace] DISABLED: {gameObject.name} id={GetInstanceID()} scene={gameObject.scene.name}");
    }

    void OnDestroy()
    {
        Debug.LogError($"[DestroyTrace] DESTROYED: {gameObject.name} id={GetInstanceID()} scene={gameObject.scene.name}");
    }
}