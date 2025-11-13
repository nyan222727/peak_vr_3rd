using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Fusion;

public class Main_NetFunction : NetworkBehaviour
{
    public static Main_NetFunction Local;

    [Header("Effect Prefab for P2")]
    [SerializeField] private NetworkPrefabRef effectPrefab;
    [SerializeField] private Vector3 spawnOffset = new Vector3(0, 0, 2f);



    // Start is called before the first frame update
    void Start()
    {

    }
    public override void Spawned()
    {
        // 只有本地玩家才設定 Local
        if (Object.HasInputAuthority)
        {
            Local = this;
            Debug.Log("這是本地玩家 → 設定 Local 成功");
        }
        else
        {
            Debug.Log("遠端玩家 Spawn");
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
    public void OnPressSummonButton()
    {
        // 只允許本機玩家觸發
        if (!Object.HasInputAuthority)
        {
            print("don't has input authority");
            return;
        }

        RPC_SpawnEvent();
    }


    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SpawnEvent( RpcInfo info = default)
    {
        Vector3 spawnPos = transform.position + transform.rotation * spawnOffset;
        Quaternion spawnRot = transform.rotation;
        // 在 StateAuthority 上 Spawn，Fusion 會自動同步給所有 Client
        var obj = Runner.Spawn(effectPrefab, spawnPos, spawnRot, Object.InputAuthority);
        //Runner.Despawn(obj);
    }
}
