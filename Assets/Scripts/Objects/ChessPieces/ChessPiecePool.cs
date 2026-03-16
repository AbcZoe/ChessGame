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

        piece.ResetPiece();
        piece.gameObject.SetActive(true);
        

        return piece;
    }

    public void ReturnPiece(ChessPiece piece)
    {
        NetworkObject netObj = piece.GetComponent<NetworkObject>();

        if (netObj != null && netObj.IsSpawned)
            netObj.Despawn(false);   // ¤£ destroy

        piece.ResetPiece();
        piece.gameObject.SetActive(false);

        if (!pool.ContainsKey(piece.type))
            pool[piece.type] = new Queue<ChessPiece>();

        pool[piece.type].Enqueue(piece);
    }
}