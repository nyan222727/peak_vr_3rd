using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Fusion.Menu;
using System.Threading.Tasks;
using TMPro;

public class BasicSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    public static BasicSpawner Instance { get; private set; }
    
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player){ }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player){ }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data){ }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress){ }

    [Header("Scene Management")]
    [SerializeField] private NetworkSceneManagerDefault sceneManager;
  
    [SerializeField] private NetworkPrefabRef _playerPrefab;
    private Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();

    public bool DebugMode = false;

    public NetworkRunner runner { get; private set; }

    public TMP_InputField RoomName;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
    }

    void Start()
    {
        if(RoomName) RoomName.text = "Lobby";
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space))
        {
            QuickStart();
        }
    }

    private async void StartGame()
    {
        // Launch the connection at start
        await Connect();
        if(DebugMode) await Connect(1);
    }

    public async Task Connect(int peerIndex = 0)
    {
        //runner = gameObject.AddComponent<NetworkRunner>();
        runner = gameObject.GetComponent<NetworkRunner>();
        runner.ProvideInput = false;
        runner.AddCallbacks(this);

        // Create the scene manager if it does not exist
        if (sceneManager == null) sceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>();

        // Create the NetworkSceneInfo from the current scene
        var scene = SceneRef.FromIndex(2);
        var sceneInfo = new NetworkSceneInfo();
        if (scene.IsValid) {
            sceneInfo.AddSceneRef(scene, LoadSceneMode.Single);
        }

        // Start or join (depends on gamemode) a session with a specific name
        var args = new StartGameArgs()
        {
            GameMode = GameMode.Shared,
            Scene = scene,
            SceneManager = sceneManager,
            SessionName = RoomName.text
        };

        await runner.StartGame(args);
    }

    public void QuickStart()
    {
        StartGame();
    }
    //進到第二個場景，然後要加 Player 進去
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (player == runner.LocalPlayer && _playerPrefab != null)
        {
            Debug.Log(runner.LocalPlayer + "," + player);
            // Create a unique position for the player
            Vector3 spawnPosition = new Vector3(0, 1, 0);
            //NetworkObject networkPlayerObject = runner.Spawn(_playerPrefab, spawnPosition, Quaternion.identity, player);
            NetworkObject networkPlayerObject = runner.Spawn(_playerPrefab, spawnPosition, Quaternion.identity, player, (runner, obj) => {
            });
            // Keep track of the player avatars for easy access
            _spawnedCharacters.Add(player, networkPlayerObject);
        }
    }

    public void OnConnectedToServer(NetworkRunner r) {
    Debug.Log("OnConnectedToServer");
    }
    public void OnDisconnectedFromServer(NetworkRunner r, NetDisconnectReason reason) {
    Debug.LogError($"OnDisconnectedFromServer: {reason}");
    }
    public void OnConnectFailed(NetworkRunner r, NetAddress addr, NetConnectFailedReason reason) {
    Debug.LogError($"OnConnectFailed: {reason}");
    }
    public void OnConnectRequest(NetworkRunner r, NetworkRunnerCallbackArgs.ConnectRequest req, byte[] token) {
    req.Accept();
    }
}