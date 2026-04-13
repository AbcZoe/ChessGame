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
using System.Net.NetworkInformation;
using System.Linq;

public class LanAutoMatchmaker : MonoBehaviour
{
    public static LanAutoMatchmaker Instance { get; private set; }

    [Header("Ports")]
    [SerializeField] private int discoveryPort = 47777;
    [SerializeField] private ushort gamePort = 7777;

    [Header("Timings")]
    [SerializeField] private float clientListenSeconds = 5f; 
    [SerializeField] private float advertiseIntervalSeconds = 0.7f;

    [SerializeField] private bool sameMachineTest = true;
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
        // 只在同機雙開測試才用檔案鎖決定 Host
        if (sameMachineTest && TryAcquireHostLock())
        {
            StartHost(gamePort);
            _ = AdvertiseHostLoopAsync(token);
            return;
        }

        // 跨機：先聽廣播找 Host
        var found = await TryFindHostAsync(clientListenSeconds, token);
        if (token.IsCancellationRequested) return;

        if (found.found)
        {
            StartClient(found.ip, gamePort);
            return;
        }

        // 找不到就自己 Host（跨機時就靠先按的人當 Host）
        StartHost(gamePort);
        _ = AdvertiseHostLoopAsync(token);
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
                    // 用封包來源當作 Host IP（比 parts[1] 更準）
                    string hostIp = result.RemoteEndPoint.Address.ToString();
                    return (true, hostIp);
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

        var endpoint = new IPEndPoint(IPAddress.Broadcast, discoveryPort);

        while (!token.IsCancellationRequested && NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            string ip = sameMachineTest ? "127.0.0.1" : GetLocalIPv4(); // "127.0.0.1"同機測試，GetLocalIPv4()不同機測試時，client 會從廣播訊息裡拿真實 IP 連回來
            string payload = $"CHESSHOST|{ip}|{gamePort}";
            byte[] data = Encoding.UTF8.GetBytes(payload);
            try { await udp.SendAsync(data, data.Length, endpoint); }
            catch { /* ignore */ }

            await Task.Delay(TimeSpan.FromSeconds(advertiseIntervalSeconds), token);
        }
    }

    private string GetLocalIPv4()
    {
        // 先挑出看起來最像「真實連線中」的網卡
        var candidates = NetworkInterface.GetAllNetworkInterfaces()
            .Where(ni =>
                ni.OperationalStatus == OperationalStatus.Up &&
                ni.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                ni.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
            .OrderByDescending(ni =>
            {
                // 粗略排序：Ethernet/WiFi 優先於其他
                return ni.NetworkInterfaceType switch
                {
                    NetworkInterfaceType.Ethernet => 3,
                    NetworkInterfaceType.Wireless80211 => 2,
                    _ => 1
                };
            });

        foreach (var ni in candidates)
        {
            // 排除一些常見虛擬網卡（可按你環境調整/刪掉）
            string name = (ni.Name ?? "").ToLowerInvariant();
            string desc = (ni.Description ?? "").ToLowerInvariant();
            if (name.Contains("virtual") || name.Contains("vmware") || name.Contains("hyper-v") ||
                name.Contains("vpn") || name.Contains("loopback") ||
                desc.Contains("virtual") || desc.Contains("vmware") || desc.Contains("hyper-v") ||
                desc.Contains("vpn"))
            {
                continue;
            }

            var ipProps = ni.GetIPProperties();
            foreach (var ua in ipProps.UnicastAddresses)
            {
                if (ua.Address.AddressFamily != AddressFamily.InterNetwork)
                    continue;

                string ip = ua.Address.ToString();

                // 避免 127.x 與 0.0.0.0
                if (ip.StartsWith("127.") || ip == "0.0.0.0")
                    continue;

                // 避免 APIPA（斷網時 Windows 會給 169.254.x.x）
                if (ip.StartsWith("169.254."))
                    continue;

                return ip;
            }
        }

        // 真的找不到就回 loopback（至少不會 null）
        return "127.0.0.1";
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
