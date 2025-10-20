using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System.Collections.Generic;
using System;
using UnityEngine.SceneManagement;
using System.Linq;

public class BasicSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    private NetworkRunner _runner;

    private void Start()
    {
        // For demonstration, we start as a Host. Change to GameMode.Client to join as a client.
        _ = StartGameAsync(GameMode.AutoHostOrClient);
    }

    private async System.Threading.Tasks.Task StartGameAsync(GameMode mode)
    {
        // 如果還沒有 Runner，就自動建立
        if (_runner == null)
        {
            _runner = gameObject.AddComponent<NetworkRunner>();
            _runner.name = "AutoCreated_NetworkRunner";
        }

        _runner.ProvideInput = true;
        _runner.AddCallbacks(this);

        var scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);

        var result = await _runner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = "TestRoom",
            Scene = scene,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>(),
            PlayerCount = 2
        });

        if (result.Ok)
            Debug.Log("[Fusion] NetworkRunner successfully started!");
        else
            Debug.LogError($"[Fusion] Failed to start NetworkRunner: {result.ShutdownReason}");
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) 
    {
        UIManager.Instance.ShowConnectMessage($" Player {player.PlayerId} joined.");
    }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) 
    {
        UIManager.Instance.ShowConnectMessage($" Player {player.PlayerId} left.");
    }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) 
    {
        int currentPlayers = runner.ActivePlayers.Count(); // 目前已加入的玩家數量

        if (currentPlayers >= 2)
        {
            request.Refuse(); // 拒絕加入
            Debug.Log("A player tried to join but the room is full.");
        }
        else
        {
            request.Accept(); // 接受加入
        }
    }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }

}
