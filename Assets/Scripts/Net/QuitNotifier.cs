using System;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

public class QuitNotifier : NetworkBehaviour
{
    public static QuitNotifier Instance { get; private set; }

    private bool leaving;

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    }

    public override void OnNetworkDespawn()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
    }

    public async void NotifyQuitAndLeave()
    {
        if (leaving) return;
        leaving = true;

        try
        {
            if (NetworkManager.Singleton == null ||
                !NetworkManager.Singleton.IsListening)
            {
                UIManager.Instance?.ReturnToMenuAsync();
                return;
            }

            if (IsServer)
            {
                HostQuitRpc();
            }
            else
            {
                ClientQuitRpc();
            }

            // 等一點點 RPC flush（比 Invoke 更安全）
            await Task.Delay(200);

            // 等 MatchSystem 真正清理完
            if (MatchSystem.Instance != null)
            {
                await MatchSystem.Instance.ExitMatch();
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Quit Error: {e.Message}");
        }

        UIManager.Instance?.ReturnToMenuAsync();
    }

    private void OnClientDisconnected(ulong clientId)
    {
        // 自己正在走退出流程，就不處理
        if (leaving) return;

        // 只要「對方」斷線，就提示並回主選單
        if (NetworkManager.Singleton != null && clientId != NetworkManager.Singleton.LocalClientId)
        {
            leaving = true;
            UIManager.Instance?.ShowMessage("QuitMsg", 2.0f);
            UIManager.Instance?.ForceReturnToMenuAfterDelay(2.0f);
        }
    }

    // Client -> Server (Host)
    [Rpc(SendTo.Server)]
    private void ClientQuitRpc()
    {
        if (leaving) return;
        leaving = true;

        UIManager.Instance?.ShowMessage("QuitMsg", 2.0f);
        UIManager.Instance?.ForceReturnToMenuAfterDelay(2.0f);
    }

    // Host -> Clients
    [Rpc(SendTo.NotServer)]
    private void HostQuitRpc()
    {
        if (leaving) return;
        leaving = true;

        UIManager.Instance?.ShowMessage("HostQuitMsg", 2.0f);
        UIManager.Instance?.ForceReturnToMenuAfterDelay(2.0f);
    }
}