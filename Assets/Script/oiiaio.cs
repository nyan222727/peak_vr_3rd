using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class oiiaio : MonoBehaviour
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
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private bool isBouncing = false;
    private bool isHolding = false;
    // Start is called before the first frame update
    void Start()
    {
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

    // 按下（開始持續效果）
    public void Press()
    {
        if (!isHolding)
        {
            initialRotation = transform.rotation;
            isHolding = true;
        }
    }

    // 放開（結束效果）
    public void Release()
    {
        isHolding = false;
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
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
                healingParticle.Play();
                Debug.Log("Healing particle playing");
                particleIsPlaying = true;
            }
            else if (!isHolding && particleIsPlaying)
            {
                healingParticle.Stop();
                Debug.Log("Healing particle stopped");
                particleIsPlaying = false;
            }
        }
        if (isHolding)
        {
            transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);
            if (rotateSound != null && !audioSource.isPlaying)
            {
                audioSource.clip = rotateSound;
                audioSource.Play();
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
}
