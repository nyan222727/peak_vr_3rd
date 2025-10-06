using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using System.Linq;

[RequireComponent(typeof(NetworkObject))]
public class NetworkOiiaio : NetworkBehaviour
{
    [Header("Healing Effect")]
    public ParticleSystem healingParticle;
    private bool particleIsPlaying = false;

    [Header("Stamina Manager")]
    public ClimbStaminaManager staminaManager;
    public float healPerSecond = 5f;
    private float healTimer = 0f;
    public float rotateSpeed = 1000f;
    public AudioClip rotateSound;
    private AudioSource audioSource;
    public float bounceAmplitude = 0.03f;
    public float bounceFrequency = 10f;
    // private Vector3 initialPosition;
    // private Quaternion initialRotation;
    public bool isBouncing = false;
    public bool isHolding = false;
    [Networked] private Vector3 initialPosition { get; set; }
    [Networked] private Quaternion initialRotation { get; set; }
    // [Networked] private NetworkBool isBouncing { get; set; }
    // [Networked] private NetworkBool isHolding { get; set; }

    // Start is called before the first frame update
    void Start()
    {
        //local
        healingParticle = FindObjectsOfType<NetworkRig>()
             .FirstOrDefault(c => c.Object != null && c.Object.HasStateAuthority).transform.GetChild(0).GetChild(0).GetChild(1).GetComponent<ParticleSystem>();
        
        staminaManager = GameObject.Find("ClimbStaminaManager").GetComponent<ClimbStaminaManager>();
    
        
        // 取得或新增 AudioSource 組件
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        initialPosition = transform.position;
        initialRotation = transform.rotation;
    }

    public void Grab()
    {
        if(!Object.HasStateAuthority)
            Object.RequestStateAuthority();
    }

    public void UnGrab()
    {
        RPC_UnGrab();
    }

    // 按下（開始持續效果）
    public void Press()
    {
        if(!Object.HasStateAuthority)
            Object.RequestStateAuthority();
        if (!isHolding)
        {
            initialPosition = transform.position;
            initialRotation = transform.rotation;
            isHolding = true;
        }
    }

    // 放開（結束效果）
    public void Release()
    {
        if(!Object.HasStateAuthority)
            Object.RequestStateAuthority();
        isHolding = false;
        if (audioSource != null && audioSource.isPlaying)
        {
            // audioSource.Stop();
            RPC_StopSound();
        }
        if (isBouncing)
        {
            transform.position = initialPosition;
            transform.rotation = initialRotation;
            isBouncing = false;
        }
    }

    // 持續效果（需每幀呼叫）
    void Update()
    {
        // 控制 healing 粒子特效
        if (healingParticle)
        {
            if (isHolding && !particleIsPlaying)
            {
                // healingParticle.Play();
                RPC_ShowParticle(healingParticle.transform.root.GetComponent<NetworkRig>().Object.Id);
                Debug.Log("Healing particle playing");
                particleIsPlaying = true;
            }
            else if (!isHolding && particleIsPlaying)
            {
                // healingParticle.Stop();
                RPC_StopParticle(healingParticle.transform.root.GetComponent<NetworkRig>().Object.Id);
                Debug.Log("Healing particle stopped");
                particleIsPlaying = false;
            }
        }
        if (isHolding)
        {
            transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);
            if (rotateSound != null && !audioSource.isPlaying)
            {
                // audioSource.clip = rotateSound;
                // audioSource.Play();
                RPC_StartSound();
            }
            float newY = initialPosition.y + Mathf.Sin(Time.time * bounceFrequency) * bounceAmplitude;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
            isBouncing = true;

            // holding 狀態下每秒治療
            if (staminaManager)
            {
                healTimer += Time.deltaTime;
                if (healTimer >= 1f)
                {
                    staminaManager.HealInjury(healPerSecond);
                    healTimer = 0f;
                }

            }
        }
        else
        {
            healTimer = 0f;
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_StartSound(RpcInfo info = default)
    {
        audioSource.clip = rotateSound;
        audioSource.Play();
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_StopSound(RpcInfo info = default)
    {
        audioSource.Stop();
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_ShowParticle(NetworkId targetId, RpcInfo info = default)
    {
        if (BasicSpawner.Instance.runner.TryFindObject(targetId, out NetworkObject obj))
        {
            var ps = obj.transform.GetChild(0).GetChild(0).GetChild(1).GetComponent<ParticleSystem>();
            ps.Play();
        }
        else
        {
            Debug.LogWarning($"找不到 NetworkObject {targetId}");
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_StopParticle(NetworkId targetId, RpcInfo info = default)
    {
        if (BasicSpawner.Instance.runner.TryFindObject(targetId, out NetworkObject obj))
        {
            var ps = obj.transform.GetChild(0).GetChild(0).GetChild(1).GetComponent<ParticleSystem>();
            ps.Stop();
        }
        else
        {
            Debug.LogWarning($"找不到 NetworkObject {targetId}");
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_UnGrab(RpcInfo info = default)
    {
        GetComponent<Rigidbody>().isKinematic = false;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
    private void RPC_PlayFx(Vector3 where, RpcInfo info = default)
    {
        // 全員執行：播音效、特效、UI 等
    }
}

