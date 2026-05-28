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
using UnityEngine;
using UnityEngine.SceneManagement;

public enum MatchState
{
    Idle,
    Matching,
    Hosting,
    Joining,
    InGame,
    Disconnecting
}

public class MatchSystem : MonoBehaviour
{
    public static MatchSystem Instance { get; private set; }

    [Header("Relay")]
    [SerializeField] private bool useDtls = false;

    [Header("Lobby Refresh")]
    [SerializeField] private float refreshInterval = 3f;

    private Lobby currentLobby;

    private CancellationTokenSource heartbeatCts;
    private CancellationTokenSource refreshCts;

    private bool isHostLobbyOwner;
    private bool hasLoadedGameScene;
    private bool isCleaning;

    public MatchState CurrentState { get; private set; } = MatchState.Idle;

    public event Action<string> OnRoomCreated;
    public event Action OnLobbyListChanged;
    public event Action<MatchState> OnStateChanged;

    private List<Lobby> cachedLobbies = new();

    private bool isReady = false;

    #region Unity Lifecycle

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

    private async void Start()
    {
        await InitializeAndSetReady();
    }


    private void OnDestroy()
    {
        StopLobbyHeartbeat();

        refreshCts?.Cancel();
        refreshCts?.Dispose();

        UnregisterNetcodeCallbacks();
    }

    private void OnApplicationQuit()
    {
        // 不做 async cleanup
        // 只停止 heartbeat 即可
        StopLobbyHeartbeat();
    }

    #endregion

    #region Initialize

    private bool startSceneLoaded;

    private async Task InitializeAndSetReady()
    {
        try
        {
            await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

            RegisterNetcodeCallbacks();

            if (refreshCts == null)
                StartLobbyRefreshLoop();

            isReady = true;

            if (!startSceneLoaded)
            {
                startSceneLoaded = true;
                SceneManager.LoadScene("StartScene");
            }
        }
        catch (Exception e)
        {
            
            isReady = false;
        }
    }

    #endregion

    #region State

    private void SetState(MatchState state)
    {
        CurrentState = state;

        

        OnStateChanged?.Invoke(state);
    }

    #endregion

    #region Lobby Refresh

    private void StartLobbyRefreshLoop()
    {
        refreshCts = new CancellationTokenSource();

        _ = LobbyRefreshLoop(refreshCts.Token);
    }

    private async Task LobbyRefreshLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                // 避免遊戲中還一直刷新 Lobby
                if (CurrentState == MatchState.Idle ||
                    CurrentState == MatchState.Matching)
                {
                    await RefreshLobby();
                }

