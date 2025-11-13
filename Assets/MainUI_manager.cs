using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainUI_manager : MonoBehaviour
{

    public Button summonButton;
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(WaitForPlayer());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    IEnumerator WaitForPlayer()
    {
        // 等到本地玩家出現
        while (Main_NetFunction.Local == null)
            yield return null;

        // 動態綁定按鈕事件
        summonButton.onClick.AddListener(() =>
        {
            Main_NetFunction.Local.OnPressSummonButton();
        });
    }
}
