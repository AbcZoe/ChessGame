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

        NetworkObject netObj = piece.GetComponent<NetworkObject>();

        if (NetworkManager.Singleton.IsServer && netObj != null && !netObj.IsSpawned)
            netObj.Spawn();

        return piece;
    }

    public void ReturnPiece(ChessPiece piece)
    {
        piece.ResetPiece();
        piece.transform.parent = transform;
        pool[piece.type].Enqueue(piece);
    }
}