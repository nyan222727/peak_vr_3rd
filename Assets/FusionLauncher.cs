using System.Collections;   
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

    [Header("Connection UI")]
    public Button connectButton;
    public Text connectionStatusText;
    private bool _lobbyJoined = false;


    [Header("Player Spawn")]
    public NetworkObject playerPrefab;
    public Transform[] spawnPoints;
    private Dictionary<PlayerRef, NetworkObject> _spawnedPlayers = new();

    [Header("Room RPC Manager")]
    public NetworkObject roomRpcManagerPrefab;   // ← Inspector 指定
    private NetworkObject _roomRpcManager;        // ← runtime 保存

    private JumpScareButton _jumpScare;

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

    public Button readyButton;   // Inspector 指到「準備」按鈕
    private bool _inRoom = false;

    private Coroutine _roomListUpdateCo;



    List<SessionInfo> _cachedRooms = new();

    private readonly Dictionary<string, SessionInfo> _sessions = new();

    enum UIState { Main, Lobby, InRoom }
    void SetUI(UIState s)
    {
       // panelMain.SetActive(s == UIState.Main);
        panelRoomList.SetActive(s == UIState.Lobby);
        panelInRoom.SetActive(s == UIState.InRoom);
    }

    async void Start()
    {
        //  Debug.Log($"[FusionLauncher] Start instanceID={GetInstanceID()} name={gameObject.name}");


        _inRoom = false;
        SetReadyButton(false);

        showRoomListButton.onClick.AddListener(() =>
        {
            SetUI(UIState.Lobby);
        });

        hostButton.onClick.AddListener(OnClickHost);
        refreshButton.onClick.AddListener(() => RedrawRoomList());
        leaveButton.onClick.AddListener(LeaveRoom);


        if (connectButton)
            connectButton.onClick.AddListener(OnClickConnect);


        SetUI(UIState.Main);

        // 一開始不連線
        SetConnectionStatus(false, "未連線");
        SetControlsInteractable(false);

        await EnsureRunner();
        //await JoinLobby();
    }
    void StartRoomListUpdate()
    {
        if (_roomListUpdateCo != null)
            StopCoroutine(_roomListUpdateCo);

        _roomListUpdateCo = StartCoroutine(RoomListUpdateCoroutine());
    }


    void StopRoomListUpdate()
    {
        if (_roomListUpdateCo != null)
        {
            StopCoroutine(_roomListUpdateCo);
            _roomListUpdateCo = null;
        }
    }

    IEnumerator RoomListUpdateCoroutine()
    {
        while (_runner != null && _lobbyJoined)
        {
            ClearRoomListUI();
            RedrawRoomList();   // 用你快取的 session list 畫 UI

            yield return new WaitForSeconds(3f);
        }

        _roomListUpdateCo = null;
        yield return new WaitForSeconds(0.1f);
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
    void SetConnectionStatus(bool ok, string msg)
    {
        if (connectionStatusText == null) return;
        connectionStatusText.text = msg;
        connectionStatusText.color = ok ? Color.green : Color.red;
    }

    void SetControlsInteractable(bool connected)
    {
        // 依你需求調整：沒連線時不給 Host/Join/Refresh
        if (hostButton) hostButton.interactable = connected;
        if (showRoomListButton) showRoomListButton.interactable = connected;
        if (refreshButton) refreshButton.interactable = connected;
    }

    async void OnClickConnect()
    {
        ClearRoomListUI();
        SetConnectionStatus(false, "連線中...");

        try
        {
            await EnsureRunner();
            var result = await _runner.JoinSessionLobby(SessionLobby.Shared);
            Debug.Log($"JoinLobby: {result}");

            _lobbyJoined = result.Ok;

            if (_lobbyJoined)
            {
                SetConnectionStatus(true, "連線成功");
                SetControlsInteractable(true);

                SetConnectButton(false);   // ✅ 連線成功 → 關閉連線按鈕
                ClearRoomListUI();
                RedrawRoomList();
                SetUI(UIState.Lobby);

                StartRoomListUpdate();   // ✅ 開始每 3 秒更新房間清單
                
                // 連上 lobby 後可切到房間列表（看你要不要）
                // SetUI(UIState.Lobby);
            }
            else
            {
                SetConnectionStatus(false, "連線失敗");
                SetControlsInteractable(false);
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            _lobbyJoined = false;
            SetConnectionStatus(false, "連線失敗");
            SetControlsInteractable(false);
        }
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
            Debug.Log($"[FusionLauncher] AddCallbacks DONE. runnerId={_runner.GetInstanceID()} launcherId={GetInstanceID()} enabled={isActiveAndEnabled}");
            return;
        }

        _runner = Instantiate(runnerPrefab);
        _runner.name = "NetworkRunner";
        _runner.AddCallbacks(this);
        _runner.ProvideInput = true;
        DontDestroyOnLoad(_runner.gameObject);
        Debug.Log($"[FusionLauncher] AddCallbacks DONE. runnerId={_runner.GetInstanceID()} launcherId={GetInstanceID()} enabled={isActiveAndEnabled}");

        await System.Threading.Tasks.Task.Yield();
    }


    async System.Threading.Tasks.Task JoinLobby()
    {
        _inRoom = false;
        SetReadyButton(false);  // ✅ 連線成功但未進房，準備不可按

        var result = await _runner.JoinSessionLobby(SessionLobby.Shared);
        Debug.Log($"JoinLobby: {result}");
    }

    async void OnClickHost()
    {

        if (_runner == null || !_lobbyJoined)
        {
            SetConnectionStatus(false, "未連線");
            return;
        }

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
            Debug.Log($"[Main_FusionLauncher] Using runner={_runner.name} id={_runner.GetInstanceID()}");
            // ✅ 只有 Host 才 Spawn（而且只 Spawn 一次）
        }

        Debug.Log($"[FusionLauncher] Spawn call instanceID={GetInstanceID()}");
    }

    async void JoinRoom(string roomName)
    {
        if (_runner == null || !_lobbyJoined)
        {
            SetConnectionStatus(false, "未連線");
            return;
        }

        var args = new StartGameArgs
        {
            GameMode = GameMode.Client, // ✅ 改成 Client
            SessionName = roomName,
        };

        var result = await _runner.StartGame(args);
        Debug.Log($"Client Join StartGame: {result}");

        if (result.Ok)
        {
            _inRoom = true;
            SetReadyButton(true);   // ✅ 進房成功，準備按鈕亮起

            SetUI(UIState.InRoom);
            UpdateInRoomUI(_runner);

            Debug.Log($"[Main_FusionLauncher] Using runner={_runner.name} id={_runner.GetInstanceID()}");
        }
        else
        {
            _inRoom = false;
            SetReadyButton(false);
        }
    }



    // === Lobby 房間清單更新 ===
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        _sessions.Clear();
        foreach (var s in sessionList)
            _sessions[s.Name] = s;

        _cachedRooms = sessionList;
        // 只更新 UI，不換 Scene
        RedrawRoomList();
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

    void ClearRoomListUI()
    {
        // 依你原本的房間清單容器命名調整
        // 例如：roomListRoot / roomListContent / roomListParent
        if (roomListParent == null) return;

        for (int i = roomListParent.childCount - 1; i >= 0; i--)
            Destroy(roomListParent.GetChild(i).gameObject);

    }

    void SetReadyButton(bool canInteract)
    {
        if (readyButton == null) return;
        readyButton.interactable = canInteract;
    }
    void SetConnectButton(bool show)
    {
        if (connectButton == null) return;
        connectButton.gameObject.SetActive(show);
    }


    // === 進房/離房後，你也可以在這裡切 UI ===
    public void OnConnectedToServer(NetworkRunner runner)
    {
        SetConnectionStatus(true, "連線成功");
        SetControlsInteractable(true);
    }
    // public void OnDisconnectedFromServer(NetworkRunner runner) { SetUI(UIState.Lobby); }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {

        StopRoomListUpdate();
        ClearRoomListUI();
        _lobbyJoined = false;
        _inRoom = false;
        SetReadyButton(false);
        SetConnectButton(true);    // ✅ 可重新連線

        SetConnectionStatus(false, "已斷線");
        SetControlsInteractable(false);
        SetUI(UIState.Main);
    }

    // 其他 callbacks 先略（照你原本的即可）
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"[Main_FusionLauncher] OnPlayerJoined: {player}  active={CountActive(runner)}");
        Debug.Log($"[Main_FusionLauncher] OnPlayerJoined fired. runner={runner.name} id={runner.GetInstanceID()} player={player}");

        UpdateInRoomUI(runner);
        if (!runner.IsServer) return; // ✅ 只有 Host Spawn
        if (player != runner.LocalPlayer) return;

        if (_roomRpcManager == null)
        {
            _roomRpcManager = runner.Spawn(roomRpcManagerPrefab, Vector3.zero, Quaternion.identity);
            _jumpScare = _roomRpcManager.GetComponentInChildren<JumpScareButton>();
        }

        int idx = _spawnedPlayers.Count % (spawnPoints != null && spawnPoints.Length > 0 ? spawnPoints.Length : 1);
        Vector3 pos = (spawnPoints != null && spawnPoints.Length > 0) ? spawnPoints[idx].position : Vector3.zero;
        Quaternion rot = (spawnPoints != null && spawnPoints.Length > 0) ? spawnPoints[idx].rotation : Quaternion.identity;
        var obj = runner.Spawn(playerPrefab, pos, rot, player);
        _spawnedPlayers[player] = obj;
        UpdateInRoomUI(runner);

    }
    public void UI_TriggerJumpScare()
    {
        if (_jumpScare == null)
        {
            // Client 端也可以用 Find 補抓（見下方）
            _jumpScare = FindObjectOfType<JumpScareButton>();
        }

        if (_jumpScare == null)
        {
            Debug.LogWarning("[FusionLauncher] JumpScareButton not ready yet.");
            return;
        }

        _jumpScare.TriggerJumpScare();
    }


    int CountActive(NetworkRunner runner)
    {
        int c = 0;
        foreach (var _ in runner.ActivePlayers) c++;
        return c;
    }
    async void LeaveRoom()
    {
        // 1) 先把房間清單清空（避免殘留）
        ClearRoomListUI();

        _inRoom = false;
        SetReadyButton(false);
        SetConnectButton(true);    // ✅ 可重新連線

        // 2) 把房內生成物清掉（如果你有字典）
        _spawnedPlayers?.Clear();

        if (_runner != null)
        {
            await _runner.Shutdown();
            _runner = null;
        }

        SetConnectionStatus(false, "未連線...");
        SetReadyButton(false);
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
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        StopRoomListUpdate();
        ClearRoomListUI();
        _lobbyJoined = false;

        _inRoom = false;
        SetReadyButton(false);
        SetConnectButton(true);    // ✅ 可重新連線

        SetConnectionStatus(false, "已離線");
        SetControlsInteractable(false);
        SetUI(UIState.Main);

    }
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
