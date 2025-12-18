using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GBAudioPlayer : MonoBehaviour
{

    //[Header("Value")]
    //public int sound_type;
    //public int sound_page;
    //string soundPath;
    //string SoundEffectName;
    [Header("AU")]
    public AudioSource AU;
    public AudioClip[] AC;
    // Use this for initialization

    private void Awake()
    {
        if (GetComponent<AudioSource>())
        {
            AU = GetComponent<AudioSource>();
        }
    }

    void Start()
    {

    }

    // Update is called once per frame


    //public IEnumerator GetSoud_file()
    //{
    //    soundPath = Application.streamingAssetsPath + "/Sound/";
    //    string file_type_Name = "";

    //    switch (sound_type)
    //    {
    //        case 0:
    //            file_type_Name = "BGM";
    //            break;
    //        case 1:
    //            file_type_Name = "White_OS";
    //            break;
    //        case 2:
    //            file_type_Name = "Girl_VO";
    //            break;
    //        case 3:
    //            file_type_Name = "_SFX";
    //            break;
    //        case 4:
    //            file_type_Name = "BGM";
    //            break;
    //        case 5:
    //            break;
    //        case 6:
    //            break;
    //    }

    //    WWW request = GetAudioFromFile(soundPath, SoundEffectName + "_Broke" + ".wav");
    //    yield return request;
    //    AC[0] = request.GetAudioClip();


    //}
    //private WWW GetAudioFromFile(string path, string filename)
    //{
    //    string audioToLoad = string.Format(path + "{0}", filename);
    //    WWW request = new WWW(audioToLoad);
    //    return request;
    //}
    public void PlayBackgroundAudio(int n, float v, bool loop)
    {
        AU.Stop();
        AU.volume = v;
        AU.clip = AC[n];
        AU.loop = loop;
        AU.Play();
    }
    public void StopPlaying()
    {
        AU.Stop();
    }
    public void setAudio2D_3D(float d)
    {
        AU.spatialBlend = d;
    }

    public void SetVolume(float V)
    {
        if (V > 1)
        {
            V = 1;
        }
        AU.volume = V;
    }
    public void SetMusic(int state)
    {
        switch (state)
        {
            case 0://播放
                AU.Play();
                break;
            case 1://暫停
                AU.Pause();
                break;

            case 2://停止

                AU.Stop();
                break;
            case 3://靜音
                AU.mute = true;
                break;
            case 4://取消靜音
                AU.mute = false;
                break;

        }
    }
    public void PlayEffectSound(int n)
    {
        if (AC[n] != null)
            AU.PlayOneShot(AC[n]);
    }

    public IEnumerator FadeToMute_louder(int n, float time, float max)
    {
        float t = 0;
        switch (n)
        {
            case 0: // 變大聲
                while (t < time)
                {
                    t += Time.deltaTime;
                    SetVolume((t / time) * max);
                    yield return new WaitForSeconds(Time.deltaTime);
                }
                break;
            case 1: // 變小聲
                while (t < time)
                {
                    t += Time.deltaTime;
                    SetVolume(max - ((t / time) * max));
                    yield return new WaitForSeconds(Time.deltaTime);
                }
                break;
            case 2:
                while (t < time)
                {
                    t += Time.deltaTime;
                    SetVolume(max - ((t / time) * max * 0.2f));
                    yield return new WaitForSeconds(Time.deltaTime);
                }
                break;
            case 3:
                break;
        }

    }
}
