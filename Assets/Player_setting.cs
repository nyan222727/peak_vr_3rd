using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class Player_setting : SimulationBehaviour, IPlayerJoined
{
    public NetworkRunner runner;
    public GameObject PlayerPrefab;

    public void PlayerJoined(PlayerRef player)
    {
        if (player == runner.LocalPlayer)
        {
            runner.Spawn(PlayerPrefab, new Vector3(0, 1, 0), Quaternion.identity, player);
        }

    }
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
