using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.VisualScripting;
using UnityEngine;

public class MatchSystem : MonoBehaviour
{
    public static MatchSystem Instance { get; private set; }
    [SerializeField] private bool useDtls = false;
    private Lobby currentLobby;
    private CancellationTokenSource heartbeatCts;

    private bool isHostLobbyOwner; // 我是否為這個 lobby 的 owner（通常就是 host）
    private bool isMatching = false;
    private bool hasLoadedGameScene = false;

    public event Action OnLobbyListChanged;
    private List<Lobby> cachedLobbies = new();
    private CancellationTokenSource refreshCts;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    async void Start()
    {
        Debug.Log("[配對系統] 開始初始化 Unity Services...");

        try
        {
            await UnityServices.InitializeAsync();
            Debug.Log("[配對系統] Unity Services 初始化完成");

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                Debug.Log("[配對系統] 尚未登入，開始匿名登入...");
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log($"[配對系統] 匿名登入成功，PlayerId={AuthenticationService.Instance.PlayerId}");
            }
            else
            {
                Debug.Log($"[配對系統] 已登入，PlayerId={AuthenticationService.Instance.PlayerId}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[配對系統] 初始化失敗：{e}");
        }

        RegisterNetcodeCallbacks();
        StartLobbyRefreshLoop();
    }

    private async Task RefreshLobby()
    {
        var result = await LobbyService.Instance.QueryLobbiesAsync(new QueryLobbiesOptions
        {
            Count = 20
        });

        // 如果內容沒變 → 不通知 UI
        if (IsSameList(result.Results, cachedLobbies))
            return;

        cachedLobbies = result.Results;
        OnLobbyListChanged?.Invoke();
    }
    public List<Lobby> GetCachedLobbies()
    {
        return cachedLobbies;
    }
    void StartLobbyRefreshLoop()
    {
        refreshCts = new CancellationTokenSource();
        _ = LobbyRefreshLoop(refreshCts.Token);
    }

    async Task LobbyRefreshLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await RefreshLobby();

