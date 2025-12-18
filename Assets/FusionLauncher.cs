using System.Collections.Generic;
using Fusion;
using Fusion.Photon.Realtime;
using Fusion.Sockets;
using UnityEngine;

public class FusionLauncher : MonoBehaviour, INetworkRunnerCallbacks
{
    public NetworkRunner runner;
    public NetworkPrefabRef playerPrefab;

    private readonly Dictionary<PlayerRef, NetworkObject> _spawned = new();


    public PhotonAppSettings photonAppSettingsAsset;

    private void Awake()
    {
        var others = FindObjectsOfType<NetworkRunner>();
        if (others.Length > 0 && runner != null && others[0] != runner)
        {
            Destroy(gameObject);
            return;
        }

        if (runner == null) runner = GetComponent<NetworkRunner>() ?? gameObject.AddComponent<NetworkRunner>();
        DontDestroyOnLoad(gameObject);
    }
    public void Start()
    {
        //StartAsHost();   // 先自動起 Host，確認能連線
        //ValidateConfig();
    }

    // ===== 你自己的啟動流程（Host/Client） =====
    public async void StartAsHost() => await StartRunner(GameMode.Host);
    public async void StartAsClient() => await StartRunner(GameMode.Client);

    private async System.Threading.Tasks.Task StartRunner(GameMode mode)
    {
        if (!runner) runner = GetComponent<NetworkRunner>() ?? gameObject.AddComponent<NetworkRunner>();
        if (!GetComponent<NetworkSceneManagerDefault>()) gameObject.AddComponent<NetworkSceneManagerDefault>();

        runner.ProvideInput = false;
        runner.AddCallbacks(this);

        var result = await runner.StartGame(new StartGameArgs
        {
            GameMode = mode,
            SessionName = "HippoRoom",
            SceneManager = GetComponent<NetworkSceneManagerDefault>()
        });

        Debug.Log($"StartGame Ok={result.Ok}, Reason={result.ShutdownReason}");
    }
    void ValidateConfig()
    {
        var inst = photonAppSettingsAsset;
        Debug.Log(inst ? "Found PhotonAppSettings" : "PhotonAppSettings MISSING");
        if (inst)
        {
            var a = inst.AppSettings;
            Debug.Log($"AppIdFusion: {(string.IsNullOrEmpty(a.AppIdFusion) ? "<EMPTY>" : a.AppIdFusion)} | FixedRegion: {a.FixedRegion}");
        }
        var npc = NetworkProjectConfig.Global;
        Debug.Log(npc);

    }

    // ===== 必填：Fusion 2 版 callbacks 簽名 =====
    public void OnConnectedToServer(NetworkRunner r) =>
        Debug.Log("✅ ConnectedToServer");

    public void OnDisconnectedFromServer(NetworkRunner r, NetDisconnectReason reason) =>
        Debug.LogWarning($"⚠️ Disconnected: {reason}");

    public void OnConnectFailed(NetworkRunner r, NetAddress addr, NetConnectFailedReason reason) =>
        Debug.LogError($"❌ ConnectFailed: {reason}");

    public void OnConnectRequest(NetworkRunner r, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
        // 視需求驗證；先全部接受
        request.Accept();
    }

    public void OnPlayerJoined(NetworkRunner r, PlayerRef player)
    {
        Debug.Log($"PlayerJoined {player}");
        if (r.IsServer)
        {
            var pos = new Vector3((player.RawEncoded % 4) * 2f, 0f, 0f);
            var obj = r.Spawn(playerPrefab, pos, Quaternion.identity, player);
            _spawned[player] = obj;
        }
    }

    public void OnPlayerLeft(NetworkRunner r, PlayerRef player)
    {
        if (_spawned.TryGetValue(player, out var obj))
        {
            r.Despawn(obj);
            _spawned.Remove(player);
        }
    }

    public void OnInput(NetworkRunner r, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner r, PlayerRef player, NetworkInput input) { }
    public void OnUserSimulationMessage(NetworkRunner r, SimulationMessagePtr msg) { }
    public void OnSessionListUpdated(NetworkRunner r, List<SessionInfo> sessions) { }
    public void OnCustomAuthenticationResponse(NetworkRunner r, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner r, HostMigrationToken token)
    {
        print("Host!!!");
    }
    public void OnReliableDataReceived(NetworkRunner r, PlayerRef player, ReliableKey key, System.ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner r, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadStart(NetworkRunner r) { }
    public void OnSceneLoadDone(NetworkRunner r) { }
    public void OnObjectEnterAOI(NetworkRunner r, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner r, NetworkObject obj, PlayerRef player) { }

    // 仍然可留著（有些版本會呼叫）
    public void OnShutdown(NetworkRunner r, ShutdownReason reason) =>
        Debug.LogWarning($"RunnerShutdown: {reason}");
}
