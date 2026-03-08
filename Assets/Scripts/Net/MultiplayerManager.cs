using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MultiplayerManager : MonoBehaviour
{
    public static MultiplayerManager Instance;

    void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void StartHost()
    {
        NetworkManager.Singleton.StartHost();

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    public void StartClient()
    {
        NetworkManager.Singleton.StartClient();
    }

    void OnClientConnected(ulong clientId)
    {
        if (NetworkManager.Singleton.ConnectedClients.Count >= 2)
        {
            NetworkManager.Singleton.SceneManager.LoadScene(
                "GameScene",
                LoadSceneMode.Single
            );
        }
    }
}