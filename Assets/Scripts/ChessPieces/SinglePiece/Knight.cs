using System.Collections.Generic;
using UnityEngine;

public class Knight : ChessPiece
{
    public override List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> moves = new List<Vector2Int>();

        int[,] moveOffsets = {
            {2, 1}, {1, 2}, {-1, 2}, {-2, 1},
            {-2, -1}, {-1, -2}, {1, -2}, {2, -1}
        };

        for (int i = 0; i < moveOffsets.GetLength(0); i++)
        {
            Vector2Int pos = new Vector2Int(currentX + moveOffsets[i, 0], currentY + moveOffsets[i, 1]);
            if (!IsInsideBoard(pos, tileCountX, tileCountY)) continue;

            ChessPiece target = board[pos.x, pos.y];
            if (target == null || target.team != team)
                moves.Add(pos);
        }

        return moves;
    }

    public override List<Vector2Int> GetAvailableAttacks(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> attacks = new List<Vector2Int>();

        int[,] moveOffsets = {
            {2, 1}, {1, 2}, {-1, 2}, {-2, 1},
            {-2, -1}, {-1, -2}, {1, -2}, {2, -1}
        };

        for (int i = 0; i < moveOffsets.GetLength(0); i++)
        {
            Vector2Int pos = new Vector2Int(currentX + moveOffsets[i, 0], currentY + moveOffsets[i, 1]);
            if (!IsInsideBoard(pos, tileCountX, tileCountY)) continue;

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
