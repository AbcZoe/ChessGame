using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MultiplayerManager : MonoBehaviour
{
    public static MultiplayerManager Instance;

    private void Awake()
    {
        if (Instance != null)
            Destroy(gameObject);
        else
            Instance = this;

        DontDestroyOnLoad(gameObject);
    }

    public void StartHost()
    {
        if (!NetworkManager.Singleton.IsServer)
        {
            NetworkManager.Singleton.StartHost();
        }

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    public void StartClient()
    {
        if (!NetworkManager.Singleton.IsClient)
        {
            NetworkManager.Singleton.StartClient();
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        // 當至少 2 位玩家連線時，載入 GameScene（只由 Host 執行）
        if (!NetworkManager.Singleton.IsServer) return;

        if (NetworkManager.Singleton.ConnectedClients.Count >= 2)
        {
            NetworkManager.Singleton.SceneManager.LoadScene(
                "GameScene",
                UnityEngine.SceneManagement.LoadSceneMode.Single
            );
        }
    }
}