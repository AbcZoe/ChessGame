using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class ChessPiecePool : Singleton<ChessPiecePool>
{
    [SerializeField] private ChessPiece[] chessPiecePrefabs;
    [SerializeField] private int initialCountPerType = 5;

    private Dictionary<ChessPieceType, Queue<ChessPiece>> pool = new();

    protected override void Awake()
    {
        base.Awake();

        foreach (ChessPieceType type in System.Enum.GetValues(typeof(ChessPieceType)))
        {
            if (type == ChessPieceType.None) continue;

            pool[type] = new Queue<ChessPiece>();

            for (int i = 0; i < initialCountPerType; i++)
            {
                ChessPiece piece = Instantiate(chessPiecePrefabs[(int)type - 1], transform);
                piece.gameObject.SetActive(false);
                piece.type = type;

                // Host 端才 Spawn
                NetworkObject netObj = piece.GetComponent<NetworkObject>();
                if (NetworkManager.Singleton.IsHost && !netObj.IsSpawned)
                    netObj.Spawn();

                pool[type].Enqueue(piece);
            }
        }
    }

    public ChessPiece GetPiece(ChessPieceType type)
    {
        ChessPiece piece;
        if (pool[type].Count > 0)
            piece = pool[type].Dequeue();
        else
            piece = Instantiate(chessPiecePrefabs[(int)type - 1]);

        piece.gameObject.SetActive(true);

        // 只 Host Spawn
        NetworkObject netObj = piece.GetComponent<NetworkObject>();
        if (NetworkManager.Singleton.IsHost && netObj != null && !netObj.IsSpawned)
            netObj.Spawn();

        return piece;
    }

    public void ReturnPiece(ChessPiece piece)
    {
        piece.gameObject.SetActive(false);
        piece.ResetPiece();
        piece.transform.parent = transform; // 回到 Pool 底下

        if (!pool.ContainsKey(piece.type))
            pool[piece.type] = new Queue<ChessPiece>();

        pool[piece.type].Enqueue(piece);
    }
}