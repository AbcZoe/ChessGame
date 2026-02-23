using System.Collections.Generic;
using UnityEngine;

public class King : ChessPiece
{
    private static readonly Vector2Int[] moveOffsets =
    {
        new(1, 0), new(-1, 0), new(0, 1), new(0, -1),
        new(1, 1), new(1, -1), new(-1, 1), new(-1, -1)
    };

    public override List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> moves = new();
        foreach (var offset in moveOffsets)
        {
            Vector2Int pos = new(currentX + offset.x, currentY + offset.y);
            if (!IsInsideBoard(pos, tileCountX, tileCountY))
                continue;

            if (board[pos.x, pos.y] == null)
                moves.Add(pos);
        }
        return moves;
    }

    public override List<Vector2Int> GetAvailableAttacks(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> attacks = new();
        foreach (var offset in moveOffsets)
        {
            Vector2Int pos = new(currentX + offset.x, currentY + offset.y);
            if (!IsInsideBoard(pos, tileCountX, tileCountY))
                continue;

            ChessPiece target = board[pos.x, pos.y];
            if (target != null && target.team != team)
                attacks.Add(pos);
        }
        return attacks;
    }

    private bool IsInsideBoard(Vector2Int pos, int tileCountX, int tileCountY)
    {
        return pos.x >= 0 && pos.x < tileCountX && pos.y >= 0 && pos.y < tileCountY;
    }
}
