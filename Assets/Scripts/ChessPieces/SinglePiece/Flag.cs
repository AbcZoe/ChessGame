using System.Collections.Generic;
using UnityEngine;

public class Flag : ChessPiece
{
    public override List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> moves = new List<Vector2Int>();

        int[,] diagonalMoves = { { 1, 1 }, { 1, -1 }, { -1, 1 }, { -1, -1 } };

        for (int i = 0; i < diagonalMoves.GetLength(0); i++)
        {
            Vector2Int pos = new Vector2Int(currentX + diagonalMoves[i, 0], currentY + diagonalMoves[i, 1]);
            if (IsInsideBoard(pos, tileCountX, tileCountY))
                moves.Add(pos);
        }

        return moves;
    }
    public override List<Vector2Int> GetAvailableAttacks(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> attacks = new List<Vector2Int>();

        int[,] crossAttacks = { { 1, 0 }, { -1, 0 }, { 0, 1 }, { 0, -1 } };

        for (int i = 0; i < crossAttacks.GetLength(0); i++)
        {
            Vector2Int pos = new Vector2Int(currentX + crossAttacks[i, 0], currentY + crossAttacks[i, 1]);
            if (IsInsideBoard(pos, tileCountX, tileCountY))
                attacks.Add(pos);
        }

        return attacks;
    }

    // 工具方法：檢查是否在棋盤內
    private bool IsInsideBoard(Vector2Int pos, int tileCountX, int tileCountY)
    {
        return pos.x >= 0 && pos.x < tileCountX && pos.y >= 0 && pos.y < tileCountY;
    }
}
