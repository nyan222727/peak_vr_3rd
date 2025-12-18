using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioTest : MonoBehaviour
{
    public GBAudioPlayer Music, EffectSound;

    int au = 0;
    // Start is called before the first frame update
    void Start()
    {
        au = 0;
    }

    public void PlayAU(int s)
    {
        switch (s)
        {
            case 0:
                Music.PlayBackgroundAudio(0, 1, true);
                break;
            case 1:
                Music.SetMusic(0);
                break;
            case 2:
                Music.SetMusic(1);
                break;
            case 3:
                Music.SetMusic(2);
                break;
            case 4:
                Music.SetMusic(3);
                break;
            case 5:
                Music.SetMusic(4);
                break;
        }
    }
    public void PlayAC()
    {
        Debug.Log(au);
        EffectSound.PlayEffectSound(au);

        if (au < EffectSound.AC.Length - 1)
        {
            au++;
        }
        else
        {
            au = 0;
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
