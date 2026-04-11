using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System;
using Unity.Netcode.Transports.UTP;
using Unity.Netcode;
using UnityEngine;
using System.IO;

public class LanAutoMatchmaker : MonoBehaviour
{
    public static LanAutoMatchmaker Instance { get; private set; }

    [Header("Ports")]
    [SerializeField] private int discoveryPort = 47777;
    [SerializeField] private ushort gamePort = 7777;

    [Header("Timings")]
    [SerializeField] private float clientListenSeconds = 10f; // 同機測試給久一點
    [SerializeField] private float advertiseIntervalSeconds = 0.7f;

    private CancellationTokenSource cts;

    // 同機雙開：用檔案鎖決定誰當 Host
    private FileStream hostLockStream;
    private string hostLockPath;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        hostLockPath = Path.Combine(Application.persistentDataPath, "lan_host.lock");
    }

    public void StartMatch()
    {
        if (cts != null) return;
        cts = new CancellationTokenSource();
        _ = StartMatchInternalAsync(cts.Token);
    }

    public void CancelMatch()
    {
        cts?.Cancel();
        cts = null;

        ReleaseHostLock();

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            NetworkManager.Singleton.Shutdown();
    }

    private async Task StartMatchInternalAsync(CancellationToken token)
    {
        // 1) 同機：先搶 Host 鎖，搶到者直接 Host（避免 port 衝突）
        if (TryAcquireHostLock())
        {
            StartHost(gamePort);
            _ = AdvertiseHostLoopAsync(token);
            return;
        }

        // 2) 搶不到：當 Client，去聽廣播找 Host
        var found = await TryFindHostAsync(clientListenSeconds, token);
        if (token.IsCancellationRequested) return;

        if (found.found)
        {
            StartClient(found.ip, gamePort);
            return;
        }

        Debug.LogWarning("[LAN] No host found (timeout). If testing on same PC, start the other build first.");
    }

    private bool TryAcquireHostLock()
    {
        try
        {
            // OpenOrCreate + FileShare.None = 只有一個 process 能成功開到
            hostLockStream = new FileStream(hostLockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            using var writer = new StreamWriter(hostLockStream, Encoding.UTF8, 256, leaveOpen: true);
            writer.WriteLine(DateTime.UtcNow.ToString("O"));
            writer.Flush();

            Debug.Log("[LAN] Acquired host lock -> will become Host.");
            return true;
        }
        catch
        {
            Debug.Log("[LAN] Host lock is held by another instance -> will become Client.");
            hostLockStream = null;
            return false;
        }
    }

    private void ReleaseHostLock()
    {
        try { hostLockStream?.Dispose(); } catch { }
        hostLockStream = null;

        // 檔案可以不刪，保留也沒差；想刪也可以
        // try { if (File.Exists(hostLockPath)) File.Delete(hostLockPath); } catch { }
    }

    private async Task<(bool found, string ip)> TryFindHostAsync(float timeoutSeconds, CancellationToken token)
    {
        using var udp = new UdpClient();
        udp.EnableBroadcast = true;
        udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        udp.Client.Bind(new IPEndPoint(IPAddress.Any, discoveryPort));

        float endTime = Time.realtimeSinceStartup + timeoutSeconds;

        while (!token.IsCancellationRequested && Time.realtimeSinceStartup < endTime)
        {
            if (udp.Available > 0)
            {
                UdpReceiveResult result = await udp.ReceiveAsync();
                string msg = Encoding.UTF8.GetString(result.Buffer);

                if (msg.StartsWith("CHESSHOST|"))
                {
                    var parts = msg.Split('|');
                    if (parts.Length >= 3)
                        return (true, parts[1]);
                }
            }

            await Task.Delay(30, token);
        }

        return (false, null);
    }

    private async Task AdvertiseHostLoopAsync(CancellationToken token)
    {
        using var udp = new UdpClient();
        udp.EnableBroadcast = true;

        string ip = "127.0.0.1"; // 同機測試，broadcast 內容用 loopback 最穩，不同機測試時，client 會從廣播訊息裡拿真實 IP 連回來 GetLocalIPv4()
        string payload = $"CHESSHOST|{ip}|{gamePort}";
        byte[] data = Encoding.UTF8.GetBytes(payload);
        var endpoint = new IPEndPoint(IPAddress.Broadcast, discoveryPort);

        while (!token.IsCancellationRequested && NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            try { await udp.SendAsync(data, data.Length, endpoint); }
            catch { /* ignore */ }

            await Task.Delay(TimeSpan.FromSeconds(advertiseIntervalSeconds), token);
        }
    }

    private void StartHost(ushort port)
    {
        var utp = NetworkManager.Singleton.GetComponent<UnityTransport>();
        utp.ConnectionData.Address = "0.0.0.0";
        utp.ConnectionData.Port = port;

        NetworkManager.Singleton.StartHost();
        Debug.Log($"[LAN] Host started on {port}");
    }

    private void StartClient(string hostIp, ushort port)
    {
        var utp = NetworkManager.Singleton.GetComponent<UnityTransport>();
        utp.ConnectionData.Address = hostIp;
        utp.ConnectionData.Port = port;

        NetworkManager.Singleton.StartClient();
        Debug.Log($"[LAN] Client connecting to {hostIp}:{port}");
    }

    private void OnApplicationQuit()
    {
        ReleaseHostLock();
    }
}