            try
            {
                await Task.Delay(3000, ct); // 3秒一次（建議）
            }
            catch { }
        }
    }
    private bool IsSameList(List<Lobby> a, List<Lobby> b)
    {
        if (a.Count != b.Count) return false;

        for (int i = 0; i < a.Count; i++)
        {
            if (a[i].Id != b[i].Id ||
                a[i].AvailableSlots != b[i].AvailableSlots)
                return false;
        }

        return true;
    }

    void RegisterNetcodeCallbacks()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogWarning("[配對系統] 找不到 NetworkManager.Singleton，無法註冊連線事件（請確認場景中有 NetworkManager）");
            return;
        }

        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;

        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

        Debug.Log("[配對系統] 已註冊 NGO 連線事件 (OnClientConnected/OnClientDisconnected)");
    }

    private void OnClientConnected(ulong clientId)
    {
        // 這個 callback 在 Host/Client 都會觸發
        if (NetworkManager.Singleton.IsServer)
        {
            Debug.Log($"[配對系統] 有玩家連線成功 (Server視角) ClientId={clientId}，目前連線數={NetworkManager.Singleton.ConnectedClients.Count}");
        }
        else if (NetworkManager.Singleton.IsClient && clientId == NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log($"[配對系統] 我已連線到 Host 成功 (Client視角) LocalClientId={NetworkManager.Singleton.LocalClientId}");
        }
        else
        {
            Debug.Log($"[配對系統] OnClientConnected 觸發 ClientId={clientId}");
        }

        if (NetworkManager.Singleton.IsServer)
        {
            int count = NetworkManager.Singleton.ConnectedClients.Count;
            Debug.Log($"[配對系統] (Host) 目前連線人數={count}/2");

            if (!hasLoadedGameScene && count == 2)
            {
                hasLoadedGameScene = true; 
                Debug.Log("[配對系統] (Host) 配對成功(2/2)，開始切換到 GameScene...");

                // 讓所有 Client 跟著一起切
                NetworkManager.Singleton.SceneManager.LoadScene("GameScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
            }
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.LogWarning($"[配對系統] 玩家斷線 ClientId={clientId}");

        // 如果我是 client 且自己被斷線，可視需求自動清理
        if (NetworkManager.Singleton != null &&
            NetworkManager.Singleton.IsClient &&
            clientId == NetworkManager.Singleton.LocalClientId)
        {
            Debug.LogWarning("[配對系統] 我被斷線了，將嘗試清理 Lobby/Relay...");
            _ = DisconnectAndCleanup();
        }
    }

    // 一鍵 QuickMatch（自動配對）
    public async void QuickMatch()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            Debug.LogWarning("[配對系統] 偵測到目前仍在連線中，先執行清理再重新配對...");
            await DisconnectAndCleanup();
        }

        if (isMatching) { Debug.LogWarning("[配對系統] 配對中，請稍候"); return; }
        isMatching = true;
        try
        {
            Debug.Log("[配對系統] 開始 QuickMatch：查詢可加入房間...");

            var queryOptions = new QueryLobbiesOptions
            {
                Count = 10,
                Filters = new List<QueryFilter>
                {
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT),
                }
            };

            var query = await LobbyService.Instance.QueryLobbiesAsync(queryOptions);
            Debug.Log($"[配對系統] 查詢完成，可加入房間數量={query.Results.Count}");

            if (query.Results.Count > 0)
            {
                var target = query.Results[0];
                Debug.Log($"[配對系統] 找到房間，嘗試加入 LobbyId={target.Id} Name={target.Name}");

                currentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(target.Id);
                OnLobbyListChanged?.Invoke(); // 通知 UI 刷新房間列表（如果有的話）
                isHostLobbyOwner = false;

                Debug.Log($"[配對系統] 加入 Lobby 成功 LobbyId={currentLobby.Id} OwnerId={currentLobby.HostId}");

                if (!currentLobby.Data.ContainsKey("joinCode"))
                    throw new Exception("[配對系統] Lobby 資料缺少 joinCode，無法加入 Relay");

                string joinCode = currentLobby.Data["joinCode"].Value;
                Debug.Log($"[配對系統] 取得 joinCode 成功：{joinCode}，開始加入 Relay...");

                await JoinRelay(joinCode);

                Debug.Log("[配對系統] 已呼叫 StartClient()，等待 NGO 連線成功事件...");
                return;
            }

            Debug.Log("[配對系統] 沒有可加入房間，改為建立 Lobby + Relay 並成為 Host");
            await CreateLobbyAndHost();
        }
        catch (Exception e)
        {
            Debug.LogError($"[配對系統] QuickMatch 失敗：{e}");
        }
        finally { isMatching = false; }
    }

    // 建立 Lobby + Relay（Host）
    async Task CreateLobbyAndHost()
    {
        Debug.Log("[配對系統] Host 流程：建立 Relay Allocation...");

        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(1);
        Debug.Log("[配對系統] Relay Allocation 建立完成");

        string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        Debug.Log($"[配對系統] Relay JoinCode 取得完成：{joinCode}");

        var options = new CreateLobbyOptions
        {
            Data = new Dictionary<string, DataObject>
            {
                { "joinCode", new DataObject(DataObject.VisibilityOptions.Member, joinCode) }
            }
        };

        string playerId = AuthenticationService.Instance.PlayerId;
        string shortId = playerId.Substring(0, 4);
        string roomName = $"Room_{shortId}";

        currentLobby = await LobbyService.Instance.CreateLobbyAsync(roomName, 2, options);
        OnLobbyListChanged?.Invoke(); // 通知 UI 刷新房間列表（如果有的話）
        isHostLobbyOwner = true;

        var ui = FindFirstObjectByType<StartUIManager>();

        if (ui != null)
        {
            ui.UpdateRoomDataText(
                $"Room Created\nName: {currentLobby.Name}\nID: {currentLobby.Id}"
            );
        }
        Debug.Log($"[配對系統] Lobby 建立成功 LobbyId={currentLobby.Id} Name={currentLobby.Name} OwnerId={currentLobby.HostId}");

        // Host needs heartbeat to keep lobby alive
        StartLobbyHeartbeat(currentLobby.Id);
        Debug.Log("[配對系統] 已啟動 Lobby Heartbeat（房間保活）");

        SetupHostRelay(allocation);
        Debug.Log("[配對系統] Host Relay 設定完成，準備 StartHost()...");

        bool ok = NetworkManager.Singleton.StartHost();
        Debug.Log(ok
            ? "[配對系統] StartHost() 呼叫成功，等待 Client 連線..."
            : "[配對系統] StartHost() 呼叫失敗（請確認 NetworkManager / Transport 設定）");
    }

    // 加入 Relay（Client）
    public async Task JoinRelay(string joinCode)
    {
        Debug.Log("[配對系統] Client 流程：JoinAllocation(joinCode)...");
        JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
        Debug.Log("[配對系統] JoinAllocation 成功，設定 UnityTransport Relay 資料...");

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetRelayServerData(
            allocation.RelayServer.IpV4,
            (ushort)allocation.RelayServer.Port,
            allocation.AllocationIdBytes,
            allocation.Key,
            allocation.ConnectionData,
            allocation.HostConnectionData,
            useDtls
        );

        Debug.Log("[配對系統] Relay 資料設定完成，準備 StartClient()...");
        bool ok = NetworkManager.Singleton.StartClient();
        Debug.Log(ok
            ? "[配對系統] StartClient() 呼叫成功，等待連線成功事件..."
            : "[配對系統] StartClient() 呼叫失敗（請確認 NetworkManager / Transport 設定）");
    }

    public async void JoinLobbyById(string lobbyId)
    {
        try
        {
            Debug.Log($"[配對系統] 嘗試手動加入 Lobby: {lobbyId}");

            currentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId);
            isHostLobbyOwner = false;

            string joinCode = currentLobby.Data["joinCode"].Value;

            await JoinRelay(joinCode);
            OnLobbyListChanged?.Invoke(); // 通知 UI 刷新房間列表（如果有的話）
        }
        catch (Exception e)
        {
            Debug.LogError($"[配對系統] 加入房間失敗: {e}");
        }
    }

    // 設定 Host Relay
    void SetupHostRelay(Allocation allocation)
    {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetRelayServerData(
            allocation.RelayServer.IpV4,
            (ushort)allocation.RelayServer.Port,
            allocation.AllocationIdBytes,
            allocation.Key,
            allocation.ConnectionData,
            allocation.ConnectionData,
            useDtls
        );
    }

    void StartLobbyHeartbeat(string lobbyId)
    {
        StopLobbyHeartbeat();
        heartbeatCts = new CancellationTokenSource();
        _ = HeartbeatLoop(lobbyId, heartbeatCts.Token);
    }

    public async void HostGame()
    {
        try
        {
            Debug.Log("[配對系統] 手動建立房間...");

            await DisconnectAndCleanup(); // 保險

            await CreateLobbyAndHost();
        }
        catch (Exception e)
        {
            Debug.LogError($"[配對系統] Host 失敗: {e}");
        }
    }

    async Task HeartbeatLoop(string lobbyId, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await LobbyService.Instance.SendHeartbeatPingAsync(lobbyId);
                Debug.Log("[配對系統] Heartbeat：Lobby 保活成功");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[配對系統] Heartbeat 失敗：{e.Message}");
            }

            try { await Task.Delay(TimeSpan.FromSeconds(15), ct); }
            catch { }
        }
    }

    void StopLobbyHeartbeat()
    {
        if (heartbeatCts != null)
        {
            heartbeatCts.Cancel();
            heartbeatCts.Dispose();
            heartbeatCts = null;
        }
    }

    // ====== 退出連線 + 清理 Lobby（給 UI 按鈕呼叫）======
    public async void ExitMatch()
    {
        await DisconnectAndCleanup();
    }

    // 退出連線，並刪除/離開 Lobby（避免「沒使用的房間」殘留）
    public async Task DisconnectAndCleanup()
    {
        Debug.Log("[配對系統] 開始退出連線與清理...");

        // 1) 停止 heartbeat
        StopLobbyHeartbeat();

        // 2) 先停止 NGO
        try
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                Debug.Log("[配對系統] 正在關閉 NGO 連線 (NetworkManager.Shutdown)...");
                NetworkManager.Singleton.Shutdown();
                Debug.Log("[配對系統] NGO 已呼叫 Shutdown()");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[配對系統] Shutdown NGO 失敗：{e.Message}");
        }

        // 3) 清 Lobby：Host 刪除、Client 離開
        try
        {
            if (currentLobby != null)
            {
                string lobbyId = currentLobby.Id;
                string playerId = AuthenticationService.Instance.PlayerId;

                if (isHostLobbyOwner)
                {
                    Debug.Log($"[配對系統] 我是 Host/Owner，準備刪除 Lobby：{lobbyId}");
                    await LobbyService.Instance.DeleteLobbyAsync(lobbyId);
                    Debug.Log("[配對系統] Lobby 已刪除（避免殘留無用房間）");
                }
                else
                {
                    Debug.Log($"[配對系統] 我是 Client，準備離開 Lobby：{lobbyId}");
                    await LobbyService.Instance.RemovePlayerAsync(lobbyId, playerId);
                    Debug.Log("[配對系統] 已離開 Lobby");
                }

                currentLobby = null;
                OnLobbyListChanged?.Invoke(); // 通知 UI 刷新房間列表（如果有的話）
                isHostLobbyOwner = false;
            }
            else
            {
                Debug.Log("[配對系統] currentLobby 為 null，無需清理 Lobby");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[配對系統] 清理 Lobby 失敗（忽略）：{e.Message}");
        }
        hasLoadedGameScene = false;
        Debug.Log("[配對系統] 退出/清理流程完成");

        FindFirstObjectByType<StartUIManager>()?.ClearRoomData();
    }

    private void OnApplicationQuit()
    {
        // 這裡不要 await，做 best-effort
        _ = DisconnectAndCleanup();
    }
}