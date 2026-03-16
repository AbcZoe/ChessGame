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

    private readonly Dictionary<int, List<ChessPiece>> deadPieces = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        deadPieces[0] = new List<ChessPiece>();
        deadPieces[1] = new List<ChessPiece>();
    }

    /// 將死亡棋子加入死亡區
    public void AddDeadPiece(ChessPiece piece)
    {
        if (piece == null) return;

        int team = piece.Team.Value;
        deadPieces[team].Add(piece);

        piece.SetScale(Vector3.one * deathSize, true);

        // 計算死亡棋子位置
        Vector3 deathBase = team == 0
            ? new Vector3(8 * tileSize, yOffset, -1 * tileSize) + Vector3.one * tileSize / 2
            : new Vector3(-1 * tileSize, yOffset, 8 * tileSize) + Vector3.one * tileSize / 2;

        float zOffset = (team == 0 ? 1f : -1f) * deathSpacing * deadPieces[team].Count * 0.5f;
        Vector3 deathOffset = new Vector3(0f, 0f, zOffset);

        piece.SetPosition(deathBase + deathOffset);

    }
    /// 重置死亡區（通常在重置棋盤時）
    public void ResetGraveyard()
    {
        foreach (var kvp in deadPieces)
        {
            foreach (var piece in kvp.Value)
            {
                if (piece == null) continue;

                // 1. 先 Despawn NetworkObject 避免 Authority 或 SpawnStateException
                NetworkObject netObj = piece.GetComponent<NetworkObject>();
                if (netObj != null && netObj.IsSpawned)
                    netObj.Despawn(false);

                // 2. 重置棋子狀態
                piece.ResetPiece();  // 包含 scale、Team、isKing、X/Y

                // 3. 回 Pool
                ChessPiecePool.Instance.ReturnPiece(piece);
            }

            // 4. 清空死亡區列表
            kvp.Value.Clear();
        }
    }
}