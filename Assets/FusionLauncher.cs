using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System;
using UnityEngine.UI;

public class FusionLauncher : MonoBehaviour, INetworkRunnerCallbacks
{
    public NetworkRunner runnerPrefab;
    private NetworkRunner _runner;

    [Header("Player Spawn")]
    public NetworkObject playerPrefab;
    public Transform[] spawnPoints;
    private Dictionary<PlayerRef, NetworkObject> _spawnedPlayers = new();

    [Header("UI Panels")]
    public GameObject panelMain;
    public GameObject panelRoomList;
    public GameObject panelInRoom;

    [Header("InRoom UI")]
    public Text roomTitleText;
    public Text roleText;
    public Text playerCountText;
    public Button leaveButton;

    [Header("UI Controls")]
    public InputField roomNameInput;
    public Button hostButton;
    public Button refreshButton;

    public Button showRoomListButton;

    [Header("Room List")]
    public Transform roomListParent;
    public GameObject roomItemPrefab;

    private readonly Dictionary<string, SessionInfo> _sessions = new();

    enum UIState { Main, Lobby, InRoom }
    void SetUI(UIState s)
    {
        panelMain.SetActive(s == UIState.Main);
        panelRoomList.SetActive(s == UIState.Lobby);
        panelInRoom.SetActive(s == UIState.InRoom);
    }

    async void Start()
    {
        showRoomListButton.onClick.AddListener(() =>
        {
            SetUI(UIState.Lobby);
        });

        hostButton.onClick.AddListener(OnClickHost);
        refreshButton.onClick.AddListener(() => RedrawRoomList());

        leaveButton.onClick.AddListener(LeaveRoom);

        SetUI(UIState.Main);

        await EnsureRunner();
        await JoinLobby();
    }

    void UpdateInRoomUI(NetworkRunner runner)
    {
        if (runner == null) return;
        if (roomTitleText == null || roleText == null || playerCountText == null) return;

        Debug.Log("更新人數囉!!!");
        // SessionInfo 可能還沒 ready，先做防呆
        var sessionName = (runner.SessionInfo != null) ? runner.SessionInfo.Name : "(no session)";
        roomTitleText.text = $"Room: {sessionName}";
        roleText.text = runner.IsServer ? "Role: Host" : "Role: Client";

        int count = -1;
        if (runner.SessionInfo != null) count = runner.SessionInfo.PlayerCount;

        if (count < 0)
        {
            count = 0;
            foreach (var _ in runner.ActivePlayers) count++;
        }
        playerCountText.text = $"Players: {count}";
    }

  async System.Threading.Tasks.Task EnsureRunner()
{
    if (_runner != null) return;

    var existing = FindObjectOfType<NetworkRunner>();
    if (existing != null)
    {
        _runner = existing;
        _runner.AddCallbacks(this);
        _runner.ProvideInput = true;

        Debug.Log($"[Main_FusionLauncher] Using existing Runner: {_runner.name}");
        return;
    }

    _runner = Instantiate(runnerPrefab);
    _runner.name = "NetworkRunner";
    _runner.AddCallbacks(this);
    _runner.ProvideInput = true;
    DontDestroyOnLoad(_runner.gameObject);

    Debug.Log($"[Main_FusionLauncher] Spawned Runner: {_runner.name}");
    await System.Threading.Tasks.Task.Yield();
}


    async System.Threading.Tasks.Task JoinLobby()
    {
        var result = await _runner.JoinSessionLobby(SessionLobby.Shared);
        Debug.Log($"JoinLobby: {result}");
    }

    async void OnClickHost()
    {
        string roomName = string.IsNullOrWhiteSpace(roomNameInput.text)
       ? $"Room_{UnityEngine.Random.Range(1000, 9999)}"
       : roomNameInput.text.Trim();

        var args = new StartGameArgs
        {
            GameMode = GameMode.Host,   // ✅ 改成 Host
            SessionName = roomName,
        };

        var result = await _runner.StartGame(args);
        Debug.Log($"Host StartGame: {result}");

        if (result.Ok)
        {
            SetUI(UIState.InRoom);
            UpdateInRoomUI(_runner);
        }
    }

    async void JoinRoom(string roomName)
    {
        var args = new StartGameArgs
        {
            GameMode = GameMode.Client, // ✅ 改成 Client
            SessionName = roomName,
        };

        var result = await _runner.StartGame(args);
        Debug.Log($"Client Join StartGame: {result}");

        if (result.Ok)
        {
            SetUI(UIState.InRoom);
            UpdateInRoomUI(_runner);
        }
    }

    void RedrawRoomList()
    {
        foreach (Transform child in roomListParent)
            Destroy(child.gameObject);

        foreach (var kv in _sessions)
        {
            var info = kv.Value;

            var go = Instantiate(roomItemPrefab, roomListParent);
            var txt = go.GetComponentInChildren<Text>();
            var btn = go.GetComponentInChildren<Button>();

            txt.text = $"{info.Name} ({info.PlayerCount}/{info.MaxPlayers})";
            btn.onClick.AddListener(() => JoinRoom(info.Name));
        }
    }

    // === Lobby 房間清單更新 ===
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        _sessions.Clear();
        foreach (var s in sessionList)
            _sessions[s.Name] = s;

        // 只更新 UI，不換 Scene
        RedrawRoomList();
    }

    // === 進房/離房後，你也可以在這裡切 UI ===
    public void OnConnectedToServer(NetworkRunner runner) { }
    // public void OnDisconnectedFromServer(NetworkRunner runner) { SetUI(UIState.Lobby); }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.Log($"Disconnected: {reason}");
        SetUI(UIState.Lobby);
    }

    // 其他 callbacks 先略（照你原本的即可）
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"[Main_FusionLauncher] OnPlayerJoined: {player}  active={CountActive(runner)}");
        UpdateInRoomUI(runner);
        if (!runner.IsServer) return; // ✅ 只有 Host Spawn

        int idx = _spawnedPlayers.Count % (spawnPoints != null && spawnPoints.Length > 0 ? spawnPoints.Length : 1);
        Vector3 pos = (spawnPoints != null && spawnPoints.Length > 0) ? spawnPoints[idx].position : Vector3.zero;
        Quaternion rot = (spawnPoints != null && spawnPoints.Length > 0) ? spawnPoints[idx].rotation : Quaternion.identity;

        var obj = runner.Spawn(playerPrefab, pos, rot, player);
        _spawnedPlayers[player] = obj;
        UpdateInRoomUI(runner);

    }
    int CountActive(NetworkRunner runner)
    {
        int c = 0;
        foreach (var _ in runner.ActivePlayers) c++;
        return c;
    }


    void LeaveRoom()
    {
        if (_runner == null) return;

        _runner.Shutdown();
        SetUI(UIState.Lobby);
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"[Main_FusionLauncher] OnPlayerLeft: {player}  active={CountActive(runner)}");
        UpdateInRoomUI(runner);

        if (!runner.IsServer) return;

        if (_spawnedPlayers.TryGetValue(player, out var obj))
        {
            runner.Despawn(obj);
            _spawnedPlayers.Remove(player);
        }

    }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { SetUI(UIState.Lobby); }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }




    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }

}
