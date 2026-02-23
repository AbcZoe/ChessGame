using System.Collections.Generic;
using UnityEngine;

public enum ChessPieceType
{
    None = 0,
    Bishop = 1,
    Knight = 2,
    King = 3,
    Queen = 4,
    Pawn = 5,
    Rook = 6
}

public class ChessPiece : MonoBehaviour
{
    public int team;
    public int currentX;
    public int currentY;
    public ChessPieceType type;

    public bool isKing = false;

    private Vector3 desiredPosition;
    private Vector3 desiredScale = Vector3.one;
    public float baseScale = 1f; // Inspector 可調比例大小

    private void Start()
    {
        desiredScale = transform.localScale * baseScale;
    }

    private void Update()
    {
        transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * 10);
        transform.localScale = Vector3.Lerp(transform.localScale, desiredScale, Time.deltaTime * 10);
    }

    public void ResetPiece()
    {
        isKing = false;
        team = 0;
        type = ChessPieceType.None;
        desiredScale = Vector3.one * baseScale;
    }

    public void SetKing(bool value)
    {
        isKing = value;
    }

    public virtual List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        return new List<Vector2Int>();
    }

    public virtual List<Vector2Int> GetAvailableAttacks(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        return new List<Vector2Int>();
    }

    public virtual void SetPosition(Vector3 position, bool force = false)
    {
        desiredPosition = position;
        if (force)
            transform.position = desiredPosition;
    }

    public virtual void SetScale(Vector3 scale, bool force = false)
    {
        desiredScale = scale * baseScale;
        if (force)
            transform.localScale = desiredScale;
    }
}