                await Task.Delay(
                    TimeSpan.FromSeconds(refreshInterval), ct);
            }
            catch (TaskCanceledException)
            {
                break;
            }
            catch (Exception e)
            {
                
            }
        }
    }

    private async Task RefreshLobby()
    {
        try
        {
            var result = await LobbyService.Instance.QueryLobbiesAsync(
                new QueryLobbiesOptions
                {
                    Count = 20,
                    Filters = new List<QueryFilter>
                    {
                        new QueryFilter(
                            QueryFilter.FieldOptions.AvailableSlots,
                            "0",
                            QueryFilter.OpOptions.GT),

                        new QueryFilter(
                            QueryFilter.FieldOptions.S1,
                            "waiting",
                            QueryFilter.OpOptions.EQ)
                    }
                });

            if (IsSameLobbyList(result.Results, cachedLobbies))
                return;

            cachedLobbies = result.Results;

            OnLobbyListChanged?.Invoke();
        }
        catch (Exception e)
        {
            
        }
    }

    private bool IsSameLobbyList(List<Lobby> a, List<Lobby> b)
    {
        if (a.Count != b.Count)
            return false;

        for (int i = 0; i < a.Count; i++)
        {
            if (a[i].Id != b[i].Id ||
                a[i].AvailableSlots != b[i].AvailableSlots)
            {
                return false;
            }
        }

        return true;
    }

    public List<Lobby> GetCachedLobbies()
    {
        return cachedLobbies;
    }

    #endregion

    #region Netcode

    private void RegisterNetcodeCallbacks()
    {
        if (NetworkManager.Singleton == null)
        {
            
            return;
        }

        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;

        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    }

    private void UnregisterNetcodeCallbacks()
    {
        if (NetworkManager.Singleton == null)
            return;

        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
    }

    private async void OnClientConnected(ulong clientId)
    {
        if (NetworkManager.Singleton.IsServer)
        {
            int count =
                NetworkManager.Singleton.ConnectedClients.Count;

            
            

            if (!hasLoadedGameScene && count == 2)
            {
                hasLoadedGameScene = true;

                SetState(MatchState.InGame);

                await SetLobbyState("ingame");

                

                NetworkManager.Singleton.SceneManager.LoadScene(
                    "GameScene",
                    UnityEngine.SceneManagement.LoadSceneMode.Single);
            }
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        

        if (NetworkManager.Singleton != null &&
            NetworkManager.Singleton.IsClient &&
            clientId == NetworkManager.Singleton.LocalClientId)
        {
            _ = DisconnectAndCleanup();
        }
    }

    #endregion

    #region Quick Match

    public async Task QuickMatch()
    {
        if (!isReady)
        {
            
            return;
        }

        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            
            await UnityServices.InitializeAsync();
        }

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        if (CurrentState != MatchState.Idle)
        {
            
            return;
        }

        SetState(MatchState.Matching);
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            
            await DisconnectAndCleanup();
        }

        try
        {
            

            var queryOptions = new QueryLobbiesOptions
            {
                Count = 10,
                Filters = new List<QueryFilter>
                {
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT),
                    new QueryFilter(QueryFilter.FieldOptions.S1, "waiting", QueryFilter.OpOptions.EQ),
                }
            };

            var query = await LobbyService.Instance.QueryLobbiesAsync(queryOptions);
            

            if (query.Results.Count > 0)
            {
                var target = query.Results[0];
                

                currentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(target.Id);
                OnLobbyListChanged?.Invoke(); // 通知 UI 刷新房間列表（如果有的話）
                isHostLobbyOwner = false;

                

                if (!currentLobby.Data.ContainsKey("joinCode"))
                    throw new Exception("[配對系統] Lobby 資料缺少 joinCode，無法加入 Relay");

                string joinCode = currentLobby.Data["joinCode"].Value;
                

                await JoinRelay(joinCode);

                
                return;
            }

            
            await CreateLobbyAndHost();
        }
        catch (Exception e)
        {
            
        }
    }

    #endregion

    #region Host

    public async Task HostGame()
    {
        if (CurrentState != MatchState.Idle)
            return;

        try
        {
            SetState(MatchState.Hosting);

            await DisconnectAndCleanup();

            await CreateLobbyAndHost();
        }
        catch (Exception e)
        {
            

            SetState(MatchState.Idle);
        }
    }

    private async Task CreateLobbyAndHost()
    {
        Allocation allocation =
            await WithTimeout(
                RelayService.Instance.CreateAllocationAsync(1));

        string joinCode =
            await WithTimeout(
                RelayService.Instance.GetJoinCodeAsync(
                    allocation.AllocationId));

        var options = new CreateLobbyOptions
        {
            Data = new Dictionary<string, DataObject>
            {
                {
                    "joinCode",
                    new DataObject(
                        DataObject.VisibilityOptions.Member,
                        joinCode)
                },
                {
                    "state",
                    new DataObject(
                        DataObject.VisibilityOptions.Public,
                        "waiting",
                        index: DataObject.IndexOptions.S1)
                }
            }
        };

        string playerId =
            AuthenticationService.Instance.PlayerId;

        string roomName =
            $"Room_{playerId.Substring(0, 4)}";

        currentLobby =
            await LobbyService.Instance.CreateLobbyAsync(
                roomName,
                2,
                options);

        OnRoomCreated?.Invoke(
            $"Room: {currentLobby.Name}\n" +
            $"ID: {currentLobby.Id}\n" +
            $"Players: {currentLobby.Players.Count}/2\n" +
            $"State: waiting");

        isHostLobbyOwner = true;

        StartLobbyHeartbeat(currentLobby.Id);

        SetupHostRelay(allocation);

        bool ok = NetworkManager.Singleton.StartHost();

        if (!ok)
        {
            OnRoomCreated?.Invoke("Room creation failed. Please cancel and try again.");
            throw new Exception("StartHost Failed");
        }

        
    }

    #endregion

    #region Client

    public async Task JoinLobbyById(string lobbyId)
    {
        try
        {
            var lobby =
                await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId);

            await JoinLobby(lobby);
        }
        catch (Exception e)
        {
            
        }
    }

    private async Task JoinLobby(Lobby lobby)
    {
        if (lobby.Data == null ||
            !lobby.Data.ContainsKey("joinCode") ||
            string.IsNullOrEmpty(lobby.Data["joinCode"].Value))
        {
            throw new Exception("Lobby Missing JoinCode (not ready or invalid)");
        }

        string joinCode = lobby.Data["joinCode"].Value;

        

        await JoinRelay(joinCode);
    }

    public async Task JoinRelay(string joinCode)
    {
        JoinAllocation allocation =
            await WithTimeout(
                RelayService.Instance.JoinAllocationAsync(joinCode));

        var transport =
            NetworkManager.Singleton.GetComponent<UnityTransport>();

        transport.SetRelayServerData(
            allocation.RelayServer.IpV4,
            (ushort)allocation.RelayServer.Port,
            allocation.AllocationIdBytes,
            allocation.Key,
            allocation.ConnectionData,
            allocation.HostConnectionData,
            useDtls);

        bool ok = NetworkManager.Singleton.StartClient();

        if (!ok)
        {
            throw new Exception("StartClient Failed");
        }
    }

    #endregion

    #region Relay

    private void SetupHostRelay(Allocation allocation)
    {
        var transport =
            NetworkManager.Singleton.GetComponent<UnityTransport>();

        transport.SetRelayServerData(
            allocation.RelayServer.IpV4,
            (ushort)allocation.RelayServer.Port,
            allocation.AllocationIdBytes,
            allocation.Key,
            allocation.ConnectionData,
            allocation.ConnectionData,
            useDtls);
    }

    #endregion

    #region Heartbeat

    private void StartLobbyHeartbeat(string lobbyId)
    {
        StopLobbyHeartbeat();

        heartbeatCts = new CancellationTokenSource();

        _ = HeartbeatLoop(lobbyId, heartbeatCts.Token);
    }

    private async Task HeartbeatLoop(
        string lobbyId,
        CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await LobbyService.Instance
                    .SendHeartbeatPingAsync(lobbyId);

                await Task.Delay(
                    TimeSpan.FromSeconds(15),
                    ct);
            }
            catch (TaskCanceledException)
            {
                break;
            }
            catch (Exception e)
            {
                
            }
        }
    }

    private void StopLobbyHeartbeat()
    {
        heartbeatCts?.Cancel();
        heartbeatCts?.Dispose();
        heartbeatCts = null;
    }

    #endregion

    #region Cleanup

    public async Task ExitMatch()
    {
        await DisconnectAndCleanup();
    }

    public async Task DisconnectAndCleanup()
    {
        if (isCleaning)
        {
            
            return;
        }

        isCleaning = true;

        SetState(MatchState.Disconnecting);

        try
        {
            StopLobbyHeartbeat();

            try
            {
                if (NetworkManager.Singleton != null &&
                    NetworkManager.Singleton.IsListening)
                {
                    NetworkManager.Singleton.Shutdown();
                }
            }
            catch (Exception e)
            {
                
            }

            try
            {
                if (currentLobby != null)
                {
                    string lobbyId = currentLobby.Id;

                    string playerId =
                        AuthenticationService.Instance.PlayerId;

                    if (isHostLobbyOwner)
                    {
                        await LobbyService.Instance
                            .DeleteLobbyAsync(lobbyId);
                    }
                    else
                    {
                        await LobbyService.Instance
                            .RemovePlayerAsync(lobbyId, playerId);
                    }
                }
            }
            catch (Exception e)
            {
                
            }

            currentLobby = null;

            isHostLobbyOwner = false;

            hasLoadedGameScene = false;

            cachedLobbies.Clear();

            OnLobbyListChanged?.Invoke();

            
        }
        finally
        {
            isCleaning = false;

            SetState(MatchState.Idle);
        }
    }

    #endregion

    #region Lobby State

    private async Task SetLobbyState(string state)
    {
        try
        {
            if (currentLobby == null)
                return;

            await LobbyService.Instance.UpdateLobbyAsync(
                currentLobby.Id,
                new UpdateLobbyOptions
                {
                    Data = new Dictionary<string, DataObject>
                    {
                        {
                            "state",
                            new DataObject(
                                DataObject.VisibilityOptions.Public,
                                state,
                                index: DataObject.IndexOptions.S1)
                        }
                    }
                });
        }
        catch (Exception e)
        {
            
        }
    }

    #endregion

    #region Utility

    private async Task<T> WithTimeout<T>(
        Task<T> task,
        int timeoutMs = 10000)
    {
        var timeoutTask = Task.Delay(timeoutMs);

        var completed =
            await Task.WhenAny(task, timeoutTask);

        if (completed == timeoutTask)
        {
            throw new TimeoutException();
        }

        return await task;
    }

    #endregion
}