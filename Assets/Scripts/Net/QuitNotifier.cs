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

    public void NotifyQuitAndLeave()
    {
        if (leaving) return;
        leaving = true;

        // 不在連線中就直接回主選單
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
        {
            UIManager.Instance?.LocalReturnToMenu();
            return;
        }

        if (IsServer)
        {
            // Host 按 Quit：通知 Client 顯示訊息
            HostQuitRpc();
        }
        else
        {
            // Client 按 Quit：通知 Host 顯示訊息
            ClientQuitRpc();
        }

        // 自己一定要離開：不要把「退出」依賴在對方是否收到 RPC
        Invoke(nameof(LeaveNow), 0.2f);
    }

    private void LeaveNow()
    {
        UIManager.Instance?.LocalReturnToMenu();
    }

    private void OnClientDisconnected(ulong clientId)
    {
        // 自己正在走退出流程，就不處理
        if (leaving) return;

        // 只要「對方」斷線，就提示並回主選單
        if (NetworkManager.Singleton != null && clientId != NetworkManager.Singleton.LocalClientId)
        {
            leaving = true;
            UIManager.Instance?.ShowMessage("Another player has quit.", 2.0f);
            UIManager.Instance?.ForceReturnToMenuAfterDelay(2.0f);
        }
    }

    // Client -> Server (Host)
    [Rpc(SendTo.Server)]
    private void ClientQuitRpc()
    {
        if (leaving) return;
        leaving = true;

        UIManager.Instance?.ShowMessage("Another player has quit.", 2.0f);
        UIManager.Instance?.ForceReturnToMenuAfterDelay(2.0f);
    }

    // Host -> Clients
    [Rpc(SendTo.NotServer)]
    private void HostQuitRpc()
    {
        if (leaving) return;
        leaving = true;

        UIManager.Instance?.ShowMessage("The host has logged out; the connection has ended.", 2.0f);
        UIManager.Instance?.ForceReturnToMenuAfterDelay(2.0f);
    }
}