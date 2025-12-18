using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GostMain : MonoBehaviour
{


    [Header("Light")]
    public Light mainTableLight;
    public float intensity_reguler, intensity_table;
    [Header("Function")]
    public UI_Manager UI_M;
    public RagdollSwitcher RD_S;


    //[Header("Camera controll")]
    // public GameObject TableView_obj;

    //public Camera TableCamera;
    //public RawImage table_camera_image;
    //public Transform[] CamPos;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            RD_S.TriggerHang();
            UI_M.PlayEnding(false);
        }
        if (Input.GetKeyDown(KeyCode.G))
        {
            UI_M.PlayEnding(true);

        }
    }

    public void SwitchStateG(int n)
    {

        switch (n)
        {
            case 0: //回初始畫面Menu
                mainTableLight.intensity = intensity_reguler;
                UI_M.FadeToState(0);
                break;

            case 1:  //從MENU 進入主畫面
                mainTableLight.intensity = intensity_reguler;
                UI_M.FadeToState(1);
                break;
            case 2://回主要互動畫面
                mainTableLight.intensity = intensity_reguler;
                UI_M.SetUIActives(0);
                break;
            case 3://進入碟仙
                mainTableLight.intensity = intensity_table;
                UI_M.SetUIActives(1);
                break;
            case 4:
                break;

        }
    }
}
