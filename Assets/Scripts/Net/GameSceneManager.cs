using Unity.Netcode;
using UnityEngine;

public class GameSceneManager : NetworkBehaviour
{
    [Header("Spawn Points")]
    [SerializeField] private Transform P1Spawn;
    [SerializeField] private Transform P2Spawn;

    [Header("Player Prefab")]
    [SerializeField] private GameObject playerPrefab;

    private void Awake()
    {
        // 檢查空物件
        if (P1Spawn == null || P2Spawn == null)
            Debug.LogError("P1Spawn 或 P2Spawn 沒有被設定！");
        if (playerPrefab == null)
            Debug.LogError("PlayerPrefab 沒有被設定！");
    }

    public override void OnNetworkSpawn()
    {
        // 只讓 Server 或 Host spawn 玩家
        if (!IsServer) return;

        // spawn P1
        if (P1Spawn != null && playerPrefab != null)
        {
            GameObject p1 = Instantiate(playerPrefab, P1Spawn.position, P1Spawn.rotation);
            p1.GetComponent<NetworkObject>().SpawnAsPlayerObject(0); // clientId 0 = host
        }

        // spawn P2
        if (P2Spawn != null && playerPrefab != null)
        {
            GameObject p2 = Instantiate(playerPrefab, P2Spawn.position, P2Spawn.rotation);
            p2.GetComponent<NetworkObject>().SpawnAsPlayerObject(1); // clientId 1 = 第二個玩家
        }
    }
}