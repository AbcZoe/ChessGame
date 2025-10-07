using System.Collections.Generic;
using UnityEngine;

public class ChessPiecePool : Singleton<ChessPiecePool>
{
    [SerializeField] private ChessPiece[] chessPiecePrefabs; // 按 ChessPieceType 排列
    [SerializeField] private int initialCountPerType = 5;     // 每種棋子預生成數量

    private Dictionary<ChessPieceType, Queue<ChessPiece>> pool = new();

    protected override void Awake()
    {
        base.Awake();

        // 初始化每種棋子隊列
        foreach (ChessPieceType type in System.Enum.GetValues(typeof(ChessPieceType)))
        {
            if (type == ChessPieceType.None) continue;

            pool[type] = new Queue<ChessPiece>();

            // 預生成棋子
            for (int i = 0; i < initialCountPerType; i++)
            {
                ChessPiece piece = Instantiate(chessPiecePrefabs[(int)type - 1], transform);
                piece.gameObject.SetActive(false);
                piece.type = type;
                pool[type].Enqueue(piece);
            }
        }
    }

    public ChessPiece GetPiece(ChessPieceType type, Transform parent = null)
    {
        if (!pool.ContainsKey(type))
        {
            Debug.LogError($"ChessPiecePool: 無法找到類型 {type} 的隊列!");
            return null;
        }

        ChessPiece piece;

        if (pool[type].Count > 0)
        {
            piece = pool[type].Dequeue();
        }
        else
        {
            // 如果池中沒有了，動態生成
            piece = Instantiate(chessPiecePrefabs[(int)type - 1]);
            piece.type = type;
        }

        piece.gameObject.SetActive(true);
        if (parent != null)
            piece.transform.parent = parent;

        return piece;
    }

    public void ReturnPiece(ChessPiece piece)
    {
        piece.gameObject.SetActive(false);
        piece.isKing = false;
        piece.transform.parent = transform; // 回到 Pool 底下

        if (!pool.ContainsKey(piece.type))
            pool[piece.type] = new Queue<ChessPiece>();

        pool[piece.type].Enqueue(piece);
    }
}
