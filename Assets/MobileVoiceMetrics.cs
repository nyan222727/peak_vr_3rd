using UnityEngine;
using Photon.Voice.Unity;

public class MobileVoiceMetrics : MonoBehaviour
{
    [Header("Photon Voice")]
    public Recorder recorder;

    [Header("Outputs (read-only, for other scripts)")]
    public float loudness;
    public bool isSpeaking;
    public float speakingDuration;

    [Header("Config")]
    public bool isSignalSource = true;  // set TRUE only on mobile player
    public float loudnessThreshold = 0.02f;

    private bool wasSpeaking;
    private float speakingStart;

    void Update()
    {
        if (!isSignalSource) return;                 // ignore on VR side
        if (recorder == null || recorder.LevelMeter == null) return;

        loudness = recorder.LevelMeter.CurrentAvgAmp;
        isSpeaking = loudness > loudnessThreshold && recorder.IsCurrentlyTransmitting;

        if (isSpeaking)
        {
            if (!wasSpeaking)
            {
                wasSpeaking = true;
                speakingStart = Time.time;
            }
            speakingDuration = Time.time - speakingStart;
        }
        else
        {
            if (wasSpeaking)
            {
                // utterance finished -> here you can send an RPC / update Fusion
                // with loudness & speakingDuration
            }
            wasSpeaking = false;
            speakingDuration = 0f;
        }
    }
}
