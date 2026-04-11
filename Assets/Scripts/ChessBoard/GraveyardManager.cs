using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GraveyardManager : MonoBehaviour
{
    public static GraveyardManager Instance { get; private set; }

    [SerializeField] private float deathSize = 50f;       // 死亡棋子縮放
    [SerializeField] private float deathSpacing = 0.4f;    // 死亡棋子間隔
    [SerializeField] private float yOffset = 0.2f;         // 高度補正
    [SerializeField] private float tileSize = 1f;          // 棋盤格大小

    // 記錄死亡棋子（Server authoritative 清理用）
    private readonly HashSet<ulong> deadPieceNetIds = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

    }

    /// 將死亡棋子加入死亡區
    public void AddDeadPiece(ChessPiece piece, int deadIndex)
    {
        if (piece == null) return;

        // 記錄（避免重複）
        if (piece.NetworkObject != null)
            deadPieceNetIds.Add(piece.NetworkObjectId);

        int team = piece.Team.Value;

        // 縮放
        piece.SetScale(Vector3.one * deathSize);

        Vector3 deathBase = team == 0
            ? new Vector3(8 * tileSize, yOffset, -1 * tileSize) + Vector3.one * tileSize / 2
            : new Vector3(-1 * tileSize, yOffset, 8 * tileSize) + Vector3.one * tileSize / 2;

        // 用 deadIndex 排隊（不依賴本機 list count）
        float zOffset = (team == 0 ? 1f : -1f) * deathSpacing * (deadIndex + 0.5f);
        Vector3 deathOffset = new Vector3(0f, 0f, zOffset);

        // 位置
        piece.SetPosition(deathBase + deathOffset);
    }

    // 只在 Server 呼叫：清除目前所有死亡棋子
    public void ClearDeadPieces()
    {
        if (!NetworkManager.Singleton || !NetworkManager.Singleton.IsServer) return;

        foreach (var netId in deadPieceNetIds)
        {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(netId, out var netObj))
            {
                if (netObj != null && netObj.IsSpawned)
                    netObj.Despawn(true);
            }
        }

        deadPieceNetIds.Clear();
    }

}